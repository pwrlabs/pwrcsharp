using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Util;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Org.BouncyCastle.Ocsp;
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

    public ulong FeePerByte = 0;
    public string RpcNodeUrl => _rpcNodeUrl;
    private byte ChainId = unchecked((byte)-1);

     public async Task<string> Request(string url)
    {
        try
        {
            HttpResponseMessage response = await _httpClient.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                var responseString = await response.Content.ReadAsStringAsync();
                 if (string.IsNullOrWhiteSpace(responseString)) throw new Exception("The response from the RPC node was empty.");
                return responseString;
            }
            else
            {
                var responseString = await response.Content.ReadAsStringAsync();
                
                var errorMessage = JsonConvert.DeserializeObject<JObject>(responseString)["message"]?.ToString();
                var error = $"Error : {Environment.NewLine} Status Code : {response.StatusCode} {Environment.NewLine} Message : {errorMessage}";
                throw new Exception(errorMessage ?? "Unknown error");          
            }
        }
        catch (HttpRequestException ex)
        {
            throw new Exception("Error fetching data from API.", ex);
        }
    }
    public async Task<string> TestRequest(string url){
    return await Request(url);
    }
    public async Task<byte> GetChainId()
{
    if (ChainId == unchecked((byte)-1))
    {
        string url = $"{_rpcNodeUrl}/chainId/";
        string responseString = await Request(url);

        if (!string.IsNullOrEmpty(responseString))
        {
            JObject responseData = JsonConvert.DeserializeObject<JObject>(responseString);
            JToken chainIdToken = responseData["chainId"];
            
            if (chainIdToken != null)
            {
                ChainId = chainIdToken.Value<byte>();
            }
            else
            {
                throw new Exception("ChainId not found in response.");
            }
        }
        else
        {
            throw new Exception("Empty response received.");
        }
    }
   
    return ChainId;
}
    public async Task<ulong> GetFeePerByte(){
        if(FeePerByte == 0){
       
            var url = $"{_rpcNodeUrl}/feePerByte/";
            string responseString = await Request(url);
            JObject responseData = JsonConvert.DeserializeObject<JObject>(responseString);

            FeePerByte = responseData["feePerByte"]?.Value<ulong>() ?? throw new Exception("Unexpected error Occured.");
        }
        return FeePerByte;
    }
    public async Task<short> GetBlockChainVersion(){
       
            var url = $"{_rpcNodeUrl}/blockchainVersion/";
            string response = await Request(url);
            
            JObject responseData = JsonConvert.DeserializeObject<JObject>(response);

           return responseData["blockchainVersion"]?.Value<short>() ?? throw new Exception("Unexpected error Occured.");

       
    }
    public ulong GetLatestBlockNumber(){
       return GetBlocksCount().Result.Data -1;
    }
    public async Task<List<VmDataTxn>> GetVmDataTxns(ulong startingBlock, ulong endingBlock, ulong vmId){
          
        var url = $"{_rpcNodeUrl}getVmTransactions/?startingBlock={startingBlock}&endingBlock={endingBlock}&vmId={vmId}";
        string responseString = await Request(url);
          
        var responseData = JsonConvert.DeserializeObject<JObject>(responseString);

    
        var vmDataTxnsJson = responseData["transactions"]?.ToString() ?? throw new Exception("The response JSON does not contain 'transactions'.");
        var vmDataTxnList = JsonConvert.DeserializeObject<List<VmDataTxn>>(vmDataTxnsJson);
            
        return vmDataTxnList;
        
    }
    public async Task<List<VmDataTxn>> GetVmDataTxnsFilterByPerBytePrefix(ulong startingBlock, ulong endingBlock, ulong vmId, byte[] prefix){
         
            var url = $"{_rpcNodeUrl}/getVmTransactionsSortByBytePrefix/?startingBlock={startingBlock}&endingBlock={endingBlock}&vmId={vmId}&bytePrefix={BitConverter.ToString(prefix).Replace("-", "").ToLower()}";
            var response = await _httpClient.GetAsync(url);
            
            var responseString = await response.Content.ReadAsStringAsync();

            var responseData = JsonConvert.DeserializeObject<JObject>(responseString);
            var vmDataTxnsJson = responseData["transactions"].ToString();
            var vmDataTxnList = JsonConvert.DeserializeObject<List<VmDataTxn>>(vmDataTxnsJson);
            return vmDataTxnList;
       
    }
    public  async Task<ulong> GetActiveVotingPower()  {
        
            var url = $"{_rpcNodeUrl}/activeVotingPower/";
            var response = await Request(url);

            var responseData = JsonConvert.DeserializeObject<JObject>(response);
           return responseData["activeVotingPower"]?.Value<ulong>() ?? 0;
    }
    public async Task<uint> GetTotalDelegatorsCount()  {
         
            var url = $"{_rpcNodeUrl}/totalDelegatorsCount/";
            var response = await Request(url);
            var responseData = JsonConvert.DeserializeObject<JObject>(response);

           return responseData["totalDelegatorsCount"]?.Value<uint>() ?? 0;

       
    }
    public  async Task<List<Validator>> GetDelegatees(string address)  {
        ValidateAddress(address);
            var url = $"{_rpcNodeUrl}/delegateesOfUser/?userAddress={address}";
            var response = await _httpClient.GetAsync(url);
            var responseString = await response.Content.ReadAsStringAsync();


            var responseData = JsonConvert.DeserializeObject<JObject>(responseString);

          return null;
    }
    public  async Task<Validator> GetValidator(string validatorAddress)  {
        ValidateAddress(validatorAddress);
         
            var url = $"{_rpcNodeUrl}/activeVotingPower/";
            var response = await Request(url);
            var responseData = JsonConvert.DeserializeObject<JObject>(response);

           return null;
        
    }
    public  async Task<ulong> GetDelegatedPWR(string delegatorAddress, string validatorAddress)  {
        ValidateAddress(delegatorAddress);
        ValidateAddress(validatorAddress);
       
            var url = $"{_rpcNodeUrl}validator/delegator/delegatedPWROfAddress/?userAddress={delegatorAddress}&validatorAddress={validatorAddress}";
            var responseString = await Request(url);

            var responseData = JsonConvert.DeserializeObject<JObject>(responseString);

           return responseData["delegatedPWR"]?.Value<ulong>() ?? 0;
    }
    public  async Task<BigDecimal> GetShareValue(string validator)  {
        ValidateAddress(validator);
         
            var url = $"{_rpcNodeUrl}/validator/shareValue/?validatorAddress={validator}";
            var response = await Request(url);
           
            var responseData = JsonConvert.DeserializeObject<JObject>(response);
            string value = responseData["shareValue"]?.Value<string>() ?? "";
           return BigDecimal.Parse(value);
    
    }
    public  async Task<string> GetOwnerOfVm(ulong vmId)  {
      
            var url = $"{_rpcNodeUrl}/ownerOfVmId/?vmId={vmId}";
            var responseString = await Request(url);

            var responseData = JsonConvert.DeserializeObject<JObject>(responseString);

            return responseData["owner"]?.Value<string>() ?? "";
    
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

            var responseData = JsonConvert.DeserializeObject<ApiResponse>(responseString);

            return new ApiResponse(response.IsSuccessStatusCode, responseData?.Message ?? "Success");
        }
        catch (Exception e)
        {
            return new ApiResponse(false, e.Message);
        }
    }
    public async Task<ApiResponse<uint>> GetNonceOfAddress(string address)
    {
        ValidateAddress(address);
        try
        {
            var url = $"{_rpcNodeUrl}/nonceOfUser/?userAddress={address}";
            var response = await _httpClient.GetAsync(url);
            var responseString = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrWhiteSpace(responseString))
                return new ApiResponse<uint>(false, "The response from the RPC node was empty.");

            var responseData = JsonConvert.DeserializeObject<JObject>(responseString);

            var nonce = responseData["nonce"]?.Value<uint>() ?? 0;

            return new ApiResponse<uint>(true, "Success", nonce);
        }
        catch (Exception e)
        {
            return new ApiResponse<uint>(false, e.Message);
        }
    }
    public async Task<ApiResponse<ulong>> GetBalanceOfAddress(string address)
    {
        ValidateAddress(address);
        try
        {
            var url = $"{_rpcNodeUrl}/balanceOf/?userAddress={address}";
            var response = await _httpClient.GetAsync(url);
            var responseString = await response.Content.ReadAsStringAsync();

            if (response.StatusCode != HttpStatusCode.OK)
            {
                var errorMessage = JsonConvert.DeserializeObject<JObject>(responseString)["message"]?.ToString();
                return new ApiResponse<ulong>(false, errorMessage ?? "Unknown error");
            }

            var responseData = JsonConvert.DeserializeObject<JObject>(responseString);
            var balance = responseData["balance"]?.Value<ulong>() ?? throw new Exception("Invalid response from RPC node");

            return new ApiResponse<ulong>(true, "Success", balance);
        }
        catch (Exception e)
        {
            return new ApiResponse<ulong>(false, e.Message);
        }
    }
    public async Task<string> GetGuardianOfAddress(string address)
    {
        ValidateAddress(address);
          var url = $"{_rpcNodeUrl}/guardianOf/?userAddress={address}";
            var response = await _httpClient.GetAsync(url);
            var responseString = await response.Content.ReadAsStringAsync();

            var responseData = JsonConvert.DeserializeObject<JObject>(responseString);

            var isGuarded = responseData["isGuarded"]?.Value<bool>() ?? false; 

           return responseData["guardian"]?.Value<string>() ?? "";     
    }
    public async Task<ApiResponse<ulong>> GetBlocksCount()
    {
        try
        {
            var url = $"{_rpcNodeUrl}/blocksCount/";
            var response = await _httpClient.GetAsync(url);
            var responseString = await response.Content.ReadAsStringAsync();

            if (response.StatusCode != HttpStatusCode.OK)
            {
                var errorMessage = JsonConvert.DeserializeObject<JObject>(responseString)["message"]?.ToString();
                return new ApiResponse<ulong>(false, errorMessage ?? "Unknown error");
            }

            var responseData = JsonConvert.DeserializeObject<JObject>(responseString);
            var blocksCount = responseData["blocksCount"]?.Value<ulong>() ?? throw new Exception("Invalid response from RPC node");

            return new ApiResponse<ulong>(true, "Success", blocksCount);
        }
        catch (Exception e)
        {
            return new ApiResponse<ulong>(false, e.Message);
        }
    }
    
    /// <summary>
