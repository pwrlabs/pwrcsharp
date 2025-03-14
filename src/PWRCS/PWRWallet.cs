using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Model;
using Nethereum.Signer;
using Nethereum.Util;
using PWRCS.Models;

namespace PWRCS;

public class PwrWallet {
    private readonly PwrApiSdk _apiSdk;
    private readonly TransactionBuilder _txnBuilder;
    public EthECKey _ecKey { get; }

    // Default constructor initializes PwrApiSdk automatically
    public PwrWallet()
        : this(EthECKey.GenerateKey().GetPrivateKeyAsBytes().ToHex()) {}

    // Constructor with a private key string
    public PwrWallet(string? privateKeyHex = null) {
        _apiSdk = new PwrApiSdk("https://pwrrpc.pwrlabs.io/");

        try {
            _txnBuilder = new TransactionBuilder();
            _ecKey = new EthECKey(privateKeyHex);
            PrivateKeyHex = _ecKey.GetPrivateKeyAsBytes().ToHex();
            PublicKeyHex = _ecKey.GetPubKey().ToHex();
            PublicAddress = _ecKey.GetPublicAddress();
        } catch (Exception ex) {
            Console.WriteLine("Error generating keys: " + ex.Message);
            throw;
        }
    }

    // Constructor with BigInteger private key
    public PwrWallet(BigInteger privateKey)
        : this(BitConverter.ToString(privateKey.ToByteArray())
                            .Replace("-", "")
                            .ToLower()) {}

    // Constructor with EthECKey object
    public PwrWallet(EthECKey key) {
        _apiSdk = new PwrApiSdk("https://pwrrpc.pwrlabs.io/");
        _ecKey = key;
        PrivateKeyHex = key.GetPrivateKeyAsBytes().ToHex();
        PublicKeyHex = key.GetPubKey().ToHex();
        PublicAddress = key.GetPublicAddress();
    }

    // Constructor with byte array private key
    public PwrWallet(byte[] privateKey)
        : this(BitConverter.ToString(privateKey).Replace("-", "").ToLower()) {}

    public string? PrivateKeyHex { get; }
    public string? PublicKeyHex { get; }
    public string? PublicAddress { get; }

    public static EthECKey ConvertToEthECKey(BigInteger privateKeyBiguint) {
        byte[] privateKeyBytes = privateKeyBiguint.ToByteArray();

        if (privateKeyBytes.Length < 32) {
        byte[] paddedBytes = new byte[32];
        Array.Copy(privateKeyBytes, 0, paddedBytes, 32 - privateKeyBytes.Length,
                    privateKeyBytes.Length);
        privateKeyBytes = paddedBytes;
        } else if (privateKeyBytes.Length > 32) {
        throw new ArgumentException(
            "Biguinteger is too large to represent an Ethereum private key");
        }

        string privateKeyHex =
            "0x" + BitConverter.ToString(privateKeyBytes).Replace("-", "");

        EthECKey ecKey = new EthECKey(privateKeyHex);

        return ecKey;
    }
    private WalletResponse CreateWalletResponse<T>(ApiResponse<T> response,
                                                    byte[]? finalTxn = null) {
        if (response.Success && finalTxn != null) {
            var txnHash = new Sha3Keccack().CalculateHash(finalTxn).ToHex();
            
            return new WalletResponse(true, "0x" + txnHash);
        } else {
            return new WalletResponse(false, null, response.Message);
        }
    }
    /// <summary>
    /// Retrieves the current nonce of the wallet.
    /// </summary>
    /// <returns>The nonce value.</returns>
    public async Task<uint> GetNonce() {
        var response = await _apiSdk.GetNonceOfAddress(PublicAddress);
        if (!response.Success) throw new Exception(response.Message);

        return response.Data;
    }
    /// <summary>
    /// Retrieves the current balance of the wallet.
    /// </summary>
    /// <returns>The balance value.</returns>
    public async Task<ulong> GetBalance() {
        var response = await _apiSdk.GetBalanceOfAddress(PublicAddress);
        if (!response.Success) throw new Exception(response.Message);

        return response.Data;
    }

    /// <summary>
    /// Retrieves the wallet address
    /// </summary>
    /// <returns>the wallet address.</returns>
    public string GetAddress() {
        return PublicAddress;
    }

    /// <summary>
    /// Retrieves the wallet address
    /// </summary>
    /// <returns>the wallet address.</returns>
    public string GetPrivateKey() {
        return $"0x{PrivateKeyHex}";
    }

