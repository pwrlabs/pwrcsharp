using System.Text;
using System.Text.Json;
using Nethereum.Hex.HexConvertors.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PWRPY;

public class ApiResponse<T>
{
    public bool Success { get; }
    public string Message { get; }
    public T? Data { get; }

    public ApiResponse(bool success, string message, T? data = default)
    {
        Success = success;
        Message = message;
        Data = data;
    }
}

public class ApiResponse : ApiResponse<object>
{
    public ApiResponse(bool success, string message) : base(success, message, null)
    {
    }
}

public class PwrApiSdk
{
    private readonly string _rpcNodeUrl;
    private readonly HttpClient _httpClient;

    public PwrApiSdk(string rpcNodeUrl, HttpClient? httpClient = null)
    {
        _rpcNodeUrl = rpcNodeUrl ?? throw new InvalidOperationException("RPC Node URL cannot be null");
        _httpClient = httpClient ?? new HttpClient();
    }

    public int FeePerByte { get; private set; } = 100;
    public string RpcNodeUrl => _rpcNodeUrl;

    public async Task<ApiResponse> BroadcastTxn(byte[] txn)
    {
        try
        {
            var url = $"{_rpcNodeUrl}/broadcast/";
            var payload = new { txn = txn.ToHex() };
            var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, content);
            response.EnsureSuccessStatusCode();
            
            var responseString = await response.Content.ReadAsStringAsync();
            var responseData = JsonConvert.DeserializeObject<ApiResponse>(responseString);

            return new ApiResponse(response.IsSuccessStatusCode, responseData?.Message ?? string.Empty);
        }
        catch (Exception e)
        {
            return new ApiResponse(false, e.Message);
        }
    }
    
    public async Task<ApiResponse<int>> GetNonceOfAddress(string address)
    {
        try
        {
            var url = $"{_rpcNodeUrl}/nonceOfUser/?userAddress={address}";
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var responseData = JsonConvert.DeserializeObject<JObject>(responseContent);
            
            var nonce = responseData["nonce"]?.Value<int>() ?? 0;

            return new ApiResponse<int>(true, "", nonce);
        }
        catch (Exception e)
        {
            return new ApiResponse<int>(false, e.Message);
        }
    }
}