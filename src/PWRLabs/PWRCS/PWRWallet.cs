using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Model;
using Nethereum.Signer;
using Nethereum.Util;
using PWRCS.Models;

namespace PWRCS;

public class PwrWallet
{
    private readonly PwrApiSdk _apiSdk;

    private readonly EthECKey _ecKey;

    public PwrWallet(PwrApiSdk apiSdk) : this(apiSdk, EthECKey.GenerateKey().GetPrivateKeyAsBytes().ToHex())
    {
        _apiSdk = apiSdk;
    }

 public PwrWallet(PwrApiSdk apiSdk, string privateKeyHex)
{
    _apiSdk = apiSdk;

    try
    {
        _ecKey = new EthECKey(privateKeyHex);

        PrivateKeyHex = _ecKey.GetPrivateKeyAsBytes().ToHex();
        PublicKeyHex = _ecKey.GetPubKey().ToHex();

        PublicAddress = _ecKey.GetPublicAddress();

        Console.WriteLine("Private Key: " + PrivateKeyHex);
        Console.WriteLine("Public Key: " + PublicKeyHex);
        Console.WriteLine("Public Address: " + PublicAddress);
    }
    catch (Exception ex)
    {
        Console.WriteLine("Error generating keys: " + ex.Message);
    }
}

    public PwrWallet(PwrApiSdk apiSdk, EthECKey key)
    {
        _apiSdk = apiSdk;

        PrivateKeyHex = key.GetPrivateKeyAsBytes().ToHex();
        PublicKeyHex = key.GetPubKey().ToHex();

        PublicAddress = key.GetPublicAddress();
    }

    public string PrivateKeyHex { get; }

    public string PublicKeyHex { get; }

    public string PublicAddress { get; }

