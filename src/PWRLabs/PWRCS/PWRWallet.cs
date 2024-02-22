using System.Numerics;
using System.Text;
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

        _ecKey = new EthECKey(privateKeyHex);

        PrivateKeyHex = _ecKey.GetPrivateKeyAsBytes().ToHex();
        PublicKeyHex = _ecKey.GetPubKey().ToHex();

        PublicAddress = _ecKey.GetPublicAddress();
    }

    public string PrivateKeyHex { get; }

    public string PublicKeyHex { get; }

    public string PublicAddress { get; }

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

    public async Task<int> GetNonce()
    {
        var response = await _apiSdk.GetNonceOfAddress(PublicAddress);
        if (!response.Success)
            throw new Exception(response.Message);

        return response.Data;
    }
    
    public async Task<decimal> GetBalance()
    {
        var response = await _apiSdk.GetBalanceOfAddress(PublicAddress);
        if (!response.Success)
            throw new Exception(response.Message);

        return response.Data;
    }

    public async Task<WalletResponse> TransferPwr(string to, int amount, int? nonce = null)
    {
        if (!nonce.HasValue)
        {
            var nonceResponse = await _apiSdk.GetNonceOfAddress(PublicAddress);
            if (!nonceResponse.Success)
                return new WalletResponse(false, null, nonceResponse.Message);
            nonce = nonceResponse.Data;
        }

        if (to.Length != 42)
            throw new ArgumentException("Invalid address");
        if (amount < 0)
            throw new ArgumentException("Amount cannot be negative");
        if (nonce < 0)
            throw new ArgumentException("Nonce cannot be negative");

        byte[] txnBuffer = new byte[33];
        txnBuffer[0] = 0;
        Array.Copy(BitConverter.GetBytes(nonce.Value), 0, txnBuffer, 1, 4);
        Array.Copy(BitConverter.GetBytes(amount), 0, txnBuffer, 5, 4);
        var addressBytes = to[2..].HexStringToByteArray();
        Array.Copy(addressBytes, 0, txnBuffer, 13, 20);

        byte[] txn = new byte[33];
        Array.Copy(txnBuffer, txn, 33);
        var signature = Signer.SignMessage(_ecKey, txn);

        byte[] finalTxn = new byte[98];
        Array.Copy(txn, 0, finalTxn, 0, 33);
        Array.Copy(signature, 0, finalTxn, 33, 65);

        var response = await _apiSdk.BroadcastTxn(finalTxn);
        return CreateWalletResponse(response, finalTxn);
    }
    
    public async Task<WalletResponse> SendVmDataTxn(long vmId, byte[] data, int? nonce = null)
    {
        if (!nonce.HasValue)
        {
            var nonceResponse = await _apiSdk.GetNonceOfAddress(PublicAddress);
            if (!nonceResponse.Success)
                return new WalletResponse(false, null, nonceResponse.Message);
            nonce = nonceResponse.Data;
        }

        if (nonce < 0)
            throw new ArgumentException("Nonce cannot be negative");
        if (nonce < await GetNonce())
            throw new ArgumentException("Nonce is too low");

        int dataLen = data.Length;
        byte[] buffer = new byte[13 + dataLen];
        buffer[0] = 5;
        Array.Copy(BitConverter.GetBytes(nonce.Value), 0, buffer, 1, 4);
        Array.Copy(BitConverter.GetBytes(vmId), 0, buffer, 5, 8);
        Array.Copy(data, 0, buffer, 13, dataLen);
        byte[] txn = buffer;
        byte[] signature = Signer.SignMessage(_ecKey, txn);

        byte[] finalTxn = new byte[13 + 65 + dataLen];
        Array.Copy(txn, finalTxn, txn.Length);
        Array.Copy(signature, 0, finalTxn, txn.Length, 65);

        var response = await _apiSdk.BroadcastTxn(finalTxn);
        return CreateWalletResponse(response, finalTxn);
    }
    
    public async Task<WalletResponse> Delegate(string to, long amount, int? nonce = null)
    {
        if (!nonce.HasValue)
        {
            var nonceResponse = await _apiSdk.GetNonceOfAddress(PublicAddress);
            if (!nonceResponse.Success)
                return new WalletResponse(false, null, nonceResponse.Message);
            nonce = nonceResponse.Data;
        }

        byte[] buffer = new byte[33];
        buffer[0] = 3;
        Array.Copy(BitConverter.GetBytes(nonce.Value), 0, buffer, 1, 4);
        Array.Copy(BitConverter.GetBytes(amount), 0, buffer, 5, 8);
        Array.Copy(to[2..].HexStringToByteArray(), 0, buffer, 13, 20);
        byte[] txn = buffer;
        byte[] signature = Signer.SignMessage(_ecKey, txn);

        int txnLen = txn.Length;
        byte[] finalTxn = new byte[txnLen + 65];
        Array.Copy(txn, finalTxn, txnLen);
        Array.Copy(signature, 0, finalTxn, txnLen, 65);

        var response = await _apiSdk.BroadcastTxn(finalTxn);
        return CreateWalletResponse(response, finalTxn);
    }
    
    public async Task<WalletResponse> Withdraw(string fromWallet, long sharesAmount, int? nonce = null)
    {
        if (!nonce.HasValue)
        {
            var nonceResponse = await _apiSdk.GetNonceOfAddress(PublicAddress);
            if (!nonceResponse.Success)
                return new WalletResponse(false, null, nonceResponse.Message);
            nonce = nonceResponse.Data;
        }

        byte[] buffer = new byte[33];
        buffer[0] = 4;
        Array.Copy(BitConverter.GetBytes(nonce.Value), 0, buffer, 1, 4);
        Array.Copy(BitConverter.GetBytes(sharesAmount), 0, buffer, 5, 8);
        Array.Copy(fromWallet[2..].HexStringToByteArray(), 0, buffer, 13, 20);
        byte[] txn = buffer;
        byte[] signature = Signer.SignMessage(_ecKey, txn);

        int txnLen = txn.Length;
        byte[] finalTxn = new byte[txnLen + 65];
        Array.Copy(txn, finalTxn, txnLen);
        Array.Copy(signature, 0, finalTxn, txnLen, 65);

        var response = await _apiSdk.BroadcastTxn(finalTxn);
        return CreateWalletResponse(response, finalTxn);
    }

    public async Task<WalletResponse> ClaimVmId(long vmId, int? nonce = null)
    {
        if (!nonce.HasValue)
        {
            var nonceResponse = await _apiSdk.GetNonceOfAddress(PublicAddress);
            if (!nonceResponse.Success)
                return new WalletResponse(false, null, nonceResponse.Message);
            nonce = nonceResponse.Data;
        }

        byte[] buffer = new byte[13];
        buffer[0] = 6;
        Array.Copy(BitConverter.GetBytes(nonce.Value), 0, buffer, 1, 4);
        Array.Copy(BitConverter.GetBytes(vmId), 0, buffer, 5, 8);
        byte[] txn = buffer;
        byte[] signature = Signer.SignMessage(_ecKey, txn);

        int txnLen = txn.Length;
        byte[] finalTxn = new byte[txnLen + 65];
        Array.Copy(txn, finalTxn, txnLen);
        Array.Copy(signature, 0, finalTxn, txnLen, 65);

        var response = await _apiSdk.BroadcastTxn(finalTxn);
        return CreateWalletResponse(response, finalTxn);
    }
    
    public byte[] GetSignedTxn(byte[] txn){
        if(txn == null) throw new ArgumentException("txn cannot be null");

        byte[] signature = Signer.SignMessage(_ecKey,txn);

        int finalTxnLength = txn.Length + 65;

        byte[] finalTxn = new byte[finalTxnLength];

        Array.Copy(txn, 0, finalTxn, 0, txn.Length);
        Array.Copy(signature, 0, finalTxn, txn.Length, signature.Length);

        return finalTxn;
    }

     public async Task<byte[]> GetTxnBase(byte identifier, int nonce)  {
         MemoryStream stream = new MemoryStream(6);

        stream.WriteByte(identifier);
        int chainId = await _apiSdk.GetChainId();
        byte[] chainIdBytes = BitConverter.GetBytes(chainId);
        stream.Write(chainIdBytes, 0, chainIdBytes.Length);
        
        byte[] nonceBytes = BitConverter.GetBytes(nonce);
        stream.Write(nonceBytes, 0, nonceBytes.Length);

        return stream.ToArray();

     }

     public async Task<byte[]> GetTransferPWRTxn(string to, long amount, int nonce)
    {
        if (to.Length != 40 && to.Length != 42)
        {
            throw new ArgumentException("Invalid address");
        }
        if (amount < 0)
        {
            throw new ArgumentException("Amount cannot be negative");
        }
        if (nonce < 0)
        {
            throw new ArgumentException("Nonce cannot be negative");
        }

        if (to.Length == 42)
        {
            to = to.Substring(2);
        }

        byte[] txnBase = await GetTxnBase(0, nonce);
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


     public async Task<byte[]> GetSignedTransferPWRTxn(String to, long amount, int nonce){
        return GetSignedTxn(await GetTransferPWRTxn(to,amount,nonce));
     }

     public async Task<ApiResponse> TransferPWR(String to, long amount, int nonce)   {
        return await _apiSdk.BroadcastTxn(await GetSignedTransferPWRTxn(to, amount, nonce));
    }
    

     public async Task<ApiResponse> TransferPWR(String to, long amount)   {
        return await TransferPWR(to,amount,await GetNonce());
    }


     public async Task<byte[]> GetJoinTxn(string ip, int nonce)
    {
        byte[] txnBase = await GetTxnBase(1, nonce);
        byte[] ipBytes = Encoding.UTF8.GetBytes(ip);

        using MemoryStream stream = new MemoryStream(txnBase.Length + ipBytes.Length);
        stream.Write(txnBase, 0, txnBase.Length);
        stream.Write(ipBytes, 0, ipBytes.Length);
        return stream.ToArray();
    }

    public async Task<byte[]> GetSignedJoinTxn(String ip, int nonce)   {
        return GetSignedTxn(await GetJoinTxn(ip, nonce));
    }

    public async Task<ApiResponse> Join(String ip, int nonce)   {
        return await _apiSdk.BroadcastTxn(await GetSignedJoinTxn(ip, nonce));
    }

    public async Task<ApiResponse> Join(String ip)   {
        return await Join(ip,await GetNonce());
    }

    public async Task<byte[]> GetClaimActiveNodeSpotTxn(int nonce)   {
        byte[] txnBase = await GetTxnBase((byte) 2, nonce);
        return txnBase;
    }


    public async Task<byte[]> GetSignedClaimActiveNodeSpotTxn(int nonce)   {
        return GetSignedTxn(await GetClaimActiveNodeSpotTxn(nonce));
    }

    public async Task<ApiResponse> ClaimActiveNodeSpot(int nonce)   {
        return await _apiSdk.BroadcastTxn(await GetSignedClaimActiveNodeSpotTxn(nonce));
    }

    public async Task<ApiResponse> ClaimActiveNodeSpot()   {
        return await ClaimActiveNodeSpot(await GetNonce());
    }


    public async Task<byte[]> GetDelegateTxn(string to, long amount, int nonce)
    {
        if (to.Length != 40 && to.Length != 42)
        {
            throw new ArgumentException("Invalid address");
        }
        if (amount < 0)
        {
            throw new ArgumentException("Amount cannot be negative");
        }
        if (nonce < 0)
        {
            throw new ArgumentException("Nonce cannot be negative");
        }

        if (to.Length == 42)
        {
            to = to.Substring(2);
        }

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

    public async Task<byte[]> GetSignedDelegateTxn(String to, long amount, int nonce)   {
        return GetSignedTxn(await GetDelegateTxn(to, amount, nonce));
    }

    public async Task<ApiResponse> Delegate(String to, long amount, int nonce){
        return await _apiSdk.BroadcastTxn(await GetSignedDelegateTxn(to,amount,nonce));
    }

     public async Task<ApiResponse> Delegate(String to, long amount){
        return await Delegate(to,amount,await GetNonce());
    }

     public async Task<byte[]> GetWithdrawTxn(string from, long sharesAmount, int nonce)
    {
        if (from.Length != 40 && from.Length != 42)
        {
            throw new ArgumentException("Invalid address");
        }
        if (sharesAmount < 0)
        {
            throw new ArgumentException("Shares amount cannot be negative");
        }
        if (nonce < 0)
        {
            throw new ArgumentException("Nonce cannot be negative");
        }

        if (from.Length == 42)
        {
            from = from.Substring(2);
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
    
    public async Task<byte[]> GetSignedWithdrawTxn(String from, long sharesAmount, int nonce)   {
        return GetSignedTxn(await GetWithdrawTxn(from, sharesAmount, nonce));
    }
    public async Task<ApiResponse> WithDraw(String to, long amount, int nonce){
        return await _apiSdk.BroadcastTxn(await GetSignedWithdrawTxn(to,amount,nonce));
    }

    public async Task<ApiResponse> WithDraw(String to, long amount){
        return await WithDraw(to,amount,await GetNonce());
    }

    public async Task<byte[]> GetWithdrawPWRTxn(string from, long pwrAmount, int nonce)
    {
        if (from.Length != 40 && from.Length != 42)
        {
            throw new ArgumentException("Invalid address");
        }
        if (pwrAmount < 0)
        {
            throw new ArgumentException("PWR amount cannot be negative");
        }
        if (nonce < 0)
        {
            throw new ArgumentException("Nonce cannot be negative");
        }

        if (from.Length == 42)
        {
            from = from.Substring(2);
        }

        decimal shareValue = await _apiSdk.GetShareValue(from);
        long sharesAmount = (long)(pwrAmount / shareValue);

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
    
    public async Task<byte[]> GetSignedWithdrawPWRTxnAsync(String from, long pwrAmount, int nonce)   {
        return GetSignedTxn(await GetWithdrawPWRTxn(from, pwrAmount, nonce));
    }

    public async Task<ApiResponse> WithDrawPWR(String to, long amount, int nonce){
        return await _apiSdk.BroadcastTxn(await GetSignedWithdrawTxn(to,amount,nonce));
    }
     public async Task<ApiResponse> WithDrawPWR(String to, long amount){
        return await WithDraw(to,amount,await GetNonce());
    }

    public async Task<byte[]> GetSendVmDataTxn(long vmId, byte[] data, int nonce)
    {
        if (nonce < 0)
        {
            throw new ArgumentException("Nonce cannot be negative");
        }
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
    

     public async Task<byte[]> GetSignedSendVmDataTxn(long vmId, byte[] data, int nonce)  {
        return GetSignedTxn(await GetSendVmDataTxn(vmId, data, nonce));
    }

    public async Task<ApiResponse> SendVmDataTxn(long vmId,byte[] data, int nonce){
        return await _apiSdk.BroadcastTxn(await GetSignedSendVmDataTxn(vmId,data,nonce));
    }

    public async Task<ApiResponse> SendVmDataTxn(long vmId,byte[] data){
        return await SendVmDataTxn(vmId,data,await GetNonce());
    }

    public async Task<byte[]> GetClaimVmIdTxn(long vmId, int nonce)
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

    public async Task<byte[]> GetSignedClaimVmIdTxn(long vmId, int nonce)   {
        return GetSignedTxn(await GetClaimVmIdTxn(vmId, nonce));
    }
    public async Task<ApiResponse> ClaimVmId(long vmid,int nonce){
        return await _apiSdk.BroadcastTxn(await GetSignedClaimVmIdTxn(vmid,nonce));
    }

    public async Task<ApiResponse> ClaimVmId(long vmId){
        return await ClaimVmId(vmId,await GetNonce());
    }


     public async Task<byte[]> GetSendConduitTransactionTxn(long vmId, byte[] txn, int nonce)
    {
        if (nonce < 0)
        {
            throw new ArgumentException("Nonce cannot be negative");
        }
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


     public async Task<byte[]> GetSignedSendConduitTransactionTxn(long vmId, byte[] txn, int nonce) {
        return GetSignedTxn(await GetSendConduitTransactionTxn(vmId, txn, nonce));
    }

    public async Task<ApiResponse> SendConduitTransaction(long vmId, byte[] txn, int nonce){
        return await _apiSdk.BroadcastTxn(await GetSignedSendConduitTransactionTxn(vmId,txn, nonce));
    }

     public async Task<ApiResponse> SendConduitTransaction(long vmId, byte[] txn){
        return await SendConduitTransaction(vmId,txn,await GetNonce());
    }

    public async Task<byte[]> GetSetGuardianTxn(string guardianAddress, long expiryDate, int nonce)
    {
        if (guardianAddress.Length != 40 && guardianAddress.Length != 42)
        {
            throw new ArgumentException("Invalid address");
        }
        if (nonce < 0)
        {
            throw new ArgumentException("Nonce cannot be negative");
        }
        if (expiryDate < 0)
        {
            throw new ArgumentException("Expiry date cannot be negative");
        }
        if (expiryDate < DateTimeOffset.Now.ToUnixTimeSeconds())
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

     public async Task<byte[]> GetSignedSetGuardianTxn(String guardianAddress, long expiryDate, int nonce)   {
        return GetSignedTxn(await GetSetGuardianTxn(guardianAddress, expiryDate, nonce));
    }

     public async Task<byte[]> GetSignedSetGuardianTxn(String guardianAddress, long expiryDate)   {
        return await GetSignedSetGuardianTxn(guardianAddress,expiryDate,await GetNonce());
    }


    public async Task<ApiResponse> SetGuardian(String guardianAddress, long expiryDate, int nonce){
        return await _apiSdk.BroadcastTxn(await GetSignedSetGuardianTxn(guardianAddress,expiryDate,nonce));
    }

    public async Task<ApiResponse> SetGuardian(String guardianAddress, long expiryDate){
        return await SetGuardian(guardianAddress,expiryDate,await GetNonce());
    }

     public async Task<byte[]> GetRemoveGuardianTxn(int nonce)
    {
        byte[] txnBase = await GetTxnBase(9, nonce);
        return txnBase;
    }

    public async Task<byte[]> GetSignedRemoveGuardianTxn(int nonce)   {
        return GetSignedTxn(await GetRemoveGuardianTxn(nonce));
    }
    public async Task<byte[]> GetSignedRemoveGuardianTxn()  {
        return GetSignedTxn(await GetRemoveGuardianTxn(await GetNonce()));
    }

    public async Task<ApiResponse> RemoveGuardian(int nonce)   {
        return await _apiSdk.BroadcastTxn(await GetSignedRemoveGuardianTxn(nonce));
    }

     public async Task<ApiResponse> RemoveGuardian()   {
        return await RemoveGuardian(await GetNonce());
    }


    public async Task<byte[]> GetSendGuardianWrappedTransactionTxn(byte[] txn, int nonce)
    {
        byte[] txnBase = await GetTxnBase(10, nonce);

        using (MemoryStream stream = new MemoryStream(txnBase.Length + txn.Length))
        {
            stream.Write(txnBase, 0, txnBase.Length);
            stream.Write(txn, 0, txn.Length);
            return stream.ToArray();
        }
    }

    public async Task<byte[]> GetSignedSendGuardianWrappedTransactionTxn(byte[] txn, int nonce)   {
        return GetSignedTxn(await GetSendGuardianWrappedTransactionTxn(txn, nonce));
    } 

    public async Task<ApiResponse> SendGuardianWrappedTransaction(byte[] txn, int nonce)   {
        return await _apiSdk.BroadcastTxn(await GetSignedSendGuardianWrappedTransactionTxn(txn, nonce));
    }

     public async Task<ApiResponse> SendGuardianWrappedTransaction(byte[] txn)   {
        return await SendGuardianWrappedTransaction(txn,await GetNonce());
    }

    public async Task<byte[]> GetSendValidatorRemoveTxn(string validator, int nonce)
    {
        if (validator.Length != 40 && validator.Length != 42)
        {
            throw new ArgumentException("Invalid address");
        }
        if (nonce < 0)
        {
            throw new ArgumentException("Nonce cannot be negative");
        }

        if (validator.Length == 42)
        {
            validator = validator.Substring(2);
        }

        byte[] txnBase = await GetTxnBase(7, nonce);
        byte[] validatorBytes = Extensions.HexStringToByteArray(validator);

        using (MemoryStream stream = new MemoryStream(txnBase.Length + 20))
        {
            stream.Write(txnBase, 0, txnBase.Length);
            stream.Write(validatorBytes, 0, validatorBytes.Length);
            return stream.ToArray();
        }
    }
    public async Task<byte[]> GetSignedSendValidatorRemoveTxn(String validator, int nonce)  {
        return GetSignedTxn(await GetSendValidatorRemoveTxn(validator, nonce));
    }

    public async Task<ApiResponse> SendValidatorRemoveTxn(String validator, int nonce){
        return await _apiSdk.BroadcastTxn(await GetSignedSendValidatorRemoveTxn(validator, nonce));
    }

    public async Task<ApiResponse> SendValidatorRemoveTxn(String validator){
        return await SendValidatorRemoveTxn(validator,await GetNonce());
    }

}