    /// <summary>
    /// Stores the wallet's private key encrypted with the provided password at the specified path.
    /// </summary>
    /// <param name="path">File path where the encrypted wallet will be stored</param>
    /// <param name="password">Password to encrypt the private key</param>
    /// <returns>Task that represents the asynchronous operation</returns>
    public void StoreWallet(string path, string password)
    {
        try
        {
            // Get the private key bytes
            byte[] privateKeyBytes = _ecKey.GetPrivateKeyAsBytes();

            // Encrypt the private key using AES256
            byte[] encryptedPrivateKey = AES256.Encrypt(privateKeyBytes, password);

            // Write the encrypted data to the file
            File.WriteAllBytes(path, encryptedPrivateKey);
        }
        catch (Exception ex)
        {
            throw new Exception($"Error storing wallet: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Loads a wallet from an encrypted file using the provided password.
    /// </summary>
    /// <param name="path">Path to the encrypted wallet file</param>
    /// <param name="password">Password to decrypt the private key</param>
    /// <returns>A PwrWallet instance if successful, null otherwise</returns>
    public static PwrWallet LoadWallet(string path, string password)
    {
        try
        {
            // Read the encrypted data from the file
            byte[] encryptedData = File.ReadAllBytes(path);

            // Decrypt the data to get the private key
            byte[] privateKeyBytes = AES256.Decrypt(encryptedData, password);

            // Convert the bytes to a hexadecimal string with "0x" prefix
            string privateKeyHex = "0x" + BitConverter.ToString(privateKeyBytes).Replace("-", "");

            // Create and return a new wallet with the loaded private key
            return new PwrWallet(privateKeyHex);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading wallet: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Signs a transaction with the wallet's private key.
    /// </summary>
    /// <param name="txn">The transaction to sign.</param>
    /// <returns>The signed transaction.</returns>
    public byte[] GetSignedTxn(byte[] txn) {
        if (txn == null) throw new ArgumentException("txn cannot be null");

        byte[] signature = Signer.SignMessage(_ecKey, txn);
        int finalTxnLength = txn.Length + 65;

        byte[] finalTxn = new byte[finalTxnLength];

        Array.Copy(txn, 0, finalTxn, 0, txn.Length);
        Array.Copy(signature, 0, finalTxn, txn.Length, signature.Length);

        return finalTxn;
    }

    /// <summary>
    /// Transfers PWR tokens to the specified recipient.
    /// </summary>
    /// <param name="to">The recipient's address.</param>
    /// <param name="amount">The amount of tokens to transfer.</param>
    /// <param name="nonce">The nonce value of the transaction.</param>
    /// <returns>The response of the transfer operation.</returns>
    public async Task<WalletResponse> TransferPWR(string to, ulong amount,
                                                    uint nonce) {
        var signed = GetSignedTxn(await _txnBuilder.GetTransferPWRTxn(
            to, amount, nonce, await _apiSdk.GetChainId()));

        return CreateWalletResponse(await _apiSdk.BroadcastTxn(signed), signed);
    }
    public async Task<WalletResponse> TransferPWR(string to, ulong amount) {
        return await TransferPWR(to, amount, await GetNonce());
    }

    /// <summary>
    /// Joins a node using the specified IP address.
    /// </summary>
    /// <param name="ip">The IP address of the node to join.</param>
    /// <param name="nonce">The nonce value of the transaction.</param>
    /// <returns>The response of the join operation.</returns>
    public async Task<WalletResponse> JoinAsValidator(string ip, uint nonce) {
        byte[] signed = GetSignedTxn(
            await _txnBuilder.GetJoinAsValidatorTxn(ip, nonce, await _apiSdk.GetChainId()));
        return CreateWalletResponse(await _apiSdk.BroadcastTxn(signed), signed);
    }
    /// <summary>
    /// Joins a node using the specified IP address.
    /// </summary>
    /// <param name="ip">The IP address of the node to join.</param>
    /// <returns>The response of the join operation.</returns>
    public async Task<WalletResponse> JoinAsValidator(string ip) {
        return await JoinAsValidator(ip, await GetNonce());
    }

    /// <summary>
    /// Claims an active node spot.
    /// </summary>
    /// <param name="nonce">The nonce value of the transaction.</param>
    /// <returns>The response of the claim operation.</returns>
    public async Task<WalletResponse> ClaimActiveNodeSpot(uint nonce) {
        byte[] signed = GetSignedTxn(await _txnBuilder.GetClaimActiveNodeSpotTxn(
            nonce, await _apiSdk.GetChainId()));

        return CreateWalletResponse(await _apiSdk.BroadcastTxn(signed), signed);
    }
    /// <summary>
    /// Claims an active node spot.
    /// </summary>
    /// <returns>The response of the claim operation.</returns>
    public async Task<WalletResponse> ClaimActiveNodeSpot() {
        return await ClaimActiveNodeSpot(await GetNonce());
    }

    /// <summary>
    /// Delegates PWR tokens to the specified address.
    /// </summary>
    /// <param name="to">The recipient's address.</param>
    /// <param name="amount">The amount of tokens to delegate.</param>
    /// <param name="nonce">The nonce value of the transaction.</param>
    /// <returns>The response of the delegation operation.</returns>
    public async Task<WalletResponse> Delegate(string to, ulong amount,
                                                uint nonce) {
        var signed = GetSignedTxn(await _txnBuilder.GetDelegateTxn(
            to, amount, nonce, await _apiSdk.GetChainId()));

        return CreateWalletResponse(await _apiSdk.BroadcastTxn(signed), signed);
    }
    /// <summary>
    /// Delegates PWR tokens to the specified address with the current nonce
    /// value.
    /// </summary>
    /// <param name="to">The recipient's address.</param>
    /// <param name="amount">The amount of tokens to delegate.</param>
    /// <returns>The response of the delegation operation.</returns>
    public async Task<WalletResponse> Delegate(string to, ulong amount) {
        return await Delegate(to, amount, await GetNonce());
    }

    /// <summary>
    /// Withdraws PWR tokens from the specified address.
    /// </summary>
    /// <param name="to">The recipient's address.</param>
    /// <param name="amount">The amount of tokens to withdraw.</param>
    /// <param name="nonce">The nonce value of the transaction.</param>
    /// <returns>The response of the withdrawal operation.</returns>
    public async Task<WalletResponse> Withdraw(string to, ulong amount,
                                                uint nonce) {
        byte[] signed = GetSignedTxn(await _txnBuilder.GetWithdrawTxn(
            to, amount, nonce, await _apiSdk.GetChainId()));

        return CreateWalletResponse(await _apiSdk.BroadcastTxn(signed), signed);
    }
    /// <summary>
    /// Withdraws PWR tokens from the specified address with the current nonce
    /// value.
    /// </summary>
    /// <param name="to">The recipient's address.</param>
    /// <param name="amount">The amount of tokens to withdraw.</param>
    /// <returns>The response of the withdrawal operation.</returns>
    public async Task<WalletResponse> Withdraw(string to, ulong amount) {
        return await Withdraw(to, amount, await GetNonce());
    }

    /// <summary>
    /// Sends data to a virtual machine.
    /// </summary>
    /// <param name="vmId">The ID of the virtual machine.</param>
    /// <param name="data">The data to send.</param>
    /// <param name="nonce">The nonce value of the transaction.</param>
    /// <returns>The response of the transaction operation.</returns>
    public async Task<WalletResponse> SendVMData(ulong vmId, byte[] data,
                                                    uint nonce) {
        byte[] signed = GetSignedTxn(await _txnBuilder.GetVmDataTxn(
            vmId, data, nonce, await _apiSdk.GetChainId()));

        return CreateWalletResponse(await _apiSdk.BroadcastTxn(signed), signed);
    }
    /// <summary>
    /// Sends data to a virtual machine with the current nonce value.
    /// </summary>
    /// <param name="vmId">The ID of the virtual machine.</param>
    /// <param name="data">The data to send.</param>
    /// <returns>The response of the transaction operation.</returns>
    public async Task<WalletResponse> SendVMData(ulong vmId, byte[] data) {
        return await SendVMData(vmId, data, await GetNonce());
    }

    /// <summary>
    /// Claims a virtual machine ID.
    /// </summary>
    /// <param name="vmId">The ID of the virtual machine to claim.</param>
    /// <param name="nonce">The nonce value of the transaction.</param>
    /// <returns>The response of the claiming operation.</returns>
    public async Task<WalletResponse> ClaimVmId(ulong vmId, uint nonce) {
        byte[] signed = GetSignedTxn(await _txnBuilder.GetClaimVmIdTxn(
            vmId, nonce, await _apiSdk.GetChainId()));

        return CreateWalletResponse(await _apiSdk.BroadcastTxn(signed), signed);
    }
    /// <summary>
    /// Claims a virtual machine ID.
    /// </summary>
    /// <param name="vmId">The ID of the virtual machine to claim.</param>
    /// <returns>The response of the claiming operation.</returns>
    public async Task<WalletResponse> ClaimVmId(ulong vmId) {
        return await ClaimVmId(vmId, await GetNonce());
    }

    /// <summary>
    /// Sets a guardian for the specified address.
    /// </summary>
    /// <param name="guardianAddress">The guardian's address.</param>
    /// <param name="expiryDate">The expiry date of the guardian.</param>
    /// <param name="nonce">The nonce value of the transaction.</param>
    /// <returns>The response of the setting guardian operation.</returns>
    public async Task<WalletResponse> SetGuardian(string guardianAddress,
                                                    ulong expiryDate, uint nonce) {
        byte[] signed = GetSignedTxn(await _txnBuilder.GetSetGuardianTxn(
            guardianAddress, expiryDate, nonce, await _apiSdk.GetChainId()));

        return CreateWalletResponse(await _apiSdk.BroadcastTxn(signed), signed);
    }
    /// <summary>
    /// Sets a guardian for the specified address with the current nonce value.
    /// </summary>
    /// <param name="guardianAddress">The guardian's address.</param>
    /// <param name="expiryDate">The expiry date of the guardian.</param>
    /// <returns>The response of the setting guardian operation.</returns>
    public async Task<WalletResponse> SetGuardian(string guardianAddress,
                                                    ulong expiryDate) {
        return await SetGuardian(guardianAddress, expiryDate, await GetNonce());
    }

    /// <summary>
    /// Removes a guardian.
    /// </summary>
    /// <param name="nonce">The nonce value of the transaction.</param>
    /// <returns>The response of the removing guardian operation.</returns>
    public async Task<WalletResponse> RemoveGuardian(uint nonce) {
        byte[] signed = GetSignedTxn(await _txnBuilder.GetRemoveGuardianTxn(
            nonce, await _apiSdk.GetChainId()));

        return CreateWalletResponse(await _apiSdk.BroadcastTxn(signed), signed);
    }
    /// <summary>
    /// Removes a guardian.
    /// </summary>
    /// <returns>The response of the removing guardian operation.</returns>
    public async Task<WalletResponse> RemoveGuardian() {
        return await RemoveGuardian(await GetNonce());
    }

    /// <summary>
    /// Sends a guardian wrapped transaction.
    /// </summary>
    /// <param name="txn">The transaction data.</param>
    /// <param name="nonce">The nonce value of the transaction.</param>
    /// <returns>The response of the transaction operation.</returns>
    public async Task<WalletResponse> SendGuardianApprovalTransaction(List<byte[]> txns,
                                                                    uint nonce) {
        byte[] signed = GetSignedTxn(await _txnBuilder.GetGuardianApprovalTxn (
            txns, nonce, await _apiSdk.GetChainId()));

        return CreateWalletResponse(await _apiSdk.BroadcastTxn(signed), signed);
    }
    /// <summary>
    /// Sends a guardian wrapped transaction with the current nonce value.
    /// </summary>
    /// <param name="txn">The transaction data.</param>
    /// <returns>The response of the transaction operation.</returns>
    public async Task<WalletResponse> SendGuardianApprovalTransaction(List<byte[]> txns) {
        return await SendGuardianApprovalTransaction(txns, await GetNonce());
    }

    /// <summary>
    /// Sends a transaction to remove a validator.
    /// </summary>
    /// <param name="validator">The address of the validator to remove.</param>
    /// <param name="nonce">The nonce value of the transaction.</param>
    /// <returns>The response of the transaction operation.</returns>
    public async Task<WalletResponse> SendValidatorRemoveTxn(string validator,
                                                            uint nonce) {
        byte[] signed = GetSignedTxn(await _txnBuilder.GetValidatorRemoveTxn(
            validator, nonce, await _apiSdk.GetChainId()));
    
        return CreateWalletResponse(await _apiSdk.BroadcastTxn(signed), signed);
    }
    /// <summary>
    /// Sends a transaction to remove a validator.
    /// </summary>
    /// <param name="validator">The address of the validator to remove.</param>
    /// <returns>The response of the transaction operation.</returns>
    public async Task<WalletResponse> SendValidatorRemoveTxn(string validator) {
        return await SendValidatorRemoveTxn(validator, await GetNonce());
    }

    public async Task<WalletResponse> ConduitApprove(ulong vmId,
                                                    List<byte[]> transactions,
                                                    uint nonce) {
        byte[] signed = GetSignedTxn(await _txnBuilder.GetConduitApprovalTxn(
            vmId, transactions, nonce, await _apiSdk.GetChainId()));

        return CreateWalletResponse(await _apiSdk.BroadcastTxn(signed), signed);
    }
    public async Task<WalletResponse> ConduitApprove(ulong vmId,
                                                    List<byte[]> transactions) {
        return await ConduitApprove(vmId, transactions, await GetNonce());
    }

    public async Task<WalletResponse> SetConduits(ulong vmId, List<byte[]> conduits,
                                                uint nonce) {
        byte[] signed = GetSignedTxn(await _txnBuilder.GetSetConduitsTxn(
            vmId, conduits, nonce, await _apiSdk.GetChainId()));

        return CreateWalletResponse(await _apiSdk.BroadcastTxn(signed), signed);
    }
    public async Task<WalletResponse> SetConduits(ulong vmId,
                                                List<byte[]> conduits) {
        return await SetConduits(vmId, conduits, await GetNonce());
    }

    public async Task<WalletResponse> AddConduits(ulong vmId, List<byte[]> conduits,
                                                uint nonce) {
        byte[] signed = GetSignedTxn(await _txnBuilder.GetAddConduitsTxn(
            vmId, conduits, nonce, await _apiSdk.GetChainId()));

        return CreateWalletResponse(await _apiSdk.BroadcastTxn(signed), signed);
    }
    public async Task<WalletResponse> AddConduits(ulong vmId,
                                                List<byte[]> conduits) {
        return await AddConduits(vmId, conduits, await GetNonce());
    }

    public async Task<WalletResponse> RemoveConduits(ulong vmId,
                                                    List<byte[]> conduits,
                                                    uint nonce) {
        byte[] signed = GetSignedTxn(await _txnBuilder.GetRemoveConduitsTxn(
            vmId, conduits, nonce, await _apiSdk.GetChainId()));

        return CreateWalletResponse(await _apiSdk.BroadcastTxn(signed), signed);
    }
    public async Task<WalletResponse> RemoveConduits(ulong vmId,
                                                    List<byte[]> conduits) {
        return await RemoveConduits(vmId, conduits, await GetNonce());
    }

    public async Task<WalletResponse> SendPayableVmDataTxn(ulong vmId,
                                                            ulong value,
                                                            byte[] data,
                                                            uint nonce) {
        byte[] signed = GetSignedTxn(await _txnBuilder.GetPayableVmDataTxn(
            vmId, value, data, nonce, await _apiSdk.GetChainId()));

        return CreateWalletResponse(await _apiSdk.BroadcastTxn(signed), signed);
    }
    public async Task<WalletResponse> SendPayableVmDataTxn(ulong vmId,
                                                            ulong value,
                                                            byte[] data) {
        return await SendPayableVmDataTxn(vmId, value, data, await GetNonce());
    }

    public async Task<WalletResponse> MoveStake(ulong sharesAmount,
                                            String fromValidator,
                                            String toValidator, uint nonce) {
        byte[] signed = GetSignedTxn(await _txnBuilder.GetMoveStakeTxn(
            sharesAmount, fromValidator, toValidator, nonce,
            await _apiSdk.GetChainId()));

        return CreateWalletResponse(await _apiSdk.BroadcastTxn(signed), signed);
    }
    public async Task<WalletResponse> MoveStake(ulong sharesAmount,
                                            String fromValidator,
                                            String toValidator) {
        return await MoveStake(sharesAmount, fromValidator, toValidator, await GetNonce());
    }

    public static string GetPublicKeyFromPrivateKey(byte[] privateKey) {
        if (privateKey.Length != 32) {
        throw new ArgumentException(
            "Private key must be a 64-character hexadecimal string.");
        }
        EthECKey ethECKey = new EthECKey(privateKey, true);

        string publicKeyHex = ethECKey.GetPubKey().ToHex();

        return publicKeyHex;
    }
    public static string GetPublicKeyFromPrivateKey(string privateKey) {
        byte[] privateKeyBytes = Extensions.HexStringToByteArray(privateKey);
        return GetPublicKeyFromPrivateKey(privateKeyBytes);
    }
    public static string GetPublicKeyFromPrivateKey(BigInteger privateKey) {
        byte[] privateKeyBytes = Extensions.HexStringToByteArray(
            BitConverter.ToString(privateKey.ToByteArray())
                .Replace("-", "")
                .ToLower());
        return GetPublicKeyFromPrivateKey(privateKeyBytes);
    }

    public static string PublicKeyToAddress(byte[] publicKey) {
        var eth = new EthECKey(publicKey, false);
        return eth.GetPublicAddress();
    }
    public static string PublicKeyToAddress(string publicKey) {
        return PublicKeyToAddress(Extensions.HexStringToByteArray(publicKey));
    }
    public static string PublicKeyToAddress(BigInteger publicKey) {
        byte[] publicKeyBytes = Extensions.HexStringToByteArray(
            BitConverter.ToString(publicKey.ToByteArray())
                .Replace("-", "")
                .ToLower());
        return PublicKeyToAddress(publicKeyBytes);
    }
}