    public static EthECKey ConvertToEthECKey(BigInteger privateKeyBiguint)
    {
        byte[] privateKeyBytes = privateKeyBiguint.ToByteArray();

        if (privateKeyBytes.Length < 32)
        {
            byte[] paddedBytes = new byte[32];
            Array.Copy(privateKeyBytes, 0, paddedBytes, 32 - privateKeyBytes.Length, privateKeyBytes.Length);
            privateKeyBytes = paddedBytes;
        }
        else if (privateKeyBytes.Length > 32)
        {
            throw new ArgumentException("Biguinteger is too large to represent an Ethereum private key");
        }

        string privateKeyHex = "0x" + BitConverter.ToString(privateKeyBytes).Replace("-", "");

        EthECKey ecKey = new EthECKey(privateKeyHex);

        return ecKey;
    }
    private WalletResponse CreateWalletResponse<T>(ApiResponse<T> response, byte[]? finalTxn = null)
    {
        if (response.Success && finalTxn != null)
        {
            var txnHash = new Sha3Keccack().CalculateHash(finalTxn).ToHex();
            return new WalletResponse(true, "0x" + txnHash);
        }
        else
        {
            return new WalletResponse(false, null, response.Message);
        }
    }
    public async Task<uint> GetNonce()
    {
        var response = await _apiSdk.GetNonceOfAddress(PublicAddress);
        if (!response.Success)
            throw new Exception(response.Message);

        return response.Data;
    }
    public async Task<ulong> GetBalance()
    {
        var response = await _apiSdk.GetBalanceOfAddress(PublicAddress);
        if (!response.Success)
            throw new Exception(response.Message);

        return response.Data;
    }
      public byte[] GetSignedTxn(byte[] txn){
        if(txn == null) throw new ArgumentException("txn cannot be null");

        byte[] signature = Signer.SignMessage(_ecKey,txn);
        Console.WriteLine("Base txn length : " + txn.Length);
        Console.WriteLine("signature length : " + signature.Length);
        int finalTxnLength = txn.Length + 65;

        byte[] finalTxn = new byte[finalTxnLength];

        Array.Copy(txn, 0, finalTxn, 0, txn.Length);
        Array.Copy(signature, 0, finalTxn, txn.Length, signature.Length);
        Console.WriteLine("Final txn legnth : " + finalTxn.Length);
        return finalTxn;
    }
    public async Task<byte[]> GetTxnBase(byte identifier, uint nonce)  {
        
        MemoryStream stream = new MemoryStream(6);

        stream.WriteByte(identifier);
        byte chainId = await _apiSdk.GetChainId();
       
        stream.WriteByte(chainId);
        
        byte[] nonceBytes = BitConverter.GetBytes(nonce);
        stream.Write(nonceBytes, 0, nonceBytes.Length);
        Console.WriteLine("here " + stream.ToArray().Length);
        return stream.ToArray();

     }
    public async Task<byte[]> GetTransferPWRTxn(string to, ulong amount, uint nonce)
    {
        ValidateAddress(to);
       
        to = to.Substring(2);
        
        byte[] txnBase = await GetTxnBase(0, nonce);
        Console.WriteLine("txn base lenth : " + txnBase.Length);
        using (MemoryStream stream = new MemoryStream(txnBase.Length + 8 + 20))
        {
            stream.Write(txnBase, 0, txnBase.Length);
            byte[] amountBytes = BitConverter.GetBytes(amount);
            stream.Write(amountBytes, 0, amountBytes.Length);
            byte[] toBytes = Extensions.HexStringToByteArray(to);
            stream.Write(toBytes, 0, toBytes.Length);
            return stream.ToArray();
        }
    }
    public async Task<byte[]> GetSignedTransferPWRTxn(string to, ulong amount, uint nonce){
        return GetSignedTxn(await GetTransferPWRTxn(to,amount,nonce));
     }
    public async Task<WalletResponse> TransferPWR(string to, ulong amount, uint nonce)   {
        var signed = await GetSignedTransferPWRTxn(to, amount, nonce);
        return CreateWalletResponse(await _apiSdk.BroadcastTxn(signed),signed);
    }
    public async Task<WalletResponse> TransferPWR(string to, ulong amount)   {
        return await TransferPWR(to,amount,await GetNonce());
    }
    public async Task<byte[]> GetJointxn(string ip, uint nonce)
    {
        byte[] txnBase = await GetTxnBase(1, nonce);
        byte[] ipBytes = Encoding.UTF8.GetBytes(ip);

        using MemoryStream stream = new MemoryStream(txnBase.Length + ipBytes.Length);
        stream.Write(txnBase, 0, txnBase.Length);
        stream.Write(ipBytes, 0, ipBytes.Length);
        return stream.ToArray();
    }
    public async Task<byte[]> GetSignedJointxn(string ip, uint nonce)   {
        return GetSignedTxn(await GetJointxn(ip, nonce));
    }
    public async Task<WalletResponse> Join(string ip, uint nonce)   {
        return CreateWalletResponse(await _apiSdk.BroadcastTxn(await GetSignedJointxn(ip, nonce)));
    }
    public async Task<WalletResponse> Join(string ip)   {
        return await Join(ip,await GetNonce());
    }
    public async Task<byte[]> GetClaimActiveNodeSpotTxn(uint nonce)   {
        byte[] txnBase = await GetTxnBase((byte) 2, nonce);
        return txnBase;
    }
    public async Task<byte[]> GetSignedClaimActiveNodeSpotTxn(uint nonce)   {
        return GetSignedTxn(await GetClaimActiveNodeSpotTxn(nonce));
    }
    public async Task<WalletResponse> ClaimActiveNodeSpot(uint nonce)   {
        return CreateWalletResponse(await _apiSdk.BroadcastTxn(await GetSignedClaimActiveNodeSpotTxn(nonce)));
    }
    public async Task<WalletResponse> ClaimActiveNodeSpot()   {
        return await ClaimActiveNodeSpot(await GetNonce());
    }
    public async Task<byte[]> GetDelegateTxn(string to, ulong amount, uint nonce)
    {
        ValidateAddress(to);
         to = to.Substring(2);
       

        byte[] txnBase = await GetTxnBase(3, nonce);
        byte[] toBytes = Extensions.HexStringToByteArray(to);

        using (MemoryStream stream = new MemoryStream(txnBase.Length + 8 + toBytes.Length))
        {
            stream.Write(txnBase, 0, txnBase.Length);
            byte[] amountBytes = BitConverter.GetBytes(amount);
            stream.Write(amountBytes, 0, amountBytes.Length);
            stream.Write(toBytes, 0, toBytes.Length);
            return stream.ToArray();
        }
    }
    public async Task<byte[]> GetSignedDelegateTxn(string to, ulong amount, uint nonce)   {
        return GetSignedTxn(await GetDelegateTxn(to, amount, nonce));
    }
    public async Task<WalletResponse> Delegate(string to, ulong amount, uint nonce){
        return CreateWalletResponse(await _apiSdk.BroadcastTxn(await GetSignedDelegateTxn(to,amount,nonce)));
    }
    public async Task<WalletResponse> Delegate(string to, ulong amount){
        return await Delegate(to,amount,await GetNonce());
    }
    public async Task<byte[]> GetWithdrawTxn(string from, ulong sharesAmount, uint nonce)
    {
        ValidateAddress(from);
            from = from.Substring(2);

        byte[] txnBase = await GetTxnBase(4, nonce);
        byte[] fromBytes = Extensions.HexStringToByteArray(from);

        using (MemoryStream stream = new MemoryStream(txnBase.Length + 8 + fromBytes.Length))
        {
            stream.Write(txnBase, 0, txnBase.Length);
            byte[] sharesAmountBytes = BitConverter.GetBytes(sharesAmount);
            stream.Write(sharesAmountBytes, 0, sharesAmountBytes.Length);
            stream.Write(fromBytes, 0, fromBytes.Length);
            return stream.ToArray();
        }
    }
    public async Task<byte[]> GetSignedWithdrawTxn(string from, ulong sharesAmount, uint nonce)   {
        return GetSignedTxn(await GetWithdrawTxn(from, sharesAmount, nonce));
    }
    public async Task<WalletResponse> WithDraw(string to, ulong amount, uint nonce){
        return CreateWalletResponse(await _apiSdk.BroadcastTxn(await GetSignedWithdrawTxn(to,amount,nonce)));
    }
    public async Task<WalletResponse> WithDraw(string to, ulong amount){
        return await WithDraw(to,amount,await GetNonce());
    }
    public async Task<byte[]> GetWithdrawPWRTxn(string from, ulong pwrAmount, uint nonce)
    {
       ValidateAddress(from);
            from = from.Substring(2);

        BigDecimal shareValue = await _apiSdk.GetShareValue(from);
        
        ulong sharesAmount = (ulong)(pwrAmount / 5000);

        if (sharesAmount <= 0)
        {
            throw new ArgumentException("Shares amount is too low");
        }

        byte[] txnBase = await GetTxnBase(4, nonce);
        byte[] fromBytes = Extensions.HexStringToByteArray(from);

        using (MemoryStream stream = new MemoryStream(txnBase.Length + 8 + fromBytes.Length))
        {
            stream.Write(txnBase, 0, txnBase.Length);
            byte[] sharesAmountBytes = BitConverter.GetBytes(sharesAmount);
            stream.Write(sharesAmountBytes, 0, sharesAmountBytes.Length);
            stream.Write(fromBytes, 0, fromBytes.Length);
            return stream.ToArray();
        }
    }
    public async Task<byte[]> GetSignedWithdrawPWRTxnAsync(string from, ulong pwrAmount, uint nonce)   {
        return GetSignedTxn(await GetWithdrawPWRTxn(from, pwrAmount, nonce));
    }
    public async Task<WalletResponse> WithDrawPWR(string to, ulong amount, uint nonce){
        return CreateWalletResponse(await _apiSdk.BroadcastTxn(await GetSignedWithdrawTxn(to,amount,nonce)));
    }
    public async Task<WalletResponse> WithDrawPWR(string to, ulong amount){
        return await WithDraw(to,amount,await GetNonce());
    }
    public async Task<byte[]> GetSendVmDataTxn(ulong vmId, byte[] data, uint nonce)
    {
        
        if (nonce < await GetNonce())
        {
            throw new ArgumentException("Nonce is too low");
        }

        byte[] txnBase = await GetTxnBase(5, nonce);

        using (MemoryStream stream = new MemoryStream(txnBase.Length + 8 + data.Length))
        {
            stream.Write(txnBase, 0, txnBase.Length);
            byte[] vmIdBytes = BitConverter.GetBytes(vmId);
            stream.Write(vmIdBytes, 0, vmIdBytes.Length);
            stream.Write(data, 0, data.Length);
            return stream.ToArray();
        }
    }
    public async Task<byte[]> GetSignedSendVmDataTxn(ulong vmId, byte[] data, uint nonce)  {
        return GetSignedTxn(await GetSendVmDataTxn(vmId, data, nonce));
    }
    public async Task<WalletResponse> SendVmDataTxn(ulong vmId,byte[] data, uint nonce){
        return CreateWalletResponse(await _apiSdk.BroadcastTxn(await GetSignedSendVmDataTxn(vmId,data,nonce)));
    }
    public async Task<WalletResponse> SendVmDataTxn(ulong vmId,byte[] data){
        return await SendVmDataTxn(vmId,data,await GetNonce());
    }
    public async Task<byte[]> GetClaimVmIdTxn(ulong vmId, uint nonce)
    {
        byte[] txnBase = await GetTxnBase(6, nonce);

        using (MemoryStream stream = new MemoryStream(txnBase.Length + 8))
        {
            stream.Write(txnBase, 0, txnBase.Length);
            byte[] vmIdBytes = BitConverter.GetBytes(vmId);
            stream.Write(vmIdBytes, 0, vmIdBytes.Length);
            return stream.ToArray();
        }
    }
    public async Task<byte[]> GetSignedClaimVmIdTxn(ulong vmId, uint nonce)   {
        return GetSignedTxn(await GetClaimVmIdTxn(vmId, nonce));
    }
    public async Task<WalletResponse> ClaimVmId(ulong vmid,uint nonce){
        return CreateWalletResponse(await _apiSdk.BroadcastTxn(await GetSignedClaimVmIdTxn(vmid,nonce)));
    }
    public async Task<WalletResponse> ClaimVmId(ulong vmId){
        return await ClaimVmId(vmId,await GetNonce());
    }
    public async Task<byte[]> GetSendConduitTransactionTxn(ulong vmId, byte[] txn, uint nonce)
    {
       
        if (nonce < await GetNonce())
        {
            throw new ArgumentException("Nonce is too low");
        }

        byte[] txnBase = await GetTxnBase(11, nonce);

        using (MemoryStream stream = new MemoryStream(txnBase.Length + 8 + txn.Length))
        {
            stream.Write(txnBase, 0, txnBase.Length);
            byte[] vmIdBytes = BitConverter.GetBytes(vmId);
            stream.Write(vmIdBytes, 0, vmIdBytes.Length);
            stream.Write(txn, 0, txn.Length);
            return stream.ToArray();
        }
    }
    public async Task<byte[]> GetSignedSendConduitTransactionTxn(ulong vmId, byte[] txn, uint nonce) {
        return GetSignedTxn(await GetSendConduitTransactionTxn(vmId, txn, nonce));
    }
    public async Task<WalletResponse> SendConduitTransaction(ulong vmId, byte[] txn, uint nonce){
        return CreateWalletResponse(await _apiSdk.BroadcastTxn(await GetSignedSendConduitTransactionTxn(vmId,txn, nonce)));
    }
    public async Task<WalletResponse> SendConduitTransaction(ulong vmId, byte[] txn){
        return await SendConduitTransaction(vmId,txn,await GetNonce());
    }
    public async Task<byte[]> GetSetGuardianTxn(string guardianAddress, ulong expiryDate, uint nonce)
    {
        ValidateAddress(guardianAddress);
        if (expiryDate < (ulong) DateTimeOffset.Now.ToUnixTimeSeconds())
        {
            throw new ArgumentException("Expiry date cannot be in the past");
        }

        if (guardianAddress.Length == 42)
        {
            guardianAddress = guardianAddress.Substring(2);
        }

        byte[] txnBase = await GetTxnBase(8, nonce);
        byte[] guardianAddressBytes = Extensions.HexStringToByteArray(guardianAddress);

        using (MemoryStream stream = new MemoryStream(txnBase.Length + 20 + guardianAddressBytes.Length))
        {
            stream.Write(txnBase, 0, txnBase.Length);
            byte[] expiryDateBytes = BitConverter.GetBytes(expiryDate);
            stream.Write(expiryDateBytes, 0, expiryDateBytes.Length);
            stream.Write(guardianAddressBytes, 0, guardianAddressBytes.Length);
            return stream.ToArray();
        }
    }
    public async Task<byte[]> GetSignedSetGuardianTxn(string guardianAddress, ulong expiryDate, uint nonce)   {
        return GetSignedTxn(await GetSetGuardianTxn(guardianAddress, expiryDate, nonce));
    }
    public async Task<byte[]> GetSignedSetGuardianTxn(string guardianAddress, ulong expiryDate)   {
        return await GetSignedSetGuardianTxn(guardianAddress,expiryDate,await GetNonce());
    }
    public async Task<WalletResponse> SetGuardian(string guardianAddress, ulong expiryDate, uint nonce){
        return CreateWalletResponse(await _apiSdk.BroadcastTxn(await GetSignedSetGuardianTxn(guardianAddress,expiryDate,nonce)));
    }
    public async Task<WalletResponse> SetGuardian(string guardianAddress, ulong expiryDate){
        return await SetGuardian(guardianAddress,expiryDate,await GetNonce());
    }
     public async Task<byte[]> GetRemoveGuardianTxn(uint nonce)
    {
        byte[] txnBase = await GetTxnBase(9, nonce);
        return txnBase;
    }
    public async Task<byte[]> GetSignedRemoveGuardianTxn(uint nonce)   {
        return GetSignedTxn(await GetRemoveGuardianTxn(nonce));
    }
    public async Task<byte[]> GetSignedRemoveGuardianTxn()  {
        return GetSignedTxn(await GetRemoveGuardianTxn(await GetNonce()));
    }
    public async Task<WalletResponse> RemoveGuardian(uint nonce)   {
        return CreateWalletResponse(await _apiSdk.BroadcastTxn(await GetSignedRemoveGuardianTxn(nonce)));
    }
     public async Task<WalletResponse> RemoveGuardian()   {
        return await RemoveGuardian(await GetNonce());
    }
    public async Task<byte[]> GetSendGuardianWrappedTransactionTxn(byte[] txn, uint nonce)
    {
        byte[] txnBase = await GetTxnBase(10, nonce);

        using (MemoryStream stream = new MemoryStream(txnBase.Length + txn.Length))
        {
            stream.Write(txnBase, 0, txnBase.Length);
            stream.Write(txn, 0, txn.Length);
            return stream.ToArray();
        }
    }
    public async Task<byte[]> GetSignedSendGuardianWrappedTransactionTxn(byte[] txn, uint nonce)   {
        return GetSignedTxn(await GetSendGuardianWrappedTransactionTxn(txn, nonce));
    } 
    public async Task<WalletResponse> SendGuardianWrappedTransaction(byte[] txn, uint nonce)   {
        return CreateWalletResponse(await _apiSdk.BroadcastTxn(await GetSignedSendGuardianWrappedTransactionTxn(txn, nonce)));
    }
    public async Task<WalletResponse> SendGuardianWrappedTransaction(byte[] txn)   {
        return await SendGuardianWrappedTransaction(txn,await GetNonce());
    }
    public async Task<byte[]> GetSendValidatorRemoveTxn(string validator, uint nonce)
    {
       ValidateAddress(validator);
            validator = validator.Substring(2);
       

        byte[] txnBase = await GetTxnBase(7, nonce);
        byte[] validatorBytes = Extensions.HexStringToByteArray(validator);

        using (MemoryStream stream = new MemoryStream(txnBase.Length + 20))
        {
            stream.Write(txnBase, 0, txnBase.Length);
            stream.Write(validatorBytes, 0, validatorBytes.Length);
            return stream.ToArray();
        }
    }
    public async Task<byte[]> GetSignedSendValidatorRemoveTxn(string validator, uint nonce)  {
        return GetSignedTxn(await GetSendValidatorRemoveTxn(validator, nonce));
    }
    public async Task<WalletResponse> SendValidatorRemoveTxn(string validator, uint nonce){
        return CreateWalletResponse(await _apiSdk.BroadcastTxn(await GetSignedSendValidatorRemoveTxn(validator, nonce)));
    }
    public async Task<WalletResponse> SendValidatorRemoveTxn(string validator){
        return await SendValidatorRemoveTxn(validator,await GetNonce());
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