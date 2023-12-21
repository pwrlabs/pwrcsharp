using System.Net;
using System.Text;
using System.Text.Json;
using Nethereum.Hex.HexConvertors.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PWRPY.Models;

namespace PWRPY;

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
            var responseString = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrWhiteSpace(responseString))
                return new ApiResponse(false, "The response from the RPC node was empty.");

            var responseData = JsonConvert.DeserializeObject<ApiResponse>(responseString);

            return new ApiResponse(response.IsSuccessStatusCode, responseData?.Message ?? "Success");
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
            var responseString = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrWhiteSpace(responseString))
                return new ApiResponse<int>(false, "The response from the RPC node was empty.");

            var responseData = JsonConvert.DeserializeObject<JObject>(responseString);

            var nonce = responseData["nonce"]?.Value<int>() ?? 0;

            return new ApiResponse<int>(true, "Success", nonce);
        }
        catch (Exception e)
        {
            return new ApiResponse<int>(false, e.Message);
        }
    }

    public async Task<ApiResponse<decimal>> GetBalanceOfAddress(string address)
    {
        try
        {
            var url = $"{_rpcNodeUrl}/balanceOf/?userAddress={address}";
            var response = await _httpClient.GetAsync(url);
            var responseString = await response.Content.ReadAsStringAsync();

            if (response.StatusCode != HttpStatusCode.OK)
            {
                var errorMessage = JsonConvert.DeserializeObject<JObject>(responseString)["message"]?.ToString();
                return new ApiResponse<decimal>(false, errorMessage ?? "Unknown error");
            }

            var responseData = JsonConvert.DeserializeObject<JObject>(responseString);
            var balance = responseData["balance"]?.Value<decimal>() ?? throw new Exception("Invalid response from RPC node");

            return new ApiResponse<decimal>(true, "Success", balance);
        }
        catch (Exception e)
        {
            return new ApiResponse<decimal>(false, e.Message);
        }
    }

    public async Task<ApiResponse<int>> GetBlocksCount()
    {
        try
        {
            var url = $"{_rpcNodeUrl}/blocksCount/";
            var response = await _httpClient.GetAsync(url);
            var responseString = await response.Content.ReadAsStringAsync();

            if (response.StatusCode != HttpStatusCode.OK)
            {
                var errorMessage = JsonConvert.DeserializeObject<JObject>(responseString)["message"]?.ToString();
                return new ApiResponse<int>(false, errorMessage ?? "Unknown error");
            }

            var responseData = JsonConvert.DeserializeObject<JObject>(responseString);
            var blocksCount = responseData["blocksCount"]?.Value<int>() ?? throw new Exception("Invalid response from RPC node");

            return new ApiResponse<int>(true, "Success", blocksCount);
        }
        catch (Exception e)
        {
            return new ApiResponse<int>(false, e.Message);
        }
    }

    public async Task<Block> GetBlockByNumber(int blockNumber)
    {
        try
        {
            var url = $"{_rpcNodeUrl}/block/?blockNumber={blockNumber}";
            var response = await _httpClient.GetAsync(url);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var responseString = await response.Content.ReadAsStringAsync();
                var jsonBlock = JObject.Parse(responseString)["block"];

                var blockInstance = new Block(
                    transactionCount: jsonBlock.Value<int>("transactionCount"),
                    size: jsonBlock.Value<int>("blockSize"),
                    number: jsonBlock.Value<int>("blockNumber"),
                    reward: jsonBlock.Value<decimal>("blockReward"),
                    timestamp: jsonBlock.Value<long>("timestamp"),
                    hash: jsonBlock.Value<string>("blockHash"),
                    submitter: jsonBlock.Value<string>("blockSubmitter"),
                    success: jsonBlock.Value<bool>("success"),
                    transactions: jsonBlock["transactions"].Select(t => new Transaction(
                        size: t.Value<int>("size"),
                        hash: t.Value<string>("hash"),
                        fee: t.Value<decimal>("fee"),
                        fromAddress: t.Value<string>("from"),
                        to: t.Value<string>("to"),
                        nonceOrValidationHash: t.Value<string>("nonceOrValidationHash"),
                        positionInTheBlock: t.Value<int>("positionInTheBlock"),
                        type: t.Value<string>("type")
                    )).ToList()
                );

                return blockInstance;
            }
            else if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                var errorData = JObject.Parse(await response.Content.ReadAsStringAsync());
                throw new Exception($"Failed with HTTP error 400 and message: {errorData["message"]}");
            }
            else
            {
                throw new Exception($"Failed with HTTP error code: {response.StatusCode}");
            }
        }
        catch (HttpRequestException httpErr)
        {
            throw new Exception($"HTTP error occurred: {httpErr.Message}");
        }
        catch (Exception err)
        {
            throw new Exception($"An error occurred: {err.Message}");
        }
    }
    
    public async Task<ApiResponse<int>> GetTotalValidatorsCount()
    {
        try
        {
            var url = $"{_rpcNodeUrl}/totalValidatorsCount/";
            var response = await _httpClient.GetAsync(url);
            var responseString = await response.Content.ReadAsStringAsync();

            if (response.StatusCode != HttpStatusCode.OK)
            {
                var errorMessage = JsonConvert.DeserializeObject<JObject>(responseString)["message"]?.ToString();
                return new ApiResponse<int>(false, errorMessage ?? "Unknown error");
            }

            var responseData = JsonConvert.DeserializeObject<JObject>(responseString);
            var validatorsCount = responseData["validatorsCount"]?.Value<int>() ?? throw new Exception("Invalid response from RPC node");

            return new ApiResponse<int>(true, "Success", validatorsCount);
        }
        catch (Exception e)
        {
            return new ApiResponse<int>(false, e.Message);
        }
    }
}