/// Retrieves a block by its number from the blockchain.
/// </summary>
/// <param name="blockNumber">The block number to retrieve.</param>
/// <returns>A <see cref="Task{Block}"/> representing the asynchronous operation, with a Block object containing block details if found.</returns>
/// <exception cref="Exception">Thrown when an HTTP error occurs or the response from the RPC node is invalid.</exception>
    public async Task<Block> GetBlockByNumber(uint blockNumber)
    {
            var url = $"{_rpcNodeUrl}/block/?blockNumber={blockNumber}";
            var response = await Request(url);
              var responseObject = JsonConvert.DeserializeObject<JObject>(response);
            var json = responseObject["block"].ToString();
            var jsonBlock = JsonConvert.DeserializeObject<JObject>(json);
            ulong timestamp = jsonBlock.Value<ulong>("timestamp");
           var blockInstance = new Block(
                    transactionCount: jsonBlock.Value<uint>("transactionCount"),
                    size: jsonBlock.Value<uint>("blockSize"),
                    number: jsonBlock.Value<uint>("blockNumber"),
                    reward: jsonBlock.Value<ulong>("blockReward"),
                    timestamp: timestamp,
                    hash: jsonBlock.Value<string>("blockHash"),
                    submitter: jsonBlock.Value<string>("blockSubmitter"),
                    success: jsonBlock.Value<bool>("success"),
                    transactions : jsonBlock["transactions"].Select(t => new Transaction(
                        blockNumber : t.Value<ulong>("blockNumber"),
                        size: t.Value<uint>("size"),
                        hash: t.Value<string>("hash"),
                        fee: t.Value<ulong>("fee"),
                        fromAddress: t.Value<string>("sender"),
                        to: t.Value<string>("receiver"),
                        nonce: t.Value<uint>("nonce"),
                        positionintheBlock: t.Value<uint>("positionInTheBlock"),
                        type: t.Value<string>("type"),
                        value : t.Value<ulong>("value"),
                        timestamp : timestamp
                    )).ToList()
                );
                return blockInstance;
          
    }
    public async Task<ApiResponse<uint>> GetTotalValidatorsCount()
    {
     
            var url = $"{_rpcNodeUrl}/totalValidatorsCount/";
            var response = await Request(url);
            var responseData = JsonConvert.DeserializeObject<JObject>(response);
            var validatorsCount = responseData["validatorsCount"]?.Value<uint>() ?? throw new Exception("Invalid response from RPC node");

            return new ApiResponse<uint>(true, "Success", validatorsCount);
    }
    public async Task<uint> GetStandbyValidatorsCount()
    {
       
            var url = $"{_rpcNodeUrl}/standbyValidatorsCount/";

            var response = await Request(url);

            var responseData = JsonConvert.DeserializeObject<JObject>(response);
            var validatorsCount = responseData["validatorsCount"]?.Value<uint>() ?? throw new Exception("Invalid response from RPC node");

            return validatorsCount;
    }
    public async Task<uint> GetActiveValidatorsCount()
    {
      
            var url = $"{_rpcNodeUrl}/activeValidatorsCount/";

            var response = await Request(url);
            var responseData = JsonConvert.DeserializeObject<JObject>(response);
            var validatorsCount = responseData["validatorsCount"]?.Value<uint>() ?? throw new Exception("Invalid response from RPC node");

            return validatorsCount;
       
    }
    public async Task<List<Validator>> GetAllValidators()
    {
       
            var response = await _httpClient.GetAsync(_rpcNodeUrl + "/allValidators/");
            var responseString = await response.Content.ReadAsStringAsync();

           
                JObject responseData = JsonConvert.DeserializeObject<JObject>(responseString);
                 var validatorsString = responseData["validators"].ToString();
                JArray validatorsArray = JsonConvert.DeserializeObject<JArray>(validatorsString);
                 List<Validator> validators = new List<Validator>();
                 
                 foreach(var token in validatorsArray){
                
                   
                   string address = token["address"].Value<string>();
                   
                   ulong votingPower = token["votingPower"]?.Value<ulong>() ?? 0;
                   var val = new Validator(
                    address : address,
                    ip : token["ip"]?.Value<string>() ?? "",
                    badActor : token["badActor"]?.Value<bool>() ?? false,
                    votingPower : token["votingPower"]?.Value<ulong>() ?? 0,
                    shares : token["totalShares"]?.Value<ulong>() ?? 0,
                    delegatorsCount : token["delegatorsCount"]?.Value<uint>() ?? 0,
                    status : token["status"]?.Value<string>() ?? "active",
                    httpClient : _httpClient
                   /* delegators : token["delegators"].Select(t => new Delegator(
                        address :  "0x" + address,
                        validatorAddress : address,
                        delegatedPwr : t["totalShares"]?.Value<ulong>() ?? 0 * votingPower,
                        shares : 0
                    )).ToList()*/
                   );
                   validators.Add(val);
                 }
                   
               
                return validators;
            
          
    }
    public async Task<List<Validator>> GetStandbyValidators()
    {
       
            var url = _rpcNodeUrl + "/standbyValidators/";
            var response = await Request(url);

       
                var responseData = JsonConvert.DeserializeObject<JObject>(response);
                 var validatorsString = responseData["validators"].ToString();
                  JArray validatorsArray = JsonConvert.DeserializeObject<JArray>(validatorsString);
                 List<Validator> validators = new List<Validator>();
                 
                 foreach(var token in validatorsArray){
                
                   
                   string address = token["address"].Value<string>();
                   
                   ulong votingPower = token["votingPower"]?.Value<ulong>() ?? 0;
                   var val = new Validator(
                    address : address,
                    ip : token["ip"]?.Value<string>() ?? "",
                    badActor : token["badActor"]?.Value<bool>() ?? false,
                    votingPower : token["votingPower"]?.Value<ulong>() ?? 0,
                    shares : token["totalShares"]?.Value<ulong>() ?? 0,
                    delegatorsCount : token["delegatorsCount"]?.Value<uint>() ?? 0,
                    status : token["status"]?.Value<string>() ?? "active",
                    httpClient : _httpClient
                   /* delegators : token["delegators"].Select(t => new Delegator(
                        address :  "0x" + address,
                        validatorAddress : address,
                        delegatedPwr : t["totalShares"]?.Value<ulong>() ?? 0 * votingPower,
                        shares : 0
                    )).ToList()*/
                   );
                   validators.Add(val);
                 }
                   
               
                return validators;
           
    }
    public async Task<List<Validator>> GetActiveValidators()
    {
        
            var url = _rpcNodeUrl + "/activeValidators/";
            var response = await Request(url);

           
               var responseData = JsonConvert.DeserializeObject<JObject>(response);
                 var validatorsString = responseData["validators"].ToString();
                JArray validatorsArray = JsonConvert.DeserializeObject<JArray>(validatorsString);
                 List<Validator> validators = new List<Validator>();
                 
                 foreach(var token in validatorsArray){
                
                   
                   string address = token["address"].Value<string>();
                   
                   ulong votingPower = token["votingPower"]?.Value<ulong>() ?? 0;
                   var val = new Validator(
                    address : address,
                    ip : token["ip"]?.Value<string>() ?? "",
                    badActor : token["badActor"]?.Value<bool>() ?? false,
                    votingPower : token["votingPower"]?.Value<ulong>() ?? 0,
                    shares : token["totalShares"]?.Value<ulong>() ?? 0,
                    delegatorsCount : token["delegatorsCount"]?.Value<uint>() ?? 0,
                    status : token["status"]?.Value<string>() ?? "active",
                    httpClient : _httpClient
                   /* delegators : token["delegators"].Select(t => new Delegator(
                        address :  "0x" + address,
                        validatorAddress : address,
                        delegatedPwr : t["totalShares"]?.Value<ulong>() ?? 0 * votingPower,
                        shares : 0
                    )).ToList()*/
                   );
                   validators.Add(val);
                 }
                   
               
                return validators;
           
    }
    public async Task<string> GetOwnerOfVm(uint vmId)
    {
       
            var url = $"{_rpcNodeUrl}/ownerOfVmId/?vmId={vmId}";
            var response = await Request(url);
            
            var data = JsonConvert.DeserializeObject<JObject>(response);
        return data["owner"]?.ToString() ?? throw new Exception("Invalid response from RPC node, owner is null");
            
    }

    /// <summary>
/// Updates the fee per byte on the blockchain.
/// </summary>
/// <returns>A <see cref="Task{ulong}"/> representing the asynchronous operation, with the new fee per byte value.</returns>
/// <exception cref="Exception">Thrown when an HTTP error occurs or the response from the RPC node is invalid.</exception>
    public async Task UpdateFeePerByte()
    {
            var url = $"{_rpcNodeUrl}/feePerByte/";
            var response = await Request(url);

            var data = JsonConvert.DeserializeObject<JObject>(response);
            FeePerByte = data["feePerByte"]?.Value<uint>() ?? throw new Exception("Invalid response from RPC node");
           
       
    }

     public void ValidateAddress(string address)
    {
        if (string.IsNullOrEmpty(address))
        {
            throw new ArgumentException("Address cannot be null or empty.");
        }
        if(address.Length != 42){
            throw new ArgumentException("Invalid address format.");
        }

        string pattern = @"^0x[0-9a-fA-F]{40}$";

        if (!Regex.IsMatch(address, pattern))
        {
            throw new ArgumentException("Invalid address format.");
        }
    }   

    
}