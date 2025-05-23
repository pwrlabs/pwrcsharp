using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PWR.Models;
using PWR.Utils;

namespace PWR;
/// <summary>
/// Provides methods to interact with the PWR blockchain via RPC calls.
/// </summary>
public class RPC
{
    private readonly string _rpcNodeUrl;
    private readonly HttpClient _httpClient;

    public RPC(string rpcNodeUrl, HttpClient? httpClient = null)
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
            string url = $"{_rpcNodeUrl}/chainId";
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

    public string GetRpcNodeUrl()
    {
        return _rpcNodeUrl;
    }

    /// <summary>
    /// Retrieves the fee per byte for transactions on the blockchain.
    /// </summary>
    /// <returns>The fee per byte.</returns>
    public async Task<ulong> GetFeePerByte()
    {
        if(FeePerByte == 0){
            var url = $"{_rpcNodeUrl}/feePerByte";
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
        var url = $"{_rpcNodeUrl}/blockchainVersion";
        string response = await Request(url);
        
        JObject responseData = JsonConvert.DeserializeObject<JObject>(response);

        return responseData["blockchainVersion"]?.Value<short>() ?? throw new Exception("Unexpected error Occured.");
    }

    /// <summary>
    /// Retrieves the ECDSA verification fee.
    /// </summary>
    /// <returns>The ECDSA verification fee.</returns>
    public async Task<ulong> GetEcdsaVerificationFee(){
        var url = $"{_rpcNodeUrl}/ecdsaVerificationFee";
        var response = await Request(url);

        var responseData = JsonConvert.DeserializeObject<JObject>(response);
        return responseData["ecdsaVerificationFee"]?.Value<ulong>() ?? throw new Exception("Invalid response from RPC node");
    }

    /// <summary>
    /// Retrieves the burn percentage.
    /// </summary>
    /// <returns>The burn percentage.</returns>
    public async Task<ulong> GetBurnPercentage(){
        var url = $"{_rpcNodeUrl}/burnPercentage";
        var response = await Request(url);

        var responseData = JsonConvert.DeserializeObject<JObject>(response);
        return responseData["burnPercentage"]?.Value<ulong>() ?? throw new Exception("Invalid response from RPC node");
    }

    /// <summary>
    /// Retrieves the total voting power.
    /// </summary>
    /// <returns>The total voting power.</returns>
    public async Task<ulong> GetTotalVotingPower(){
        var url = $"{_rpcNodeUrl}/totalVotingPower";
        var response = await Request(url);

        var responseData = JsonConvert.DeserializeObject<JObject>(response);
        return responseData["totalVotingPower"]?.Value<ulong>() ?? throw new Exception("Invalid response from RPC node");
    }

    /// <summary>
    /// Retrieves the pwr rewards per year.
    /// </summary>
    /// <returns>The pwr rewards per year.</returns>
    public async Task<ulong> GetPwrRewardsPerYear(){
        var url = $"{_rpcNodeUrl}/pwrRewardsPerYear";
        var response = await Request(url);

        var responseData = JsonConvert.DeserializeObject<JObject>(response);
        return responseData["pwrRewardsPerYear"]?.Value<ulong>() ?? throw new Exception("Invalid response from RPC node");
    }

    /// <summary>
    /// Retrieves the withdrawal lock time.
    /// </summary>
    /// <returns>The withdrawal lock time.</returns>
    public async Task<ulong> GetWithdrawalLockTime(){
        var url = $"{_rpcNodeUrl}/withdrawalLockTime";
        var response = await Request(url);

        var responseData = JsonConvert.DeserializeObject<JObject>(response);
        return responseData["withdrawalLockTime"]?.Value<ulong>() ?? throw new Exception("Invalid response from RPC node");
    }

    public async Task<ulong> GetMaxBlockSize(){
        var url = $"{_rpcNodeUrl}/maxBlockSize";
        var response = await Request(url);

        var responseData = JsonConvert.DeserializeObject<JObject>(response);
        return responseData["maxBlockSize"]?.Value<ulong>() ?? throw new Exception("Invalid response from RPC node");
    }

    public async Task<ulong> GetMaxTransactionSize(){
        var url = $"{_rpcNodeUrl}/maxTransactionSize";
        var response = await Request(url);

        var responseData = JsonConvert.DeserializeObject<JObject>(response);
        return responseData["maxTransactionSize"]?.Value<ulong>() ?? throw new Exception("Invalid response from RPC node");
    }

    public async Task<ulong> GetBlockTimestamp(){
        var url = $"{_rpcNodeUrl}/blockTimestamp";
        var response = await Request(url);

        var responseData = JsonConvert.DeserializeObject<JObject>(response);
        return responseData["blockTimestamp"]?.Value<ulong>() ?? throw new Exception("Invalid response from RPC node");
    }

    public async Task<ulong> GetProposalFee(){
        var url = $"{_rpcNodeUrl}/proposalFee";
        var response = await Request(url);

        var responseData = JsonConvert.DeserializeObject<JObject>(response);
        return responseData["proposalFee"]?.Value<ulong>() ?? throw new Exception("Invalid response from RPC node");
    }

    public async Task<ulong> GetProposalValidityTime(){
        var url = $"{_rpcNodeUrl}/proposalValidityTime";
        var response = await Request(url);

        var responseData = JsonConvert.DeserializeObject<JObject>(response);
        return responseData["proposalFee"]?.Value<ulong>() ?? throw new Exception("Invalid response from RPC node");
    }

    public async Task<ulong> GetValidatorCountLimit(){
        var url = $"{_rpcNodeUrl}/validatorCountLimit";
        var response = await Request(url);

        var responseData = JsonConvert.DeserializeObject<JObject>(response);
        return responseData["validatorCountLimit"]?.Value<ulong>() ?? throw new Exception("Invalid response from RPC node");
    }

    public async Task<ulong> GetValidatorSlashingFee(){
        var url = $"{_rpcNodeUrl}/validatorSlashingFee";
        var response = await Request(url);

        var responseData = JsonConvert.DeserializeObject<JObject>(response);
        return responseData["validatorSlashingFee"]?.Value<ulong>() ?? throw new Exception("Invalid response from RPC node");
    }

    public async Task<ulong> GetValidatorOperationalFee(){
        var url = $"{_rpcNodeUrl}/validatorOperationalFee";
        var response = await Request(url);

        var responseData = JsonConvert.DeserializeObject<JObject>(response);
        return responseData["validatorOperationalFee"]?.Value<ulong>() ?? throw new Exception("Invalid response from RPC node");
    }

    public async Task<ulong> GetValidatorJoiningFee(){
        var url = $"{_rpcNodeUrl}/validatorJoiningFee";
        var response = await Request(url);

        var responseData = JsonConvert.DeserializeObject<JObject>(response);
        return responseData["validatorJoiningFee"]?.Value<ulong>() ?? throw new Exception("Invalid response from RPC node");
    }

    public async Task<ulong> GetMinimumDelegatingAmount(){
        var url = $"{_rpcNodeUrl}/minimumDelegatingAmount";
        var response = await Request(url);

        var responseData = JsonConvert.DeserializeObject<JObject>(response);
        return responseData["minimumDelegatingAmount"]?.Value<ulong>() ?? throw new Exception("Invalid response from RPC node");
    }

    public async Task<ulong> GetVidaOwnerTransactionFeeShare(){
        var url = $"{_rpcNodeUrl}/vidaOwnerTransactionFeeShare";
        var response = await Request(url);

        var responseData = JsonConvert.DeserializeObject<JObject>(response);
        return responseData["vidaOwnerTransactionFeeShare"]?.Value<ulong>() ?? throw new Exception("Invalid response from RPC node");
    }

    public async Task<ulong> GetVidaIdClaimingFee(){
        var url = $"{_rpcNodeUrl}/vidaIdClaimingFee";
        var response = await Request(url);

        var responseData = JsonConvert.DeserializeObject<JObject>(response);
        return responseData["vidaIdClaimingFee"]?.Value<ulong>() ?? throw new Exception("Invalid response from RPC node");
    }

    public async Task<ulong> GetMaxGuardianTime(){
        var url = $"{_rpcNodeUrl}/maxGuardianTime";
        var response = await Request(url);

        var responseData = JsonConvert.DeserializeObject<JObject>(response);
        return responseData["maxGuardianTime"]?.Value<ulong>() ?? throw new Exception("Invalid response from RPC node");
    }

    /// <summary>
    /// Retrieves the total count of active validators on the blockchain.
    /// </summary>
    /// <returns>The total count of active validators.</returns>
    public async Task<uint> GetActiveValidatorsCount()
    {
        var url = $"{_rpcNodeUrl}/activeValidatorsCount";

        var response = await Request(url);
        var responseData = JsonConvert.DeserializeObject<JObject>(response);
        var validatorsCount = responseData["validatorsCount"]?.Value<uint>() ?? throw new Exception("Invalid response from RPC node");

        return validatorsCount;
    }

    public string GetVidaIdAddress(int vidaId)
    {
        string hexAddress = "0";
        if (vidaId >= 0)
        {
            hexAddress = "1";
        }
        if (vidaId < 0)
        {
            vidaId = -vidaId;
        }

        string vidaIdString = vidaId.ToString();
        int padding = 39 - vidaIdString.Length;

        if (padding > 0)
        {
            hexAddress += new string('0', padding);
        }

        hexAddress += vidaIdString;

        return "0x" + hexAddress;
    }


    /// <summary>
    /// Retrieves a list of virtual machine data transactions within the specified block range and virtual machine ID.
    /// </summary>
    /// <param name="startingBlock">The starting block number of the range.</param>
    /// <param name="endingBlock">The ending block number of the range.</param>
    /// <param name="vidaId">The ID of the virtual machine.</param>
    /// <returns>A list of <see cref="VidaDataTxn"/> representing the virtual machine data transactions.</returns>
    /// <exception cref="Exception">Thrown when an error occurs during the HTTP request or the response JSON does not contain 'transactions'.</exception>   
    public async Task<List<VidaDataTxn>> GetVidaDataTransactions(ulong startingBlock, ulong endingBlock, ulong vidaId){
        var url = $"{_rpcNodeUrl}/getVidaTransactions?startingBlock={startingBlock}&endingBlock={endingBlock}&vidaId={vidaId}";
        string responseString = await Request(url);

        var responseData = JsonConvert.DeserializeObject<JObject>(responseString);
        var vidaDataTxnsJson = responseData["transactions"]?.ToString() ?? throw new Exception("The response JSON does not contain 'transactions'.");
        
        // Parse the transactions array
        var transactionsArray = JArray.Parse(vidaDataTxnsJson);
        var vidaDataTxnList = new List<VidaDataTxn>();

        foreach (var transaction in transactionsArray)
        {
            JObject? obj = null;
            if (transaction is JObject jobj)
            {
                obj = jobj;
            }
            else if (transaction.Type == JTokenType.String)
            {
                try
                {
                    obj = JObject.Parse(transaction.Value<string>() ?? "{}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Failed to parse string transaction as JObject: {transaction}, error: {ex.Message}");
                    continue;
                }
            }
            else
            {
                Console.WriteLine($"Warning: Skipping transaction of unexpected type: {transaction.GetType()} value: {transaction}");
                continue;
            }
            var vidaDataTxn = new VidaDataTxn(
                size: obj["size"]?.Value<uint>() ?? 0,
                blockNumber: obj["blockNumber"]?.Value<ulong>() ?? 0,
                positionintheBlock: obj["positionInBlock"]?.Value<uint>() ?? 0,
                fee: obj["paidTotalFee"]?.Value<ulong>() ?? 0,
                type: "VIDA Data",
                sender: obj["sender"]?.Value<string>() ?? "",
                receiver: "",
                nonce: obj["nonce"]?.Value<uint>() ?? 0,
                hash: obj["transactionHash"]?.Value<string>() ?? "",
                value: obj["amount"]?.Value<ulong>() ?? 0,
                timestamp: obj["timeStamp"]?.Value<ulong>() ?? 0,
                vidaId: obj["vidaId"]?.Value<ulong>() ?? 0,
                data: obj["data"]?.Value<string>() ?? ""
            );
            vidaDataTxnList.Add(vidaDataTxn);
        }

        return vidaDataTxnList;
    }

    /// <summary>
    /// Retrieves a list of virtual machine data transactions within the specified block range, virtual machine ID, and byte prefix filter.
    /// </summary>
    /// <param name="startingBlock">The starting block number of the range.</param>
    /// <param name="endingBlock">The ending block number of the range.</param>
    /// <param name="vidaId">The ID of the virtual machine.</param>
    /// <param name="prefix">The byte prefix filter.</param>
    /// <returns>A list of <see cref="VidaDataTxn"/> representing the virtual machine data transactions.</returns>
    public async Task<List<VidaDataTxn>> GetVidaDataTransactionsFilterByPerBytePrefix(ulong startingBlock, ulong endingBlock, ulong vidaId, byte[] prefix){
        var url = $"{_rpcNodeUrl}/getVidaTransactionsSortByBytePrefix?startingBlock={startingBlock}&endingBlock={endingBlock}&vidaId={vidaId}&bytePrefix={BitConverter.ToString(prefix).Replace("-", "").ToLower()}";
        var response = await _httpClient.GetAsync(url);
        
        var responseString = await response.Content.ReadAsStringAsync();

        // Pre-process the JSON string to quote large numbers in totalShares
        var regex = new Regex(@"""totalShares"":\s*(\d+)");
        responseString = regex.Replace(responseString, match => {
            var number = match.Groups[1].Value;
            return $@"""totalShares"": ""{number}""";
        });

        var responseData = JsonConvert.DeserializeObject<JObject>(responseString);
        var vidaDataTxnsJson = responseData["transactions"].ToString();
        var vidaDataTxnList = JsonConvert.DeserializeObject<List<VidaDataTxn>>(vidaDataTxnsJson);
        return vidaDataTxnList;
    }

    /// <summary>
    /// Retrieves the active voting power on the blockchain.
    /// </summary>
    /// <returns>The active voting power.</returns>
    public  async Task<ulong> GetActiveVotingPower()  {
        var url = $"{_rpcNodeUrl}/activeVotingPower";
        var response = await Request(url);

        var responseData = JsonConvert.DeserializeObject<JObject>(response);
        return responseData["activeVotingPower"]?.Value<ulong>() ?? 0;
    }

    /// <summary>
    /// Retrieves the total count of delegators on the blockchain.
    /// </summary>
    /// <returns>The total count of delegators.</returns>
    public async Task<uint> GetDelegatorsCount()  {
        var url = $"{_rpcNodeUrl}/totalDelegatorsCount";
        var response = await Request(url);
        var responseData = JsonConvert.DeserializeObject<JObject>(response);

        return responseData["delegatorsCount"]?.Value<uint>() ?? 0;
    }

    /// <summary>
    /// Retrieves the list of delegatees of the specified user address.
    /// </summary>
    /// <param name="address">The address of the user.</param>
    /// <returns>A list of <see cref="Validator"/> representing the delegatees.</returns>
    public async Task<List<Validator>> GetDelegatees(string address)  {
        ValidateAddress(address);
        var url = $"{_rpcNodeUrl}/delegateesOfUser?userAddress={address}";
        var response = await Request(url);
        var responseData = JsonConvert.DeserializeObject<JObject>(response);
        if (responseData["validators"] == null)
            return new List<Validator>();
        var tk = JsonConvert.DeserializeObject<JArray>(responseData["validators"].ToString());
        List<Validator> validators = new List<Validator>();
        foreach(var token in tk){
            // Safely parse values with proper type handling
            string addressStr = token["address"]?.ToString() ?? "";
            string ip = token["ip"]?.ToString() ?? "";
            string status = token["status"]?.ToString() ?? "active";
            
            bool badActor = false;
            if (token["badActor"] != null)
            {
                if (token["badActor"].Type == JTokenType.Boolean)
                {
                    badActor = token["badActor"].Value<bool>();
                }
                else
                {
                    bool.TryParse(token["badActor"].ToString(), out badActor);
                }
            }
            
            ulong votingPower = 0;
            if (token["votingPower"] != null)
            {
                string vpStr = token["votingPower"].ToString();
                if (!string.IsNullOrEmpty(vpStr))
                {
                    ulong.TryParse(vpStr, out votingPower);
                }
            }
            
            ulong totalShares = 0;
            if (token["totalShares"] != null)
            {
                string sharesStr = token["totalShares"].ToString();
                if (!string.IsNullOrEmpty(sharesStr))
                {
                    if (sharesStr.Length > 20) // Handle very large numbers
                    {
                        totalShares = ulong.MaxValue;
                    }
                    else
                    {
                        ulong.TryParse(sharesStr, out totalShares);
                    }
                }
            }
            
            uint delegatorsCount = 0;
            if (token["delegatorsCount"] != null)
            {
                string countStr = token["delegatorsCount"].ToString();
                if (!string.IsNullOrEmpty(countStr))
                {
                    uint.TryParse(countStr, out delegatorsCount);
                }
            }
            
            ulong lastCreatedBlock = 0;
            if (token["lastCreatedBlock"] != null)
            {
                string lcbStr = token["lastCreatedBlock"].ToString();
                if (!string.IsNullOrEmpty(lcbStr))
                {
                    ulong.TryParse(lcbStr, out lastCreatedBlock);
                }
            }
            
            var validator = new Validator(
                address: "0x" + addressStr,
                ip: ip,
                badActor: badActor,
                votingPower: votingPower,
                totalShares: totalShares,
                delegatorsCount: delegatorsCount,
                status: status,
                lastCreatedBlock: lastCreatedBlock,
                httpClient: _httpClient
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
    public async Task<Validator> GetValidator(string validatorAddress)
    {
        ValidateAddress(validatorAddress);
         
        var url = $"{_rpcNodeUrl}/validator?validatorAddress={validatorAddress}";
        var response = await Request(url);
        var responseData = JsonConvert.DeserializeObject<JObject>(response);
        var token = JsonConvert.DeserializeObject<JObject>(responseData["validator"].ToString());

        // Safely parse values with proper type handling
        ulong votingPower = 0;
        if (token["votingPower"] != null)
        {
            string vpStr = token["votingPower"].ToString();
            if (!string.IsNullOrEmpty(vpStr))
            {
                if (ulong.TryParse(vpStr, out ulong vp))
                {
                    votingPower = vp;
                }
            }
        }

        ulong totalShares = 0;
        if (token["totalShares"] != null)
        {
            string sharesStr = token["totalShares"].ToString();
            if (!string.IsNullOrEmpty(sharesStr))
            {
                if (ulong.TryParse(sharesStr, out ulong shares))
                {
                    totalShares = shares;
                }
            }
        }

        ulong lastCreatedBlock = 0;
        if (token["lastCreatedBlock"] != null)
        {
            string lcbStr = token["lastCreatedBlock"].ToString();
            if (!string.IsNullOrEmpty(lcbStr))
            {
                if (ulong.TryParse(lcbStr, out ulong lcb))
                {
                    lastCreatedBlock = lcb;
                }
            }
        }

        uint delegatorsCount = 0;
        if (token["delegatorsCount"] != null)
        {
            string countStr = token["delegatorsCount"].ToString();
            if (!string.IsNullOrEmpty(countStr))
            {
                if (uint.TryParse(countStr, out uint count))
                {
                    delegatorsCount = count;
                }
            }
        }

        bool badActor = false;
        if (token["badActor"] != null)
        {
            if (token["badActor"].Type == JTokenType.Boolean)
            {
                badActor = token["badActor"].Value<bool>();
            }
            else
            {
                string badActorStr = token["badActor"].ToString();
                if (!string.IsNullOrEmpty(badActorStr))
                {
                    bool.TryParse(badActorStr, out badActor);
                }
            }
        }

        var validator = new Validator(
            address: token["address"]?.ToString() ?? "",
            ip: token["ip"]?.ToString() ?? "",
            badActor: badActor,
            votingPower: votingPower,
            totalShares: totalShares,
            delegatorsCount: delegatorsCount,
            status: token["status"]?.ToString() ?? "standby",
            lastCreatedBlock: lastCreatedBlock,
            httpClient: _httpClient
        );
        return validator;
    }

    /// <summary>
    /// Retrieves the delegated PWR (Power) for the specified delegator and validator addresses.
    /// </summary>
    /// <param name="delegatorAddress">The address of the delegator.</param>
    /// <param name="validatorAddress">The address of the validator.</param>
    /// <returns>The delegated PWR amount.</returns>
    public async Task<ulong> GetDelegatorsOfPwr(string delegatorAddress, string validatorAddress)  {
        ValidateAddress(delegatorAddress);
        ValidateAddress(validatorAddress);
       
        var url = $"{_rpcNodeUrl}/validator/delegator/delegatedPWROfAddress?userAddress={delegatorAddress}&validatorAddress={validatorAddress}";
        var responseString = await Request(url);

        var responseData = JsonConvert.DeserializeObject<JObject>(responseString);

        return responseData["delegatedPWR"]?.Value<ulong>() ?? 0;
    }

    public async Task<ulong> GetSharesOfDelegator(string delegatorAddress, string validatorAddress)  {
        ValidateAddress(delegatorAddress);
        ValidateAddress(validatorAddress);
       
        var url = $"{_rpcNodeUrl}/validator/delegator/sharesOfAddress?userAddress={delegatorAddress}&validatorAddress={validatorAddress}";
        var responseString = await Request(url);

        var responseData = JsonConvert.DeserializeObject<JObject>(responseString);

        return responseData["shares"]?.Value<ulong>() ?? 0;
    }

    /// <summary>
    /// Retrieves the share value of the specified validator.
    /// </summary>
    /// <param name="validator">The address of the validator.</param>
    /// <returns>The share value.</returns>
    public async Task<double> GetShareValue(string validator)
    {
        ValidateAddress(validator);
        
        var url = $"{_rpcNodeUrl}/validator/shareValue?validatorAddress={validator}";
        var response = await Request(url);
        
        var responseData = JsonConvert.DeserializeObject<JObject>(response);
        
        // Try to get the value directly as double, or parse the string
        if (responseData["shareValue"] == null)
        {
            return 0.0;
        }
        
        return responseData["shareValue"].Value<double>();
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
            var url = $"{_rpcNodeUrl}/broadcast";
            var payload = new { txn = txn.ToHex() };
            var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(url, content);
            var responseString = await response.Content.ReadAsStringAsync();

            var responseData = JsonConvert.DeserializeObject<ApiResponse>(responseString);
            
            if (responseData != null && !string.IsNullOrEmpty(responseData.Message))
            {
                return new ApiResponse(response.IsSuccessStatusCode, responseData.Message);
            }
            
            return new ApiResponse(response.IsSuccessStatusCode, "Success");
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
    /// <returns>The nonce value.</returns>
    public async Task<uint> GetNonceOfAddress(string address)
    {
        ValidateAddress(address);
        var url = $"{_rpcNodeUrl}/nonceOfUser?userAddress={address}";
        var response = await _httpClient.GetAsync(url);
        var responseString = await response.Content.ReadAsStringAsync();

        var responseData = JsonConvert.DeserializeObject<JObject>(responseString);
        var nonce = responseData["nonce"]?.Value<uint>() ?? 0;
        return nonce;
    }
    /// <summary>
    /// Retrieves the balance of the specified user address.
    /// </summary>
    /// <param name="address">The address of the user.</param>
    /// <returns>The balance value.</returns>
    public async Task<ulong> GetBalanceOfAddress(string address)
    {
        ValidateAddress(address);
        var url = $"{_rpcNodeUrl}/balanceOf?userAddress={address}";
        var response = await _httpClient.GetAsync(url);
        var responseString = await response.Content.ReadAsStringAsync();

        var responseData = JsonConvert.DeserializeObject<JObject>(responseString);
        var balance = responseData["balance"]?.Value<ulong>() ?? throw new Exception("Invalid response from RPC node");
        return balance;
    }

    /// <summary>
    /// Retrieves the guardian address and guardian status of the specified user address.
    /// </summary>
    /// <param name="address">The address of the user.</param>
    /// <returns>The guardian address if present and the guardian status.</returns>
    public async Task<string> GetGuardianOfAddress(string address)
    {
        ValidateAddress(address);
        var url = $"{_rpcNodeUrl}/guardianOf?userAddress={address}";
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
        var url = $"{_rpcNodeUrl}/blocksCount";
        var response = await Request(url);

        var responseData = JsonConvert.DeserializeObject<JObject>(response);
        var blocksCount = responseData["blocksCount"]?.Value<ulong>() ?? throw new Exception("Invalid response from RPC node");
        return blocksCount;     
    }

    /// <summary>
    /// Retrieves the total count of blocks in the blockchain.
    /// </summary>
    /// <returns>The total count of blocks.</returns>
    public async Task<ulong> GetLatestBlockNumber()
    {
        var url = $"{_rpcNodeUrl}/blockNumber";
        var response = await Request(url);

        var responseData = JsonConvert.DeserializeObject<JObject>(response);
        var blocksCount = responseData["blockNumber"]?.Value<ulong>() ?? throw new Exception("Invalid response from RPC node");
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
        var url = $"{_rpcNodeUrl}/block?blockNumber={blockNumber}";
        var response = await Request(url);
        var responseObject = JsonConvert.DeserializeObject<JObject>(response);
        var json = responseObject["block"].ToString();
        var jsonBlock = JsonConvert.DeserializeObject<JObject>(json);

        var blockInstance = new Block(
            processedWithoutCriticalErrors: jsonBlock.Value<bool>("processedWithoutCriticalErrors"),
            timeStamp: jsonBlock.Value<ulong>("timeStamp"),
            blockHash: jsonBlock.Value<string>("blockHash"),
            previousBlockHash: jsonBlock.Value<string>("previousBlockHash"),
            size: jsonBlock.Value<uint>("size"),
            proposer: jsonBlock.Value<string>("proposer"),
            blockNumber: jsonBlock.Value<ulong>("blockNumber"),
            burnedFees: jsonBlock.Value<ulong>("burnedFees"),
            rootHash: jsonBlock.Value<string>("rootHash"),
            blockReward: jsonBlock.Value<ulong>("blockReward"),
            transactions: jsonBlock["transactions"].Select(t =>
                new BlockTransaction(
                    identifier: t.Value<uint>("identifier"),
                    transactionHash: t.Value<string>("transactionHash")
                )
            ).ToList(),
            newSharesPerSpark: jsonBlock.Value<ulong>("newSharesPerSpark")
        );
        return blockInstance;
          
    }
    public Transaction DeserializeTransaction(string type, JToken jsonObject, Newtonsoft.Json.JsonSerializer serializer)
    {
    
        switch (type)
        {
            case "VIDA Data":
                return jsonObject.ToObject<VidaDataTxn>(serializer);
            case "Set Guardian":
                return jsonObject.ToObject<SetGuardianTxn>(serializer);
            case "Remove Guardian":
                return jsonObject.ToObject<RemoveGuardianTxn>(serializer);
            case "Guardian Approval":
                return jsonObject.ToObject<ClaimSpotTxn>(serializer);
            case "Validator Claim Spot":
                return jsonObject.ToObject<ClaimSpotTxn>(serializer);
            case "Payable VIDA Data":
                return jsonObject.ToObject<PayableVidaDataTxn>(serializer);
            case "Claim VIDA ID":
                return jsonObject.ToObject<ClaimVlmdTxn>(serializer);
            case "Conduit Approval":
                return jsonObject.ToObject<ConduitApprovalTxn >(serializer);  
            case "Delegate":
                return jsonObject.ToObject<DelegateTxn>(serializer);
            case "Validator Join":
                return jsonObject.ToObject<JoinTxn>(serializer);
            case "Transfer":
                return jsonObject.ToObject<TransferTxn>(serializer);
            case "Withdraw":
                return jsonObject.ToObject<WithdrawTxn>(serializer);
            default:
                return jsonObject.ToObject<Transaction>(serializer);
        }
    }

    /// <summary>
    /// Retrieves the total count of validators on the blockchain.
    /// </summary>
    /// <returns>The total count of validators.</returns>
    public async Task<uint> GetValidatorsCount()
    {
        var url = $"{_rpcNodeUrl}/totalValidatorsCount";
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
        var url = $"{_rpcNodeUrl}/standbyValidatorsCount";
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
        try
        {
            var response = await _httpClient.GetAsync(_rpcNodeUrl + "/allValidators");
            var responseString = await response.Content.ReadAsStringAsync();

            // Pre-process the JSON string to quote large numbers in totalShares
            var regex = new Regex(@"""totalShares"":\s*(\d+)");
            responseString = regex.Replace(responseString, match => {
                var number = match.Groups[1].Value;
                return $@"""totalShares"": ""{number}""";
            });

            JObject responseData = JsonConvert.DeserializeObject<JObject>(responseString);
            JArray validatorsArray = (JArray)responseData["validators"];
            List<Validator> validators = new List<Validator>();

            foreach(var validator in validatorsArray)
            {
                try
                {
                    // Handle string values
                    string address = validator["address"]?.ToString() ?? "";
                    string ip = validator["ip"]?.ToString() ?? "";
                    string status = validator["status"]?.ToString() ?? "active";
                    
                    // Handle numeric values with safer conversions
                    bool badActor = false;
                    if (validator["badActor"] != null)
                    {
                        if (validator["badActor"].Type == JTokenType.Boolean)
                        {
                            badActor = validator["badActor"].Value<bool>();
                        }
                        else
                        {
                            badActor = Convert.ToBoolean(validator["badActor"].ToString());
                        }
                    }
                    
                    ulong votingPower = 0;
                    if (validator["votingPower"] != null)
                    {
                        string vpStr = validator["votingPower"].ToString();
                        if (!string.IsNullOrEmpty(vpStr))
                        {
                            votingPower = Convert.ToUInt64(vpStr);
                        }
                    }
                    
                    ulong shares = 0;
                    if (validator["totalShares"] != null)
                    {
                        string sharesStr = validator["totalShares"].ToString();
                        if (!string.IsNullOrEmpty(sharesStr))
                        {
                            shares = Convert.ToUInt64(sharesStr);
                        }
                    }
                    
                    uint delegatorsCount = 0;
                    if (validator["delegatorsCount"] != null)
                    {
                        string countStr = validator["delegatorsCount"].ToString();
                        if (!string.IsNullOrEmpty(countStr))
                        {
                            delegatorsCount = Convert.ToUInt32(countStr);
                        }
                    }
                    
                    ulong lastCreatedBlock = 0;
                    if (validator["lastCreatedBlock"] != null)
                    {
                        string lcbStr = validator["lastCreatedBlock"].ToString();
                        if (!string.IsNullOrEmpty(lcbStr))
                        {
                            lastCreatedBlock = Convert.ToUInt64(lcbStr);
                        }
                    }
                    
                    validators.Add(new Validator(
                        address: address,
                        ip: ip,
                        badActor: badActor,
                        votingPower: votingPower,
                        totalShares: shares,
                        delegatorsCount: delegatorsCount,
                        status: status,
                        lastCreatedBlock: lastCreatedBlock,
                        httpClient: _httpClient
                    ));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error parsing validator: {ex.Message}");
                    // Skip this validator if it can't be parsed
                }
            }
            
            return validators;
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to get validators: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Retrieves a list of standby validators on the blockchain.
    /// </summary>
    /// <returns>A list of <see cref="Validator"/> objects representing standby validators.</returns>
    public async Task<List<Validator>> GetStandbyValidators()
    {
        try
        {
            var response = await _httpClient.GetAsync(_rpcNodeUrl + "/standbyValidators");
            var responseString = await response.Content.ReadAsStringAsync();

            // Pre-process the JSON string to quote large numbers in totalShares
            var regex = new Regex(@"""totalShares"":\s*(\d+)");
            responseString = regex.Replace(responseString, match => {
                var number = match.Groups[1].Value;
                return $@"""totalShares"": ""{number}""";
            });

            JObject responseData = JObject.Parse(responseString);
            JArray validatorsArray = (JArray)responseData["validators"];
            List<Validator> validators = new List<Validator>();

            foreach(var validator in validatorsArray)
            {
                try
                {
                    string address = validator["address"]?.ToString() ?? "";
                    string ip = validator["ip"]?.ToString() ?? "";
                    string status = validator["status"]?.ToString() ?? "standby";
                    
                    bool badActor = false;
                    if (validator["badActor"] != null)
                    {
                        if (validator["badActor"].Type == JTokenType.Boolean)
                        {
                            badActor = validator["badActor"].Value<bool>();
                        }
                        else
                        {
                            badActor = Convert.ToBoolean(validator["badActor"].ToString());
                        }
                    }
                    
                    ulong votingPower = 0;
                    if (validator["votingPower"] != null)
                    {
                        string vpStr = validator["votingPower"].ToString();
                        if (!string.IsNullOrEmpty(vpStr))
                        {
                            ulong.TryParse(vpStr, out votingPower);
                        }
                    }
                    
                    ulong shares = 0;
                    if (validator["totalShares"] != null)
                    {
                        string sharesStr = validator["totalShares"].ToString();
                        if (!string.IsNullOrEmpty(sharesStr))
                        {
                            ulong.TryParse(sharesStr, out shares);
                        }
                    }
                    
                    uint delegatorsCount = 0;
                    if (validator["delegatorsCount"] != null)
                    {
                        string countStr = validator["delegatorsCount"].ToString();
                        if (!string.IsNullOrEmpty(countStr))
                        {
                            uint.TryParse(countStr, out delegatorsCount);
                        }
                    }
                    
                    ulong lastCreatedBlock = 0;
                    if (validator["lastCreatedBlock"] != null)
                    {
                        string lcbStr = validator["lastCreatedBlock"].ToString();
                        if (!string.IsNullOrEmpty(lcbStr))
                        {
                            ulong.TryParse(lcbStr, out lastCreatedBlock);
                        }
                    }
                    
                    validators.Add(new Validator(
                        address: address,
                        ip: ip,
                        badActor: badActor,
                        votingPower: votingPower,
                        totalShares: shares,
                        delegatorsCount: delegatorsCount,
                        status: status,
                        lastCreatedBlock: lastCreatedBlock,
                        httpClient: _httpClient
                    ));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error parsing standby validator: {ex.Message}");
                }
            }
            return validators;
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to get standby validators: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Retrieves a list of active validators on the blockchain.
    /// </summary>
    /// <returns>A list of <see cref="Validator"/> objects representing active validators.</returns>
    public async Task<List<Validator>> GetActiveValidators()
    {
        var url = _rpcNodeUrl + "/activeValidators";
        var response = await Request(url);

        // Apply regex to the raw response string to quote large numbers in totalShares
        var regex = new Regex(@"(""validators""\s*:\s*\[.*?\])", RegexOptions.Singleline);
        var match = regex.Match(response);
        string validatorsString = "[]";
        if (match.Success)
        {
            // Extract the validators array string
            var arrayRegex = new Regex(@"""totalShares""\s*:\s*(\d+)");
            validatorsString = match.Groups[1].Value;
            validatorsString = arrayRegex.Replace(validatorsString, m => $"\"totalShares\": \"{m.Groups[1].Value}\"");
            // Remove the 'validators:' key to get a pure array
            int idx = validatorsString.IndexOf('[');
            if (idx >= 0)
                validatorsString = validatorsString.Substring(idx);
        }
        JArray validatorsArray = JArray.Parse(validatorsString);
        List<Validator> validators = new List<Validator>();
        foreach(var token in validatorsArray){
            string address = token["address"]?.ToString() ?? "";
            string ip = token["ip"]?.ToString() ?? "";
            string status = token["status"]?.ToString() ?? "active";
            bool badActor = token["badActor"]?.Type == JTokenType.Boolean ? token["badActor"].Value<bool>() : false;
            ulong votingPower = 0;
            if (token["votingPower"] != null) ulong.TryParse(token["votingPower"].ToString(), out votingPower);
            // Handle totalShares as string to avoid overflow
            string totalSharesStr = token["totalShares"]?.ToString() ?? "0";
            ulong totalShares = 0;
            if (!string.IsNullOrEmpty(totalSharesStr))
            {
                if (totalSharesStr.Length > 20)
                {
                    totalShares = ulong.MaxValue;
                }
                else
                {
                    ulong.TryParse(totalSharesStr, out totalShares);
                }
            }
            uint delegatorsCount = 0;
            if (token["delegatorsCount"] != null) uint.TryParse(token["delegatorsCount"].ToString(), out delegatorsCount);
            ulong lastCreatedBlock = 0;
            if (token["lastCreatedBlock"] != null) ulong.TryParse(token["lastCreatedBlock"].ToString(), out lastCreatedBlock);
            var val = new Validator(
                address: address,
                ip: ip,
                badActor: badActor,
                votingPower: votingPower,
                totalShares: totalShares,
                delegatorsCount: delegatorsCount,
                status: status,
                lastCreatedBlock: lastCreatedBlock,
                httpClient: _httpClient
            );
            validators.Add(val);
        }
        return validators;
    }

    /// <summary>
    /// Retrieves the owner address of the virtual machine with the specified ID.
    /// </summary>
    /// <param name="vidaId">The ID of the virtual machine.</param>
    /// <returns>The owner address of the virtual machine.</returns>
    public async Task<string> GetOwnerOfVidaIds(uint vidaId)
    {
        var url = $"{_rpcNodeUrl}/ownerOfVidaId?vidaId={vidaId}";
        var response = await Request(url);
        
        var data = JsonConvert.DeserializeObject<JObject>(response);
        return data["owner"]?.ToString() ?? throw new Exception("Invalid response from RPC node, owner is null");
    }

    /// <summary>
    /// Updates the fee per byte on the blockchain.
    /// </summary>
    /// <returns>A <see cref="Task{ulong}"/> representing the asynchronous operation, with the new fee per byte value.</returns>
    public async Task UpdateFeePerByte()
    {
        var url = $"{_rpcNodeUrl}/feePerByte";
        var response = await Request(url);

        var data = JsonConvert.DeserializeObject<JObject>(response);
        FeePerByte = data["feePerByte"]?.Value<uint>() ?? throw new Exception("Invalid response from RPC node");
    }

    /// <summary>
    /// Retrieves a transaction by its hash from the blockchain.
    /// </summary>
    /// <param name="transactionHash">The hash of the transaction to retrieve.</param>
    /// <returns>A <see cref="Task{TransactionResponse}"/> representing the asynchronous operation, with a TransactionResponse object containing transaction details if found.</returns>
    /// <exception cref="Exception">Thrown when an HTTP error occurs or the response from the RPC node is invalid.</exception>
    public async Task<TransactionResponse> GetTransactionByHash(string transactionHash)
    {
        var url = $"{_rpcNodeUrl}/transactionByHash?transactionHash={transactionHash}";
        var response = await Request(url);
        var responseObject = JsonConvert.DeserializeObject<JObject>(response);
        var json = responseObject["transaction"].ToString();
        var jsonTransaction = JsonConvert.DeserializeObject<JObject>(json);

        // Handle data field as string first
        byte[] data = Array.Empty<byte>();
        var dataToken = jsonTransaction["data"];
        if (dataToken != null && dataToken.Type != JTokenType.Null)
        {
            var dataStr = dataToken.ToString();
            if (!string.IsNullOrEmpty(dataStr))
            {
                try
                {
                    // Remove "0x" prefix if present
                    if (dataStr.StartsWith("0x"))
                    {
                        dataStr = dataStr.Substring(2);
                    }
                    // Convert hex string to bytes
                    data = Enumerable.Range(0, dataStr.Length / 2)
                        .Select(x => Convert.ToByte(dataStr.Substring(x * 2, 2), 16))
                        .ToArray();
                }
                catch
                {
                    data = Array.Empty<byte>();
                }
            }
        }

        // Handle guardian data field
        byte[] guardianData = Array.Empty<byte>();
        var guardianDataToken = jsonTransaction["guardianData"];
        if (guardianDataToken != null && guardianDataToken.Type != JTokenType.Null)
        {
            var guardianDataStr = guardianDataToken.ToString();
            if (!string.IsNullOrEmpty(guardianDataStr))
            {
                try
                {
                    // Remove "0x" prefix if present
                    if (guardianDataStr.StartsWith("0x"))
                    {
                        guardianDataStr = guardianDataStr.Substring(2);
                    }
                    // Convert hex string to bytes
                    guardianData = Enumerable.Range(0, guardianDataStr.Length / 2)
                        .Select(x => Convert.ToByte(guardianDataStr.Substring(x * 2, 2), 16))
                        .ToArray();
                }
                catch
                {
                    guardianData = Array.Empty<byte>();
                }
            }
        }

        return new TransactionResponse(
            identifier: jsonTransaction.Value<uint>("identifier"),
            paidTotalFee: jsonTransaction.Value<ulong>("paidTotalFee"),
            amount: jsonTransaction.Value<ulong>("amount"),
            paidActionFee: jsonTransaction.Value<ulong>("paidActionFee"),
            nonce: jsonTransaction.Value<uint>("nonce"),
            transactionHash: jsonTransaction.Value<string>("transactionHash"),
            timeStamp: jsonTransaction.Value<ulong>("timeStamp"),
            feePerByte: jsonTransaction.Value<ulong>("feePerByte"),
            size: jsonTransaction.Value<uint>("size"),
            sender: jsonTransaction.Value<string>("sender"),
            success: jsonTransaction.Value<bool>("success"),
            blockNumber: jsonTransaction.Value<uint>("blockNumber"),
            positionInTheBlock: jsonTransaction.Value<uint>("positionInTheBlock"),
            vidaId: jsonTransaction.Value<ulong>("vidaId"),
            receiver: jsonTransaction.Value<string>("receiver") ?? "",
            data: data,
            type: jsonTransaction.Value<string>("type") ?? "",
            signature: jsonTransaction.Value<string>("signature") ?? "",
            publicKey: jsonTransaction.Value<string>("publicKey") ?? "",
            guardian: jsonTransaction.Value<string>("guardian") ?? "",
            guardianSignature: jsonTransaction.Value<string>("guardianSignature") ?? "",
            guardianPublicKey: jsonTransaction.Value<string>("guardianPublicKey") ?? "",
            guardianNonce: jsonTransaction.Value<uint>("guardianNonce"),
            guardianTimeStamp: jsonTransaction.Value<ulong>("guardianTimeStamp"),
            guardianTransactionHash: jsonTransaction.Value<string>("guardianTransactionHash") ?? "",
            guardianFeePerByte: jsonTransaction.Value<ulong>("guardianFeePerByte"),
            guardianSize: jsonTransaction.Value<uint>("guardianSize"),
            guardianSender: jsonTransaction.Value<string>("guardianSender") ?? "",
            guardianSuccess: jsonTransaction.Value<bool>("guardianSuccess"),
            guardianBlockNumber: jsonTransaction.Value<uint>("guardianBlockNumber"),
            guardianPositionInTheBlock: jsonTransaction.Value<uint>("guardianPositionInTheBlock"),
            guardianVidaId: jsonTransaction.Value<ulong>("guardianVidaId"),
            guardianReceiver: jsonTransaction.Value<string>("guardianReceiver") ?? "",
            guardianData: guardianData
        );
    }

    /// <summary>
    /// Subscribes to VIDA transactions for a specific VIDA with custom polling interval
    /// </summary>
    /// <param name="vidaId">The VIDA ID to subscribe to</param>
    /// <param name="startingBlock">The block number to start checking from</param>
    /// <param name="handler">The handler for processing transactions</param>
    /// <param name="pollInterval">Interval in milliseconds between polling</param>
    /// <returns>The subscription object that can be used to control the subscription</returns>
    public VidaTransactionSubscription SubscribeToVidaTransactions(
        ulong vidaId, 
        ulong startingBlock, 
        VidaTransactionHandler handler, 
        int pollInterval)
    {
        var subscription = new VidaTransactionSubscription(this, vidaId, startingBlock, handler, pollInterval);
        subscription.Start();
        return subscription;
    }

    /// <summary>
    /// Subscribes to VIDA transactions for a specific VIDA with default polling interval (100ms)
    /// </summary>
    /// <param name="vidaId">The VIDA ID to subscribe to</param>
    /// <param name="startingBlock">The block number to start checking from</param>
    /// <param name="handler">The handler for processing transactions</param>
    /// <returns>The subscription object that can be used to control the subscription</returns>
    public VidaTransactionSubscription SubscribeToVidaTransactions(
        ulong vidaId,
        ulong startingBlock,
        VidaTransactionHandler handler)
    {
        return SubscribeToVidaTransactions(vidaId, startingBlock, handler, 100);
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

    public  void ValidateTxn(string txn)
    {
        if (txn.Length != 66 || !txn.StartsWith("0x"))
        {
            throw new ArgumentException("Invalid Address Format.");
        }
        
        Regex hexPattern = new Regex(@"^0x[a-fA-F0-9]{64}$");
        if (!hexPattern.IsMatch(txn)) throw new ArgumentException("Invalid Address Format.");

    }
}
