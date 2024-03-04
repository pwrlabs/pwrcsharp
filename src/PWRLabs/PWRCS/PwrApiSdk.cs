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
/// <summary>
/// Provides methods to interact with the PWR blockchain via RPC calls.
/// </summary>
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


     /// <summary>
    /// Makes a generic HTTP GET request to the specified URL.
    /// </summary>
    /// <param name="url">The URL to send the request to.</param>
    /// <returns>A string containing the response from the server.</returns>
    /// <exception cref="Exception">Thrown when an error occurs during the HTTP request.</exception>
     public async Task<string> Request(string url)
    {
        try
        {
            HttpResponseMessage response = await _httpClient.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("success");
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
    /// <summary>
    /// Retrieves the chain ID of the blockchain.
    /// </summary>
    /// <returns>The chain ID.</returns>
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
    /// <summary>
    /// Retrieves the fee per byte for transactions on the blockchain.
    /// </summary>
    /// <returns>The fee per byte.</returns>
    public async Task<ulong> GetFeePerByte(){
        if(FeePerByte == 0){
       
            var url = $"{_rpcNodeUrl}/feePerByte/";
            string responseString = await Request(url);
            JObject responseData = JsonConvert.DeserializeObject<JObject>(responseString);

            FeePerByte = responseData["feePerByte"]?.Value<ulong>() ?? throw new Exception("Unexpected error Occured.");
        }
        return FeePerByte;
    }
    /// <summary>
    /// Retrieves the version of the blockchain.
    /// </summary>
    /// <returns>The version of the blockchain.</returns>
    public async Task<short> GetBlockChainVersion(){
       
            var url = $"{_rpcNodeUrl}/blockchainVersion/";
            string response = await Request(url);
            
            JObject responseData = JsonConvert.DeserializeObject<JObject>(response);

           return responseData["blockchainVersion"]?.Value<short>() ?? throw new Exception("Unexpected error Occured.");

       
    }
    /// <summary>
    /// Retrieves the number of the latest block in the blockchain.
    /// </summary>
    /// <returns>The number of the latest block.</returns>
    public async Task<ulong> GetLatestBlockNumber(){
       return await GetBlocksCount() - (ulong)1;
    }
    /// <summary>
    /// Retrieves a list of virtual machine data transactions within the specified block range and virtual machine ID.
    /// </summary>
    /// <param name="startingBlock">The starting block number of the range.</param>
    /// <param name="endingBlock">The ending block number of the range.</param>
    /// <param name="vmId">The ID of the virtual machine.</param>
    /// <returns>A list of <see cref="VmDataTxn"/> representing the virtual machine data transactions.</returns>
    /// <exception cref="Exception">Thrown when an error occurs during the HTTP request or the response JSON does not contain 'transactions'.</exception>   
    public async Task<List<VmDataTxn>> GetVmDataTxns(ulong startingBlock, ulong endingBlock, ulong vmId){
          
        var url = $"{_rpcNodeUrl}getVmTransactions/?startingBlock={startingBlock}&endingBlock={endingBlock}&vmId={vmId}";
        string responseString = await Request(url);
          
        var responseData = JsonConvert.DeserializeObject<JObject>(responseString);

    
        var vmDataTxnsJson = responseData["transactions"]?.ToString() ?? throw new Exception("The response JSON does not contain 'transactions'.");
        var vmDataTxnList = JsonConvert.DeserializeObject<List<VmDataTxn>>(vmDataTxnsJson);
            
        return vmDataTxnList;
        
    }
    /// <summary>
    /// Retrieves a list of virtual machine data transactions within the specified block range, virtual machine ID, and byte prefix filter.
    /// </summary>
    /// <param name="startingBlock">The starting block number of the range.</param>
    /// <param name="endingBlock">The ending block number of the range.</param>
    /// <param name="vmId">The ID of the virtual machine.</param>
    /// <param name="prefix">The byte prefix filter.</param>
    /// <returns>A list of <see cref="VmDataTxn"/> representing the virtual machine data transactions.</returns>
    public async Task<List<VmDataTxn>> GetVmDataTxnsFilterByPerBytePrefix(ulong startingBlock, ulong endingBlock, ulong vmId, byte[] prefix){
         
            var url = $"{_rpcNodeUrl}/getVmTransactionsSortByBytePrefix/?startingBlock={startingBlock}&endingBlock={endingBlock}&vmId={vmId}&bytePrefix={BitConverter.ToString(prefix).Replace("-", "").ToLower()}";
            var response = await _httpClient.GetAsync(url);
            
            var responseString = await response.Content.ReadAsStringAsync();

            var responseData = JsonConvert.DeserializeObject<JObject>(responseString);
            var vmDataTxnsJson = responseData["transactions"].ToString();
            var vmDataTxnList = JsonConvert.DeserializeObject<List<VmDataTxn>>(vmDataTxnsJson);
            return vmDataTxnList;
       
    }
    /// <summary>
    /// Retrieves the active voting power on the blockchain.
    /// </summary>
    /// <returns>The active voting power.</returns>
    public  async Task<ulong> GetActiveVotingPower()  {
        
            var url = $"{_rpcNodeUrl}/activeVotingPower/";
            var response = await Request(url);

            var responseData = JsonConvert.DeserializeObject<JObject>(response);
           return responseData["activeVotingPower"]?.Value<ulong>() ?? 0;
    }
    /// <summary>
    /// Retrieves the total count of delegators on the blockchain.
    /// </summary>
    /// <returns>The total count of delegators.</returns>
    public async Task<uint> GetTotalDelegatorsCount()  {
         
            var url = $"{_rpcNodeUrl}/totalDelegatorsCount/";
            var response = await Request(url);
            var responseData = JsonConvert.DeserializeObject<JObject>(response);

           return responseData["totalDelegatorsCount"]?.Value<uint>() ?? 0;

       
    }
    /// <summary>
    /// Retrieves the list of delegatees of the specified user address.
    /// </summary>
    /// <param name="address">The address of the user.</param>
    /// <returns>A list of <see cref="Validator"/> representing the delegatees.</returns>
    public  async Task<List<Validator>> GetDelegatees(string address)  {
        ValidateAddress(address);
            var url = $"{_rpcNodeUrl}/delegateesOfUser/?userAddress={address}";
            var response = await Request(url);
            var responseData = JsonConvert.DeserializeObject<JObject>(response);
            var tk = JsonConvert.DeserializeObject<JArray>(responseData["delegatees"].ToString());
            List<Validator> validators = new List<Validator>();
            foreach(var token in tk){
            var validator = new Validator(
                    address : token["address"].Value<string>(),
                    ip : token["ip"]?.Value<string>() ?? "",
                    badActor : token["badActor"]?.Value<bool>() ?? false,
                    votingPower : token["votingPower"]?.Value<ulong>() ?? 0,
                    shares : token["totalShares"]?.Value<ulong>() ?? 0,
                    delegatorsCount : token["delegatorsCount"]?.Value<uint>() ?? 0,
                    status : token["status"]?.Value<string>() ?? "active",
                    httpClient : _httpClient
            );
            validators.Add(validator);
            }
          return validators;
    }
    /// <summary>
    /// Retrieves the validator information for the specified validator address.
    /// </summary>
    /// <param name="validatorAddress">The address of the validator.</param>
    /// <returns>The <see cref="Validator"/> object representing the validator.</returns>
    public  async Task<Validator> GetValidator(string validatorAddress)  {
        ValidateAddress(validatorAddress);
         
            var url = $"{_rpcNodeUrl}/validator/?validatorAddress={validatorAddress}";
            var response = await Request(url);
            var responseData = JsonConvert.DeserializeObject<JObject>(response);
            var token = JsonConvert.DeserializeObject<JObject>(responseData["validator"].ToString());
            var validator = new Validator(
                    address : token["address"].Value<string>(),
                    ip : token["ip"]?.Value<string>() ?? "",
                    badActor : token["badActor"]?.Value<bool>() ?? false,
                    votingPower : token["votingPower"]?.Value<ulong>() ?? 0,
                    shares : token["totalShares"]?.Value<ulong>() ?? 0,
                    delegatorsCount : token["delegatorsCount"]?.Value<uint>() ?? 0,
                    status : token["status"]?.Value<string>() ?? "active",
                    httpClient : _httpClient
            );
           return validator;
        
    }

    /// <summary>
    /// Retrieves the delegated PWR (Power) for the specified delegator and validator addresses.
    /// </summary>
    /// <param name="delegatorAddress">The address of the delegator.</param>
    /// <param name="validatorAddress">The address of the validator.</param>
    /// <returns>The delegated PWR amount.</returns>
    public  async Task<ulong> GetDelegatedPWR(string delegatorAddress, string validatorAddress)  {
        ValidateAddress(delegatorAddress);
        ValidateAddress(validatorAddress);
       
            var url = $"{_rpcNodeUrl}validator/delegator/delegatedPWROfAddress/?userAddress={delegatorAddress}&validatorAddress={validatorAddress}";
            var responseString = await Request(url);

            var responseData = JsonConvert.DeserializeObject<JObject>(responseString);

           return responseData["delegatedPWR"]?.Value<ulong>() ?? 0;
    }
    /// <summary>
    /// Retrieves the share value of the specified validator.
    /// </summary>
    /// <param name="validator">The address of the validator.</param>
    /// <returns>The share value.</returns>
    public  async Task<BigDecimal> GetShareValue(string validator)  {
        ValidateAddress(validator);
         
            var url = $"{_rpcNodeUrl}/validator/shareValue/?validatorAddress={validator}";
            var response = await Request(url);
           
            var responseData = JsonConvert.DeserializeObject<JObject>(response);
            string value = responseData["shareValue"]?.Value<string>() ?? "";
           return BigDecimal.Parse(value);
    
    }
    /// <summary>
    /// Retrieves the owner of the virtual machine with the specified ID.
    /// </summary>
    /// <param name="vmId">The ID of the virtual machine.</param>
    /// <returns>The owner of the virtual machine.</returns>
    public  async Task<string> GetOwnerOfVm(ulong vmId)  {
      
            var url = $"{_rpcNodeUrl}/ownerOfVmId/?vmId={vmId}";
            var responseString = await Request(url);

            var responseData = JsonConvert.DeserializeObject<JObject>(responseString);

            return responseData["owner"]?.Value<string>() ?? "";
    
    }
    /// <summary>
    /// Broadcasts a transaction to the blockchain.
    /// </summary>
    /// <param name="txn">The transaction to broadcast.</param>
    /// <returns>An <see cref="ApiResponse"/> object representing the result of the broadcast.</returns>
    public async Task<ApiResponse> BroadcastTxn(byte[] txn)
    {
        try
        {
            var url = $"{_rpcNodeUrl}/broadcast/";
            var payload = new { txn = txn.ToHex() };
            var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
            Console.WriteLine(JsonConvert.SerializeObject(payload).ToString());
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
    /// <summary>
    /// Retrieves the nonce of the specified user address.
    /// </summary>
    /// <param name="address">The address of the user.</param>
    /// <returns>An <see cref="ApiResponse{T}"/> object containing the nonce value.</returns>
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
    /// <summary>
    /// Retrieves the balance of the specified user address.
    /// </summary>
    /// <param name="address">The address of the user.</param>
    /// <returns>An <see cref="ApiResponse{T}"/> object containing the balance value.</returns>
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
    /// <summary>
    /// Retrieves the guardian address and guardian status of the specified user address.
    /// </summary>
    /// <param name="address">The address of the user.</param>
    /// <returns>The guardian address if present and the guardian status.</returns>
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
    /// <summary>
    /// Retrieves the total count of blocks in the blockchain.
    /// </summary>
    /// <returns>The total count of blocks.</returns>
    public async Task<ulong> GetBlocksCount()
    {
        
            var url = $"{_rpcNodeUrl}/blocksCount/";
            var response = await Request(url);

            var responseData = JsonConvert.DeserializeObject<JObject>(response);
            var blocksCount = responseData["blocksCount"]?.Value<ulong>() ?? throw new Exception("Invalid response from RPC node");

            return blocksCount;       
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
        transactions: jsonBlock["transactions"].Select(t =>
            DeserializeTransaction(t.Value<string>("type"), t, new Newtonsoft.Json.JsonSerializer())
        ).ToList()
    );
                return blockInstance;
          
    }
    public Transaction DeserializeTransaction(string type, JToken jsonObject, Newtonsoft.Json.JsonSerializer serializer)
{
   
    switch (type)
    {
        case "claimvlmdtxn.type":
            return jsonObject.ToObject<ClaimVlmdTxn>(serializer);
        case "delegatetxn.type":
            return jsonObject.ToObject<DelegateTxn>(serializer);
        case "jointxn.type":
            return jsonObject.ToObject<JoinTxn>(serializer);
        case "transfertxn.type":
            return jsonObject.ToObject<TransferTxn>(serializer);
        case "vmdatatxn.type":
            return jsonObject.ToObject<VmDataTxn>(serializer);
        case "transaction.type":
            return jsonObject.ToObject<WithdrawTxn>(serializer);
        default:
            return jsonObject.ToObject<Transaction>(serializer);
    }
}
    /// <summary>
    /// Retrieves the total count of validators on the blockchain.
    /// </summary>
    /// <returns>The total count of validators.</returns>
    public async Task<uint> GetTotalValidatorsCount()
    {
     
            var url = $"{_rpcNodeUrl}/totalValidatorsCount/";
            var response = await Request(url);
            var responseData = JsonConvert.DeserializeObject<JObject>(response);
            var validatorsCount = responseData["validatorsCount"]?.Value<uint>() ?? throw new Exception("Invalid response from RPC node");

            return validatorsCount;
    }
    /// <summary>
    /// Retrieves the total count of standby validators on the blockchain.
    /// </summary>
    /// <returns>The total count of standby validators.</returns>
    public async Task<uint> GetStandbyValidatorsCount()
    {
       
            var url = $"{_rpcNodeUrl}/standbyValidatorsCount/";

            var response = await Request(url);

            var responseData = JsonConvert.DeserializeObject<JObject>(response);
            var validatorsCount = responseData["validatorsCount"]?.Value<uint>() ?? throw new Exception("Invalid response from RPC node");

            return validatorsCount;
    }
    /// <summary>
    /// Retrieves the total count of active validators on the blockchain.
    /// </summary>
    /// <returns>The total count of active validators.</returns>
    public async Task<uint> GetActiveValidatorsCount()
    {
      
            var url = $"{_rpcNodeUrl}/activeValidatorsCount/";

            var response = await Request(url);
            var responseData = JsonConvert.DeserializeObject<JObject>(response);
            var validatorsCount = responseData["validatorsCount"]?.Value<uint>() ?? throw new Exception("Invalid response from RPC node");

            return validatorsCount;
       
    }
    /// <summary>
    /// Retrieves a list of all validators on the blockchain.
    /// </summary>
    /// <returns>A list of <see cref="Validator"/> objects representing all validators.</returns>
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
    /// <summary>
    /// Retrieves a list of standby validators on the blockchain.
    /// </summary>
    /// <returns>A list of <see cref="Validator"/> objects representing standby validators.</returns>
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
    /// <summary>
    /// Retrieves a list of active validators on the blockchain.
    /// </summary>
    /// <returns>A list of <see cref="Validator"/> objects representing active validators.</returns>
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
    /// <summary>
    /// Retrieves the owner address of the virtual machine with the specified ID.
    /// </summary>
    /// <param name="vmId">The ID of the virtual machine.</param>
    /// <returns>The owner address of the virtual machine.</returns>
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

/// <summary>
/// Validates the format of an address.
/// </summary>
/// <param name="address">The address to validate.</param>
/// <exception cref="ArgumentException">Thrown when the address is null, empty, or has an invalid format.</exception>
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