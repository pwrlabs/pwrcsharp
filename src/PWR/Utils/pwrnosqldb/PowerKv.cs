using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Web;

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
        _serverUrl = "https://pwrnosqlvida.pwrlabs.io/";

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

    public async Task<bool> PutAsync(object key, object data)
    {
        var keyBytes = ToBytes(key);
        var dataBytes = ToBytes(data);
        return await PutBytesAsync(keyBytes, dataBytes);
    }

    public async Task<bool> PutBytesAsync(byte[] key, byte[] data)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var url = _serverUrl + "/storeData";
        var payload = new StoreDataRequest
        {
            ProjectId = _projectId,
            Secret = _secret,
            Key = ToHexString(key),
            Value = ToHexString(data)
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

            throw new PowerKvException("ServerError", $"storeData failed: {message}");
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

        var keyHex = ToHexString(key);
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
                    
                    return FromHexString(responseObj.Value);
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

