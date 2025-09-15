using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Web;
using System.Security.Cryptography;
using System.IO;
using PWR.Utils;

namespace PWR.Utils.PowerKv;

public class PowerKvException : Exception
{
    public string ErrorType { get; }

    public PowerKvException(string errorType, string message) : base(message)
    {
        ErrorType = errorType;
    }

    public PowerKvException(string errorType, string message, Exception innerException) : base(message, innerException)
    {
        ErrorType = errorType;
    }
}

public class StoreDataRequest
{
    [JsonProperty("projectId")]
    public string ProjectId { get; set; } = string.Empty;

    [JsonProperty("secret")]
    public string Secret { get; set; } = string.Empty;

    [JsonProperty("key")]
    public string Key { get; set; } = string.Empty;

    [JsonProperty("value")]
    public string Value { get; set; } = string.Empty;
}

public class GetValueResponse
{
    [JsonProperty("value")]
    public string Value { get; set; } = string.Empty;
}

public class ErrorResponse
{
    [JsonProperty("message")]
    public string? Message { get; set; }
}

public class PowerKv : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly string _serverUrl;
    private readonly string _projectId;
    private readonly string _secret;
    private bool _disposed = false;

    public PowerKv(string projectId, string secret)
    {
        if (string.IsNullOrWhiteSpace(projectId))
            throw new PowerKvException("InvalidInput", "Project ID cannot be null or empty");
        
        if (string.IsNullOrWhiteSpace(secret))
            throw new PowerKvException("InvalidInput", "Secret cannot be null or empty");

        _projectId = projectId;
        _secret = secret;
        _serverUrl = "https://powerkvbe.pwrlabs.io";

        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(10)
        };
    }

    public string GetServerUrl() => _serverUrl;
    public string GetProjectId() => _projectId;

    private static string ToHexString(byte[] data)
    {
        return Convert.ToHexString(data).ToLowerInvariant();
    }

    private static byte[] FromHexString(string hexString)
    {
        // Handle both with and without 0x prefix
        if (hexString.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            hexString = hexString[2..];

        try
        {
            return Convert.FromHexString(hexString);
        }
        catch (Exception ex)
        {
            throw new PowerKvException("HexDecodeError", $"Invalid hex: {ex.Message}", ex);
        }
    }

    private static byte[] ToBytes(object data)
    {
        return data switch
        {
            null => throw new PowerKvException("InvalidInput", "Data cannot be null"),
            byte[] bytes => bytes,
            string str => Encoding.UTF8.GetBytes(str),
            int i => Encoding.UTF8.GetBytes(i.ToString()),
            long l => Encoding.UTF8.GetBytes(l.ToString()),
            float f => Encoding.UTF8.GetBytes(f.ToString()),
            double d => Encoding.UTF8.GetBytes(d.ToString()),
            _ => throw new PowerKvException("InvalidInput", "Data must be byte[], string, or number")
        };
    }

    private static byte[] Hash256(byte[] input)
    {
        // PWRHash - Keccak256 hash function
        // Using SHA256 as a fallback since SHA3 is not available in all .NET versions
        using var sha256 = SHA256.Create();
        return sha256.ComputeHash(input);
    }

    private static byte[] PackData(byte[] key, byte[] data)
    {
        // Binary data packing (ByteBuffer equivalent)
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        
        // Write key length (4 bytes, big-endian) + key bytes
        var keyLengthBytes = BitConverter.GetBytes((uint)key.Length);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(keyLengthBytes);
        writer.Write(keyLengthBytes);
        writer.Write(key);
        
        // Write data length (4 bytes, big-endian) + data bytes
        var dataLengthBytes = BitConverter.GetBytes((uint)data.Length);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(dataLengthBytes);
        writer.Write(dataLengthBytes);
        writer.Write(data);
        
        return stream.ToArray();
    }

    private static (byte[] key, byte[] data) UnpackData(byte[] packedBuffer)
    {
        // Binary data unpacking
        using var stream = new MemoryStream(packedBuffer);
        using var reader = new BinaryReader(stream);
        
        // Read key length (4 bytes, big-endian)
        var keyLengthBytes = reader.ReadBytes(4);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(keyLengthBytes);
        var keyLength = BitConverter.ToUInt32(keyLengthBytes, 0);
        
        // Read key bytes
        var key = reader.ReadBytes((int)keyLength);
        
        // Read data length (4 bytes, big-endian)
        var dataLengthBytes = reader.ReadBytes(4);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(dataLengthBytes);
        var dataLength = BitConverter.ToUInt32(dataLengthBytes, 0);
        
        // Read data bytes
        var data = reader.ReadBytes((int)dataLength);
        
        return (key, data);
    }

    public async Task<bool> PutAsync(object key, object data)
    {
        var keyBytes = ToBytes(key);
        var dataBytes = ToBytes(data);
        return await PutBytesAsync(keyBytes, dataBytes);
    }

    public async Task<bool> PutBytesAsync(byte[] key, byte[] data)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        // Hash the key with Keccak256
        var keyHash = Hash256(key);
        
        // Pack the original key and data
        var packedData = PackData(key, data);
        
        // Encrypt the packed data
        var encryptedData = AES256.Encrypt(packedData, _secret);

        var url = _serverUrl + "/storeData";
        var payload = new StoreDataRequest
        {
            ProjectId = _projectId,
            Secret = _secret,
            Key = ToHexString(keyHash),
            Value = ToHexString(encryptedData)
        };

        var jsonContent = JsonConvert.SerializeObject(payload);
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        try
        {
            var response = await _httpClient.PostAsync(url, content);
            var responseText = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                return true;
            }

            throw new PowerKvException("ServerError", $"storeData failed: {(int)response.StatusCode} - {responseText}");
        }
        catch (HttpRequestException ex)
        {
            throw new PowerKvException("NetworkError", $"Request failed: {ex.Message}", ex);
        }
        catch (TaskCanceledException ex)
        {
            throw new PowerKvException("NetworkError", "Request timeout", ex);
        }
    }

    public async Task<byte[]> GetValueAsync(object key)
    {
        var keyBytes = ToBytes(key);
        return await GetValueBytesAsync(keyBytes);
    }

    public async Task<byte[]> GetValueBytesAsync(byte[] key)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        // Hash the key with Keccak256
        var keyHash = Hash256(key);
        var keyHex = ToHexString(keyHash);
        var url = $"{_serverUrl}/getValue?projectId={HttpUtility.UrlEncode(_projectId)}&key={keyHex}";

        try
        {
            var response = await _httpClient.GetAsync(url);
            var responseText = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                try
                {
                    var responseObj = JsonConvert.DeserializeObject<GetValueResponse>(responseText);
                    if (responseObj?.Value == null)
                        throw new PowerKvException("ServerError", $"Unexpected response shape from /getValue: {responseText}");
                    
                    var valueHex = responseObj.Value;
                    
                    // Handle both with/without 0x prefix
                    var cleanHex = valueHex;
                    if (cleanHex.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                        cleanHex = cleanHex[2..];
                    
                    var encryptedValue = FromHexString(cleanHex);
                    
                    // Decrypt the data
                    var decryptedData = AES256.Decrypt(encryptedValue, _secret);
                    
                    // Unpack the data to get original key and data
                    var (originalKey, actualData) = UnpackData(decryptedData);
                    
                    return actualData;
                }
                catch (JsonException ex)
                {
                    throw new PowerKvException("ServerError", $"Unexpected response shape from /getValue: {responseText}", ex);
                }
            }

            // Parse error message
            string message;
            try
            {
                var errorResponse = JsonConvert.DeserializeObject<ErrorResponse>(responseText);
                message = errorResponse?.Message ?? $"HTTP {(int)response.StatusCode}";
            }
            catch
            {
                message = $"HTTP {(int)response.StatusCode} — {responseText}";
            }

            throw new PowerKvException("ServerError", $"getValue failed: {message}");
        }
        catch (HttpRequestException ex)
        {
            throw new PowerKvException("NetworkError", $"GET /getValue failed (network/timeout): {ex.Message}", ex);
        }
        catch (TaskCanceledException ex)
        {
            throw new PowerKvException("NetworkError", "GET /getValue failed (network/timeout)", ex);
        }
    }

    public async Task<string> GetStringValueAsync(object key)
    {
        var data = await GetValueAsync(key);
        return Encoding.UTF8.GetString(data);
    }

    public async Task<int> GetIntValueAsync(object key)
    {
        var data = await GetValueAsync(key);
        var str = Encoding.UTF8.GetString(data);
        
        if (!int.TryParse(str, out var value))
            throw new PowerKvException("ServerError", $"Invalid integer: {str}");
        
        return value;
    }

    public async Task<long> GetLongValueAsync(object key)
    {
        var data = await GetValueAsync(key);
        var str = Encoding.UTF8.GetString(data);
        
        if (!long.TryParse(str, out var value))
            throw new PowerKvException("ServerError", $"Invalid long: {str}");
        
        return value;
    }

    public async Task<double> GetDoubleValueAsync(object key)
    {
        var data = await GetValueAsync(key);
        var str = Encoding.UTF8.GetString(data);
        
        if (!double.TryParse(str, out var value))
            throw new PowerKvException("ServerError", $"Invalid double: {str}");
        
        return value;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _httpClient?.Dispose();
            _disposed = true;
        }
    }
}