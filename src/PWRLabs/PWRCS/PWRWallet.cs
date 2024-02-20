using System.Numerics;
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

}