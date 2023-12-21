using System.Numerics;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Model;
using Nethereum.Signer;
using Nethereum.Util;
using PWRPY.Models;

namespace PWRPY;

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
    
    public async Task<int> GetBalance()
    {
        var response = await _apiSdk.GetNonceOfAddress(PublicAddress);
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
}