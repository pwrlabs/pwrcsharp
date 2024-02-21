using System.Net;
using System.Text;
using System.Text.Json;
using Nethereum.Hex.HexConvertors.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PWRCS.Models;

namespace PWRCS;

public class PwrApiSdk
{
    private readonly string _rpcNodeUrl;
    private readonly HttpClient _httpClient;

    public PwrApiSdk(string rpcNodeUrl, HttpClient? httpClient = null)
    {
        _rpcNodeUrl = rpcNodeUrl ?? throw new InvalidOperationException("RPC Node URL cannot be null");
        _httpClient = httpClient ?? new HttpClient();
    }

    public long FeePerByte = 0;
    public string RpcNodeUrl => _rpcNodeUrl;
    private byte ChainId = unchecked((byte)-1);

    public async Task<byte> GetChainId(){
        if(ChainId ==  unchecked((byte)-1)){
           try
        {
            var url = $"{_rpcNodeUrl}/chainId/";
            var response = await _httpClient.GetAsync(url);
            var responseString = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrWhiteSpace(responseString))
                throw new Exception("The response from the RPC node was empty.");

            var responseData = JsonConvert.DeserializeObject<JObject>(responseString);

            ChainId = responseData["chainId"]?.Value<byte>() ?? unchecked((byte)-1);

        }
        catch (Exception e)
        {
            
           throw new Exception($"Error retriving data {e.Message}" );
        }
        }
        return ChainId;
    }
    public async Task<long> GetFeePerByte(){
        if(FeePerByte == 0){
  try
        {
            var url = $"{_rpcNodeUrl}/feePerByte/";
            var response = await _httpClient.GetAsync(url);
            var responseString = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrWhiteSpace(responseString))
                throw new Exception("The response from the RPC node was empty.");

            var responseData = JsonConvert.DeserializeObject<JObject>(responseString);

            FeePerByte = responseData["feePerByte"]?.Value<long>() ?? 0;

        }
        catch (Exception e)
        {
            
          throw new Exception($"Error retriving data {e.Message}" );
        }
        }
        return FeePerByte;
    }
    public async Task<short> GetBlockChainVersion(){
        try
        {
            var url = $"{_rpcNodeUrl}/blockchainVersion/";
            var response = await _httpClient.GetAsync(url);
            var responseString = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrWhiteSpace(responseString))
                throw new Exception("The response from the RPC node was empty.");

            var responseData = JsonConvert.DeserializeObject<JObject>(responseString);

           return responseData["blockChainVersion"]?.Value<short>() ?? 0;

        }
        catch (Exception e)
        {
            
          throw new Exception($"Error retriving data {e.Message}" );
        }
    }
    public long GetLatestBlockNumber(){
       return GetBlocksCount().Result.Data -1;
    }
    public async Task<List<VmDataTxn>> GetVmDataTxns(ulong startingBlock, ulong endingBlock, ulong vmId){
          
            var url = $"{_rpcNodeUrl}getVmTransactions/?startingBlock={startingBlock}&endingBlock={endingBlock}&vmId={vmId}";
            var response = await _httpClient.GetAsync(url);
            var responseString = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrWhiteSpace(responseString))
                throw new Exception("The response from the RPC node was empty.");

            var responseData = JsonConvert.DeserializeObject<JObject>(responseString);

            if (responseData["transactions"] == null)
        {
            throw new Exception("The response JSON does not contain 'transactions'.");
        }
        var vmDataTxnsJson = responseData["transactions"].ToString();
            var vmDataTxnList = JsonConvert.DeserializeObject<List<VmDataTxn>>(vmDataTxnsJson);
            
                return vmDataTxnList;

        
    }
    public async Task<List<VmDataTxn>> GetVmDataTxnsFilterByPerBytePrefix(long startingBlock, long endingBlock, long vmId, byte[] prefix){
          try
        {
            var url = $"{_rpcNodeUrl}/getVmTransactionsSortByBytePrefix/?startingBlock={startingBlock}&endingBlock={endingBlock}&vmId={vmId}&bytePrefix={BitConverter.ToString(prefix).Replace("-", "").ToLower()}";
            var response = await _httpClient.GetAsync(url);
            var responseString = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrWhiteSpace(responseString))
                throw new Exception("The response from the RPC node was empty.");

            var responseData = JsonConvert.DeserializeObject<JObject>(responseString);
            var vmDataTxnsJson = responseData["transactions"].ToString();
            var vmDataTxnList = JsonConvert.DeserializeObject<List<VmDataTxn>>(vmDataTxnsJson);
                return vmDataTxnList;

        }
        catch (Exception e)
        {
            
          throw new Exception($"Error retriving data {e.Message}" );
        }
    }
    public  async Task<long> GetActiveVotingPower()  {
           try
        {
            var url = $"{_rpcNodeUrl}/activeVotingPower/";
            var response = await _httpClient.GetAsync(url);
            var responseString = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrWhiteSpace(responseString))
                throw new Exception("The response from the RPC node was empty.");

            var responseData = JsonConvert.DeserializeObject<JObject>(responseString);

           return responseData["activeVotingPower"]?.Value<long>() ?? 0;

        }
        catch (Exception e)
        {
            
          throw new Exception($"Error retriving data {e.Message}" );
        }
    }
    public async Task<int> GetTotalDelegatorsCount()  {
           try
        {
            var url = $"{_rpcNodeUrl}/activeVotingPower/";
            var response = await _httpClient.GetAsync(url);
            var responseString = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrWhiteSpace(responseString))
                throw new Exception("The response from the RPC node was empty.");

            var responseData = JsonConvert.DeserializeObject<JObject>(responseString);

           return responseData["totalDelegatorsCount"]?.Value<int>() ?? 0;

        }
        catch (Exception e)
        {
            
          throw new Exception($"Error retriving data {e.Message}" );
        }
    }
    public  async Task<long> GetDelegatees()  {
           try
        {
            var url = $"{_rpcNodeUrl}/activeVotingPower/";
            var response = await _httpClient.GetAsync(url);
            var responseString = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrWhiteSpace(responseString))
                throw new Exception("The response from the RPC node was empty.");

            var responseData = JsonConvert.DeserializeObject<JObject>(responseString);

           return responseData["activeVotingPower"]?.Value<long>() ?? 0;

        }
        catch (Exception e)
        {
            
          throw new Exception($"Error retriving data {e.Message}" );
        }
    }
    public  async Task<Validator> GetValidator(string validatorAddress)  {
           try
        {
            var url = $"{_rpcNodeUrl}/activeVotingPower/";
            var response = await _httpClient.GetAsync(url);
            var responseString = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrWhiteSpace(responseString))
                throw new Exception("The response from the RPC node was empty.");

            var responseData = JsonConvert.DeserializeObject<JObject>(responseString);

           return null;

        }
        catch (Exception e)
        {
            
          throw new Exception($"Error retriving data {e.Message}" );
        }
    }
    public  async Task<long> GetDelegatedPWR(String delegatorAddress, String validatorAddress)  {
           try
        {
            var url = $"{_rpcNodeUrl}validator/delegator/delegatedPWROfAddress/?userAddress={delegatorAddress}&validatorAddress={validatorAddress}";
            var response = await _httpClient.GetAsync(url);
            var responseString = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrWhiteSpace(responseString))
                throw new Exception("The response from the RPC node was empty.");

            var responseData = JsonConvert.DeserializeObject<JObject>(responseString);

           return responseData["delegatedPWR"]?.Value<long>() ?? 0;

        }
        catch (Exception e)
        {
            
          throw new Exception($"Error retriving data {e.Message}" );
        }
    }
    public  async Task<decimal> GetShareValue(string validator)  {
           try
        {
            var url = $"{_rpcNodeUrl}/validator/shareValue/?validatorAddress={validator}    ";
            var response = await _httpClient.GetAsync(url);
            var responseString = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrWhiteSpace(responseString))
                throw new Exception("The response from the RPC node was empty.");

            var responseData = JsonConvert.DeserializeObject<JObject>(responseString);

           return responseData["shareValue"]?.Value<decimal>() ?? 0;


        }
        catch (Exception e)
        {
            
          throw new Exception($"Error retriving data {e.Message}" );
        }
    }
    public  async Task<string> GetOwnerOfVm(long vmId)  {
           try
        {
            var url = $"{_rpcNodeUrl}/ownerOfVmId/?vmId={vmId}";
            var response = await _httpClient.GetAsync(url);
            var responseString = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrWhiteSpace(responseString))
                throw new Exception("The response from the RPC node was empty.");

            var responseData = JsonConvert.DeserializeObject<JObject>(responseString);

            return responseData["owner"]?.Value<string>() ?? "";

        }
        catch (Exception e)
        {
            
          throw new Exception($"Error retriving data {e.Message}" );
        }
    }
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
                        blockNumber : t.Value<long>("blockNumber"),
                        size: t.Value<int>("size"),
                        hash: t.Value<string>("hash"),
                        fee: t.Value<long>("fee"),
                        fromAddress: t.Value<string>("from"),
                        to: t.Value<string>("to"),
                        nonce: t.Value<int>("nonce"),
                        positionInTheBlock: t.Value<int>("positionInTheBlock"),
                        type: t.Value<string>("type"),
                        timestamp : DateTime.Now.Ticks
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
    
    public async Task<ApiResponse<int>> GetStandbyValidatorsCount()
    {
        try
        {
            var url = $"{_rpcNodeUrl}/standbyValidatorsCount/";

            var response = await _httpClient.GetAsync(url);
            var responseString = await response.Content.ReadAsStringAsync();

            if (response.StatusCode != HttpStatusCode.OK)
            {
                var errorMessage = JsonConvert.DeserializeObject<JObject>(responseString)["message"]?.ToString();
                return new ApiResponse<int>(false, errorMessage ?? "Unknown error");
            }

            var responseData = JsonConvert.DeserializeObject<JObject>(responseString);
            var validatorsCount = responseData["validatorsCount"]?.Value<int>() ?? throw new Exception("Invalid response from RPC node");

            return new ApiResponse<int>(true, responseData["message"]?.ToString() ?? "Success", validatorsCount);
        }
        catch (Exception e)
        {
            return new ApiResponse<int>(false, e.Message);
        }
    }
    
    public async Task<ApiResponse<int>> GetActiveValidatorsCount()
    {
        try
        {
            var url = $"{_rpcNodeUrl}/activeValidatorsCount/";

            var response = await _httpClient.GetAsync(url);
            var responseString = await response.Content.ReadAsStringAsync();

            if (response.StatusCode != HttpStatusCode.OK)
            {
                var errorMessage = JsonConvert.DeserializeObject<JObject>(responseString)["message"]?.ToString();
                return new ApiResponse<int>(false, errorMessage ?? "Unknown error");
            }

            var responseData = JsonConvert.DeserializeObject<JObject>(responseString);
            var validatorsCount = responseData["validatorsCount"]?.Value<int>() ?? throw new Exception("Invalid response from RPC node");

            return new ApiResponse<int>(true, responseData["validatorsCount"]?.ToString() ?? "Success", validatorsCount);
        }
        catch (Exception e)
        {
            return new ApiResponse<int>(false, e.Message);
        }
    }
    
    public async Task<List<Validator>> GetAllValidators()
    {
        try
        {
            var response = await _httpClient.GetAsync(_rpcNodeUrl + "/allValidators/");
            var responseString = await response.Content.ReadAsStringAsync();

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var data = JsonConvert.DeserializeObject<JObject>(responseString);
                var validators = data["validators"].ToObject<List<JObject>>();
                var validatorsList = new List<Validator>();

                foreach (var validatorData in validators)
                {
                    var validator = new Validator(
                        "0x" + validatorData["address"],
                        validatorData["ip"]?.ToString() ?? throw new Exception("Invalid response from RPC node, ip is null"),
                        validatorData["badActor"]?.ToObject<bool>() ?? throw new Exception("Invalid response from RPC node, badActor is null"),
                        validatorData["votingPower"]?.ToObject<decimal>() ?? throw new Exception("Invalid response from RPC node, votingPower is null"),
                        validatorData["totalShares"]?.ToObject<decimal>() ?? throw new Exception("Invalid response from RPC node, totalShares is null"),
                        validatorData["delegatorsCount"]?.ToObject<int>() ?? throw new Exception("Invalid response from RPC node, delegatorsCount is null"),
                        validatorData["status"]?.ToString() ?? throw new Exception("Invalid response from RPC node, status is null"),
                        _httpClient
                    );
                    validatorsList.Add(validator);
                }

                return validatorsList;
            }
            else
            {
                var errorMessage = JsonConvert.DeserializeObject<JObject>(responseString)["message"]?.ToString();
                throw new Exception(errorMessage ?? "Unknown error");
            }
        }
        catch (Exception e)
        {
            throw new Exception($"Failed to get all validators: {e.Message}");
        }
    }
    
    public async Task<List<Validator>> GetStandbyValidators()
    {
        try
        {
            var url = _rpcNodeUrl + "/standbyValidators/";
            var response = await _httpClient.GetAsync(url);
            var responseString = await response.Content.ReadAsStringAsync();

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var data = JsonConvert.DeserializeObject<JObject>(responseString);
                var validators = data["validators"].ToObject<List<JObject>>();
                var validatorsList = new List<Validator>();

                foreach (var validatorData in validators)
                {
                    var validator = new Validator(
                        "0x" + validatorData["address"] ?? throw new Exception("Invalid response from RPC node, address is null"),
                        validatorData["ip"]?.ToString() ?? throw new Exception("Invalid response from RPC node, ip is null"),
                        validatorData["badActor"]?.ToObject<bool>() ?? throw new Exception("Invalid response from RPC node, badActor is null"),
                        validatorData["votingPower"]?.ToObject<decimal>() ?? throw new Exception("Invalid response from RPC node, votingPower is null"),
                        validatorData["totalShares"]?.ToObject<decimal>() ?? throw new Exception("Invalid response from RPC node, totalShares is null"),
                        validatorData["delegatorsCount"]?.ToObject<int>() ?? throw new Exception("Invalid response from RPC node, delegatorsCount is null"),
                        validatorData["status"]?.ToString() ?? throw new Exception("Invalid response from RPC node, status is null"),
                        _httpClient
                    );
                    validatorsList.Add(validator);
                }

                return validatorsList;
            }
            else
            {
                var errorMessage = JsonConvert.DeserializeObject<JObject>(responseString)["message"]?.ToString();
                throw new Exception(errorMessage ?? "Unknown error");
            }
        }
        catch (Exception e)
        {
            throw new Exception($"Failed to get standby validators: {e.Message}");
        }
    }
    
    public async Task<List<Validator>> GetActiveValidators()
    {
        try
        {
            var url = _rpcNodeUrl + "/activeValidators/";
            var response = await _httpClient.GetAsync(url);
            var responseString = await response.Content.ReadAsStringAsync();

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var data = JsonConvert.DeserializeObject<JObject>(responseString);
                var validators = data["validators"].ToObject<List<JObject>>();
                var validatorsList = new List<Validator>();

                foreach (var validatorData in validators)
                {
                    var validator = new Validator(
                        "0x" + validatorData["address"] ?? throw new Exception("Invalid response from RPC node, address is null"),
                        validatorData["ip"]?.ToString() ?? throw new Exception("Invalid response from RPC node, ip is null"),
                        validatorData["badActor"]?.ToObject<bool>() ?? throw new Exception("Invalid response from RPC node, badActor is null"),
                        validatorData["votingPower"]?.ToObject<decimal>() ?? throw new Exception("Invalid response from RPC node, votingPower is null"),
                        validatorData["totalShares"]?.ToObject<decimal>() ?? throw new Exception("Invalid response from RPC node, totalShares is null"),
                        validatorData["delegatorsCount"]?.ToObject<int>() ?? throw new Exception("Invalid response from RPC node, delegatorsCount is null"),
                        string.Empty,
                        _httpClient
                    );
                    validatorsList.Add(validator);
                }

                return validatorsList;
            }
            else
            {
                var errorMessage = JsonConvert.DeserializeObject<JObject>(responseString)["message"]?.ToString();
                throw new Exception(errorMessage ?? "Unknown error");
            }
        }
        catch (Exception e)
        {
            throw new Exception($"Failed to get standby validators: {e.Message}");
        }
    }
    
    public async Task<string> GetOwnerOfVm(int vmId)
    {
        try
        {
            var url = $"{_rpcNodeUrl}/ownerOfVmId/?vmId={vmId}";
            var response = await _httpClient.GetAsync(url);
            var responseString = await response.Content.ReadAsStringAsync();

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var data = JsonConvert.DeserializeObject<JObject>(responseString);
                return data["owner"]?.ToString() ?? throw new Exception("Invalid response from RPC node, owner is null");
            }
            else
            {
                var errorMessage = JsonConvert.DeserializeObject<JObject>(responseString)["message"]?.ToString();
                throw new Exception(errorMessage ?? "Unknown error");
            }
        }
        catch (Exception e)
        {
            throw new Exception($"Failed to get owner of VM: {e.Message}");
        }
    }
    
    public async Task<long> UpdateFeePerByte()
    {
        try
        {
            var url = $"{_rpcNodeUrl}/feePerByte/";
            var response = await _httpClient.GetAsync(url);
            var responseString = await response.Content.ReadAsStringAsync();

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var data = JsonConvert.DeserializeObject<JObject>(responseString);
                FeePerByte = data["feePerByte"]?.Value<int>() ?? throw new Exception("Invalid response from RPC node");
                return FeePerByte;
            }
            else
            {
                var errorMessage = JsonConvert.DeserializeObject<JObject>(responseString)["message"]?.ToString();
                throw new Exception(errorMessage ?? "Unknown error");
            }
        }
        catch (Exception e)
        {
            throw new Exception($"Failed to update fee per byte: {e.Message}");
        }
    }

   

    
}