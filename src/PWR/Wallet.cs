using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using PWR.Models;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Pqc.Crypto.Falcon;
using Org.BouncyCastle.Security;
using System.Security.Cryptography;
using PWR.Utils;

namespace PWR;

public class Wallet {
    private readonly RPC _apiSdk;
    private readonly TransactionBuilder _txnBuilder;
    private readonly AsymmetricCipherKeyPair _keyPair;
    private readonly string _seedPhrase;
    private readonly byte[] _address;

    // Constructor with a seed phrase
    public Wallet(int wordCount, RPC? apiSdk = null) {
        if (wordCount != 12 && wordCount != 15 && wordCount != 18 && wordCount != 21 && wordCount != 24) {
            throw new ArgumentException("Word count must be one of 12, 15, 18, 21, or 24");
        }

        _apiSdk = apiSdk ?? new RPC("https://pwrrpc.pwrlabs.io/");

        // Calculate entropy bytes based on word count
        int entropyBytes;
        switch (wordCount) {
            case 12: entropyBytes = 16; break; // 128 bits
            case 15: entropyBytes = 20; break; // 160 bits
            case 18: entropyBytes = 24; break; // 192 bits
            case 21: entropyBytes = 28; break; // 224 bits
            case 24: entropyBytes = 32; break; // 256 bits
            default: throw new ArgumentException("Invalid word count");
        }

        // Generate random entropy
        byte[] entropy = new byte[entropyBytes];
        var rng = new SecureRandom();
        rng.NextBytes(entropy);

        // Generate mnemonic phrase (simplified for now)
        _seedPhrase = Convert.ToBase64String(entropy);
        byte[] seedPhraseBytes = Encoding.UTF8.GetBytes(_seedPhrase);
        var pbkdf2 = new Rfc2898DeriveBytes(seedPhraseBytes, Encoding.UTF8.GetBytes("mnemonic"), 2048, HashAlgorithmName.SHA512);
        byte[] seed = pbkdf2.GetBytes(64);

        // Generate Falcon key pair
        _keyPair = Falcon.GenerateKeyPair512FromSeed(seed);
        var publicKey = (FalconPublicKeyParameters)_keyPair.Public;
        
        // Generate address from public key
        byte[] publicKeyBytes = publicKey.GetEncoded();
        var hasher = new Org.BouncyCastle.Crypto.Digests.KeccakDigest(224);
        hasher.BlockUpdate(publicKeyBytes, 0, publicKeyBytes.Length);
        byte[] hash = new byte[hasher.GetDigestSize()];
        hasher.DoFinal(hash, 0);
        _address = new byte[20];
        Array.Copy(hash, 0, _address, 0, 20);

        _txnBuilder = new TransactionBuilder();
    }

    public Wallet(string seedPhrase, RPC? apiSdk = null) {
        _apiSdk = apiSdk ?? new RPC("https://pwrrpc.pwrlabs.io/");
        _seedPhrase = seedPhrase;

        byte[] seedPhraseBytes = Encoding.UTF8.GetBytes(seedPhrase);
        var pbkdf2 = new Rfc2898DeriveBytes(seedPhraseBytes, Encoding.UTF8.GetBytes("mnemonic"), 2048, HashAlgorithmName.SHA512);
        byte[] seed = pbkdf2.GetBytes(64);

        _keyPair = Falcon.GenerateKeyPair512FromSeed(seed);

        var publicKey = (FalconPublicKeyParameters)_keyPair.Public;

        byte[] publicKeyBytes = publicKey.GetEncoded();
        var hasher = new Org.BouncyCastle.Crypto.Digests.KeccakDigest(224);
        hasher.BlockUpdate(publicKeyBytes, 0, publicKeyBytes.Length);
        byte[] hash = new byte[hasher.GetDigestSize()];
        hasher.DoFinal(hash, 0);
        _address = new byte[20];
        Array.Copy(hash, 0, _address, 0, 20);

        _txnBuilder = new TransactionBuilder();
    }

    private WalletResponse CreateWalletResponse<T>(ApiResponse<T> response,
                                                    byte[]? finalTxn = null) {
        if (response.Success && finalTxn != null) {
            var txnHash = Hash.Sha3(finalTxn).ToHex();
            
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
        return await _apiSdk.GetNonceOfAddress(GetAddress());
    }

    /// <summary>
    /// Retrieves the current balance of the wallet.
    /// </summary>
    /// <returns>The balance value.</returns>
    public async Task<ulong> GetBalance() {
        return await _apiSdk.GetBalanceOfAddress(GetAddress());
    }

    /// <summary>
    /// Retrieves the wallet address
    /// </summary>
    /// <returns>the wallet address.</returns>
    public string GetAddress() {
        return "0x" + _address.ToHex();
    }

    /// <summary>
    /// Retrieves the seed phrase of the wallet.
    /// </summary>
    /// <returns>The seed phrase.</returns>
    public string GetSeedPhrase() {
        return _seedPhrase;
    }

    /// <summary>
    /// Retrieves the public key of the wallet.
    /// </summary>
    /// <returns>The public key.</returns>
    public byte[] GetPublicKey() {
        return ((FalconPublicKeyParameters)_keyPair.Public).GetEncoded();
    }

    /// <summary>
    /// Retrieves the private key of the wallet.
    /// </summary>
    /// <returns>The private key.</returns>
    public byte[] GetPrivateKey() {
        return ((FalconPrivateKeyParameters)_keyPair.Private).GetEncoded();
    }

    /// <summary>
    /// Signs a message with the wallet's private key.
    /// </summary>
    /// <param name="message">The message to sign.</param>
    /// <returns>The signed message.</returns>
    public byte[] Sign(byte[] message) {
        return Falcon.Sign(message, _keyPair);
    }

    /// <summary>
    /// Verifies a signature for a message.
    /// </summary>
    /// <param name="message">The message to verify.</param>
    /// <param name="signature">The signature to verify.</param>
    /// <returns>True if the signature is valid, false otherwise.</returns>
    public bool Verify(byte[] message, byte[] signature) {
        return Falcon.Verify512(message, signature, GetPublicKey());
    }

    /// <summary>
    /// Stores the wallet's seed phrase encrypted with the provided password at the specified path.
    /// </summary>
    /// <param name="path">File path where the encrypted wallet will be stored</param>
    /// <param name="password">Password to encrypt the seed phrase</param>
    /// <returns>Task that represents the asynchronous operation</returns>
    public void StoreWallet(string path, string password)
    {
        try
        {
            byte[] seedPhraseBytes = Encoding.UTF8.GetBytes(_seedPhrase);
            byte[] encryptedSeedPhrase = AES256.Encrypt(seedPhraseBytes, password);
            File.WriteAllBytes(path, encryptedSeedPhrase);
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
    /// <param name="password">Password to decrypt the seed phrase</param>
    /// <returns>A Wallet instance if successful, null otherwise</returns>
    public static Wallet LoadWallet(string path, string password)
    {
        try
        {
            byte[] encryptedData = File.ReadAllBytes(path);
            byte[] seedPhraseBytes = AES256.Decrypt(encryptedData, password);
            string seedPhrase = Encoding.UTF8.GetString(seedPhraseBytes);

            return new Wallet(seedPhrase);
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

        // Hash the transaction using Keccak256
        var hasher = new Org.BouncyCastle.Crypto.Digests.KeccakDigest(256);
        hasher.BlockUpdate(txn, 0, txn.Length);
        byte[] txnHash = new byte[hasher.GetDigestSize()];
        hasher.DoFinal(txnHash, 0);

        // Sign the hash
        byte[] signature = Sign(txnHash);

        // Append signature and its length to the transaction
        byte[] signatureLenBytes = BitConverter.GetBytes((short)signature.Length);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(signatureLenBytes);

        byte[] finalTxn = new byte[txn.Length + signature.Length + signatureLenBytes.Length];
        Array.Copy(txn, 0, finalTxn, 0, txn.Length);
        Array.Copy(signature, 0, finalTxn, txn.Length, signature.Length);
        Array.Copy(signatureLenBytes, 0, finalTxn, txn.Length + signature.Length, signatureLenBytes.Length);

        return finalTxn;
    }

    /// <summary>
    /// Sets the public key of the wallet.
    /// </summary>
    /// <param name="feePerByte">The fee per byte of the transaction.</param>
    /// <param name="nonce">The nonce value of the transaction.</param>
    /// <returns>The response of the transfer operation.</returns>
    public async Task<WalletResponse> SetPublicKey(ulong feePerByte, uint nonce) {
        var signed = GetSignedTxn(await _txnBuilder.GetSetPublicKeyTxn(
            GetPublicKey().ToHex(), nonce, await _apiSdk.GetChainId(), feePerByte, GetAddress()));

        return CreateWalletResponse(await _apiSdk.BroadcastTxn(signed), signed);
    }
    public async Task<WalletResponse> SetPublicKey(ulong feePerByte) {
        return await SetPublicKey(feePerByte, await GetNonce());
    }
    public async Task<WalletResponse> SetPublicKey() {
        return await SetPublicKey(await _apiSdk.GetFeePerByte(), await GetNonce());
    }

    /// <summary>
    /// Joins the wallet as a validator.
    /// </summary>
    /// <param name="ip">The IP address of the validator.</param>
    /// <param name="feePerByte">The fee per byte of the transaction.</param>
    /// <param name="nonce">The nonce value of the transaction.</param>
    /// <returns>The response of the transfer operation.</returns>
    public async Task<WalletResponse> JoinAsValidator(string ip, ulong feePerByte, uint nonce) {
        var response = await MakeSurePublicKeyIsSet();
        if (response != null && !response.Success) return response;

        var signed = GetSignedTxn(await _txnBuilder.GetJoinAsValidatorTxn(
            ip, nonce, await _apiSdk.GetChainId(), feePerByte, GetAddress()));

        return CreateWalletResponse(await _apiSdk.BroadcastTxn(signed), signed);
    }
    public async Task<WalletResponse> JoinAsValidator(string ip, ulong feePerByte) {
        return await JoinAsValidator(ip, feePerByte, await GetNonce());
    }
    public async Task<WalletResponse> JoinAsValidator(string ip) {
        return await JoinAsValidator(ip, await _apiSdk.GetFeePerByte(), await GetNonce());
    }

    /// <summary>
    /// Delegate PWR tokens to the specified validator.
    /// </summary>
    /// <param name="to">The validator's address.</param>
    /// <param name="amount">The amount of tokens to delegate.</param>
    /// <param name="feePerByte">The fee per byte of the transaction.</param>
    /// <param name="nonce">The nonce value of the transaction.</param>
    /// <returns>The response of the delegate operation.</returns>
    public async Task<WalletResponse> Delegate(string to, ulong amount, ulong feePerByte, uint nonce) {
        var response = await MakeSurePublicKeyIsSet();
        if (response != null && !response.Success) return response;

        var signed = GetSignedTxn(await _txnBuilder.GetDelegateTxn(
            to, amount, nonce, await _apiSdk.GetChainId(), feePerByte, GetAddress()));

        return CreateWalletResponse(await _apiSdk.BroadcastTxn(signed), signed);
    }
    public async Task<WalletResponse> Delegate(string to, ulong amount, ulong feePerByte) {
        return await Delegate(to, amount, feePerByte, await GetNonce());
    }
    public async Task<WalletResponse> Delegate(string to, ulong amount) {
        return await Delegate(to, amount, await _apiSdk.GetFeePerByte(), await GetNonce());
    }

    /// <summary>
    /// Changes the IP address of the validator.
    /// </summary>
    /// <param name="ip">The IP address of the validator.</param>
    /// <param name="feePerByte">The fee per byte of the transaction.</param>
    /// <param name="nonce">The nonce value of the transaction.</param>
    /// <returns>The response of the transfer operation.</returns>
    public async Task<WalletResponse> ChangeIp(string ip, ulong feePerByte, uint nonce) {
        var response = await MakeSurePublicKeyIsSet();
        if (response != null && !response.Success) return response;

        var signed = GetSignedTxn(await _txnBuilder.GetChangeIpTxn(
            ip, nonce, await _apiSdk.GetChainId(), feePerByte, GetAddress()));

        return CreateWalletResponse(await _apiSdk.BroadcastTxn(signed), signed);
    }
    public async Task<WalletResponse> ChangeIp(string ip, ulong feePerByte) {
        return await ChangeIp(ip, feePerByte, await GetNonce());
    }
    public async Task<WalletResponse> ChangeIp(string ip) {
        return await ChangeIp(ip, await _apiSdk.GetFeePerByte(), await GetNonce());
    }

    /// <summary>
    /// Claims the active node spot of the wallet.
    /// </summary>
    /// <param name="feePerByte">The fee per byte of the transaction.</param>
    /// <param name="nonce">The nonce value of the transaction.</param>
    /// <returns>The response of the claim operation.</returns>
    public async Task<WalletResponse> ClaimActiveNodeSpot(ulong feePerByte, uint nonce) {
        var response = await MakeSurePublicKeyIsSet();
        if (response != null && !response.Success) return response;

        var signed = GetSignedTxn(await _txnBuilder.GetClaimActiveNodeSpotTxn(
            nonce, await _apiSdk.GetChainId(), feePerByte, GetAddress()));

        return CreateWalletResponse(await _apiSdk.BroadcastTxn(signed), signed);
    }
    public async Task<WalletResponse> ClaimActiveNodeSpot(ulong feePerByte) {
        return await ClaimActiveNodeSpot(feePerByte, await GetNonce());
    }
    public async Task<WalletResponse> ClaimActiveNodeSpot() {
        return await ClaimActiveNodeSpot(await _apiSdk.GetFeePerByte(), await GetNonce());
    }

    /// <summary>
    /// Transfers PWR tokens to the specified recipient.
    /// </summary>
    /// <param name="to">The recipient's address.</param>
    /// <param name="amount">The amount of tokens to transfer.</param>
    /// <param name="feePerByte">The fee per byte of the transaction.</param>
    /// <param name="nonce">The nonce value of the transaction.</param>
    /// <returns>The response of the transfer operation.</returns>
    public async Task<WalletResponse> TransferPWR(string to, ulong amount, ulong feePerByte, uint nonce) {
        var response = await MakeSurePublicKeyIsSet();
        if (response != null && !response.Success) return response;

        var signed = GetSignedTxn(await _txnBuilder.GetTransferPWRTxn(
            to, amount, nonce, await _apiSdk.GetChainId(), feePerByte, GetAddress()));

        return CreateWalletResponse(await _apiSdk.BroadcastTxn(signed), signed);
    }
    public async Task<WalletResponse> TransferPWR(string to, ulong amount, ulong feePerByte) {
        return await TransferPWR(to, amount, feePerByte, await GetNonce());
    }
    public async Task<WalletResponse> TransferPWR(string to, ulong amount) {
        return await TransferPWR(to, amount, await _apiSdk.GetFeePerByte(), await GetNonce());
    }

    // --- Proposal Transactions ---
    public async Task<WalletResponse> ProposeChangeEarlyWithdrawPenalty(string title, string description, ulong withdrawPenaltyTime, ulong withdrawPenalty, ulong feePerByte, uint nonce) {
        var response = await MakeSurePublicKeyIsSet();
        if (response != null && !response.Success) return response;

        var signed = GetSignedTxn(await _txnBuilder.GetChangeEarlyWithdrawPenaltyProposalTxn(
            title, description, withdrawPenaltyTime, withdrawPenalty, nonce, await _apiSdk.GetChainId(), feePerByte, GetAddress()));
        return CreateWalletResponse(await _apiSdk.BroadcastTxn(signed), signed);
    }
    public async Task<WalletResponse> ProposeChangeEarlyWithdrawPenalty(string title, string description, ulong withdrawPenaltyTime, ulong withdrawPenalty, ulong feePerByte) {
        return await ProposeChangeEarlyWithdrawPenalty(title, description, withdrawPenaltyTime, withdrawPenalty, feePerByte, await GetNonce());
    }
    public async Task<WalletResponse> ProposeChangeEarlyWithdrawPenalty(string title, string description, ulong withdrawPenaltyTime, ulong withdrawPenalty) {
        return await ProposeChangeEarlyWithdrawPenalty(title, description, withdrawPenaltyTime, withdrawPenalty, await _apiSdk.GetFeePerByte(), await GetNonce());
    }

    public async Task<WalletResponse> ProposeChangeFeePerByte(string title, string description, ulong newFeePerByte, ulong feePerByte, uint nonce) {
        var response = await MakeSurePublicKeyIsSet();
        if (response != null && !response.Success) return response;

        var signed = GetSignedTxn(await _txnBuilder.GetChangeFeePerByteProposalTxn(
            title, description, newFeePerByte, nonce, await _apiSdk.GetChainId(), feePerByte, GetAddress()));
        return CreateWalletResponse(await _apiSdk.BroadcastTxn(signed), signed);
    }
    public async Task<WalletResponse> ProposeChangeFeePerByte(string title, string description, ulong newFeePerByte, ulong feePerByte) {
        return await ProposeChangeFeePerByte(title, description, newFeePerByte, feePerByte, await GetNonce());
    }
    public async Task<WalletResponse> ProposeChangeFeePerByte(string title, string description, ulong newFeePerByte) {
        return await ProposeChangeFeePerByte(title, description, newFeePerByte, await _apiSdk.GetFeePerByte(), await GetNonce());
    }

    public async Task<WalletResponse> ProposeChangeMaxBlockSize(string title, string description, ulong maxBlockSize, ulong feePerByte, uint nonce) {
        var response = await MakeSurePublicKeyIsSet();
        if (response != null && !response.Success) return response;

        var signed = GetSignedTxn(await _txnBuilder.GetChangeMaxBlockSizeProposalTxn(
            title, description, maxBlockSize, nonce, await _apiSdk.GetChainId(), feePerByte, GetAddress()));
        return CreateWalletResponse(await _apiSdk.BroadcastTxn(signed), signed);
    }
    public async Task<WalletResponse> ProposeChangeMaxBlockSize(string title, string description, ulong maxBlockSize, ulong feePerByte) {
        return await ProposeChangeMaxBlockSize(title, description, maxBlockSize, feePerByte, await GetNonce());
    }
    public async Task<WalletResponse> ProposeChangeMaxBlockSize(string title, string description, ulong maxBlockSize) {
        return await ProposeChangeMaxBlockSize(title, description, maxBlockSize, await _apiSdk.GetFeePerByte(), await GetNonce());
    }

    public async Task<WalletResponse> ProposeChangeMaxTxnSize(string title, string description, ulong maxTxnSize, ulong feePerByte, uint nonce) {
        var response = await MakeSurePublicKeyIsSet();
        if (response != null && !response.Success) return response;

        var signed = GetSignedTxn(await _txnBuilder.GetChangeMaxTxnSizeProposalTxn(
            title, description, maxTxnSize, nonce, await _apiSdk.GetChainId(), feePerByte, GetAddress()));
        return CreateWalletResponse(await _apiSdk.BroadcastTxn(signed), signed);
    }
    public async Task<WalletResponse> ProposeChangeMaxTxnSize(string title, string description, ulong maxTxnSize, ulong feePerByte) {
        return await ProposeChangeMaxTxnSize(title, description, maxTxnSize, feePerByte, await GetNonce());
    }
    public async Task<WalletResponse> ProposeChangeMaxTxnSize(string title, string description, ulong maxTxnSize) {
        return await ProposeChangeMaxTxnSize(title, description, maxTxnSize, await _apiSdk.GetFeePerByte(), await GetNonce());
    }

    public async Task<WalletResponse> ProposeChangeOverallBurnPercentage(string title, string description, ulong burnPercentage, ulong feePerByte, uint nonce) {
        var response = await MakeSurePublicKeyIsSet();
        if (response != null && !response.Success) return response;

        var signed = GetSignedTxn(await _txnBuilder.GetChangeOverallBurnPercentageProposalTxn(
            title, description, burnPercentage, nonce, await _apiSdk.GetChainId(), feePerByte, GetAddress()));
        return CreateWalletResponse(await _apiSdk.BroadcastTxn(signed), signed);
    }
    public async Task<WalletResponse> ProposeChangeOverallBurnPercentage(string title, string description, ulong burnPercentage, ulong feePerByte) {
        return await ProposeChangeOverallBurnPercentage(title, description, burnPercentage, feePerByte, await GetNonce());
    }
    public async Task<WalletResponse> ProposeChangeOverallBurnPercentage(string title, string description, ulong burnPercentage) {
        return await ProposeChangeOverallBurnPercentage(title, description, burnPercentage, await _apiSdk.GetFeePerByte(), await GetNonce());
    }

    public async Task<WalletResponse> ProposeChangeRewardPerYear(string title, string description, ulong rewardPerYear, ulong feePerByte, uint nonce) {
        var response = await MakeSurePublicKeyIsSet();
        if (response != null && !response.Success) return response;

        var signed = GetSignedTxn(await _txnBuilder.GetChangeRewardPerYearProposalTxn(
            title, description, rewardPerYear, nonce, await _apiSdk.GetChainId(), feePerByte, GetAddress()));
        return CreateWalletResponse(await _apiSdk.BroadcastTxn(signed), signed);
    }
    public async Task<WalletResponse> ProposeChangeRewardPerYear(string title, string description, ulong rewardPerYear, ulong feePerByte) {
        return await ProposeChangeRewardPerYear(title, description, rewardPerYear, feePerByte, await GetNonce());
    }
    public async Task<WalletResponse> ProposeChangeRewardPerYear(string title, string description, ulong rewardPerYear) {
        return await ProposeChangeRewardPerYear(title, description, rewardPerYear, await _apiSdk.GetFeePerByte(), await GetNonce());
    }

    public async Task<WalletResponse> ProposeChangeValidatorCountLimit(string title, string description, ulong validatorCountLimit, ulong feePerByte, uint nonce) {
        var response = await MakeSurePublicKeyIsSet();
        if (response != null && !response.Success) return response;

        var signed = GetSignedTxn(await _txnBuilder.GetChangeValidatorCountLimitProposalTxn(
            title, description, validatorCountLimit, nonce, await _apiSdk.GetChainId(), feePerByte, GetAddress()));
        return CreateWalletResponse(await _apiSdk.BroadcastTxn(signed), signed);
    }
    public async Task<WalletResponse> ProposeChangeValidatorCountLimit(string title, string description, ulong validatorCountLimit, ulong feePerByte) {
        return await ProposeChangeValidatorCountLimit(title, description, validatorCountLimit, feePerByte, await GetNonce());
    }
    public async Task<WalletResponse> ProposeChangeValidatorCountLimit(string title, string description, ulong validatorCountLimit) {
        return await ProposeChangeValidatorCountLimit(title, description, validatorCountLimit, await _apiSdk.GetFeePerByte(), await GetNonce());
    }

    public async Task<WalletResponse> ProposeChangeValidatorJoiningFee(string title, string description, ulong joiningFee, ulong feePerByte, uint nonce) {
        var response = await MakeSurePublicKeyIsSet();
        if (response != null && !response.Success) return response;

        var signed = GetSignedTxn(await _txnBuilder.GetChangeValidatorJoiningFeeProposalTxn(
            title, description, joiningFee, nonce, await _apiSdk.GetChainId(), feePerByte, GetAddress()));
        return CreateWalletResponse(await _apiSdk.BroadcastTxn(signed), signed);
    }
    public async Task<WalletResponse> ProposeChangeValidatorJoiningFee(string title, string description, ulong joiningFee, ulong feePerByte) {
        return await ProposeChangeValidatorJoiningFee(title, description, joiningFee, feePerByte, await GetNonce());
    }
    public async Task<WalletResponse> ProposeChangeValidatorJoiningFee(string title, string description, ulong joiningFee) {
        return await ProposeChangeValidatorJoiningFee(title, description, joiningFee, await _apiSdk.GetFeePerByte(), await GetNonce());
    }

    public async Task<WalletResponse> ProposeChangeVidaIdClaimingFee(string title, string description, ulong claimingFee, ulong feePerByte, uint nonce) {
        var response = await MakeSurePublicKeyIsSet();
        if (response != null && !response.Success) return response;

        var signed = GetSignedTxn(await _txnBuilder.GetChangeVidaIdClaimingFeeProposalTxn(
            title, description, claimingFee, nonce, await _apiSdk.GetChainId(), feePerByte, GetAddress()));
        return CreateWalletResponse(await _apiSdk.BroadcastTxn(signed), signed);
    }
    public async Task<WalletResponse> ProposeChangeVidaIdClaimingFee(string title, string description, ulong claimingFee, ulong feePerByte) {
        return await ProposeChangeVidaIdClaimingFee(title, description, claimingFee, feePerByte, await GetNonce());
    }
    public async Task<WalletResponse> ProposeChangeVidaIdClaimingFee(string title, string description, ulong claimingFee) {
        return await ProposeChangeVidaIdClaimingFee(title, description, claimingFee, await _apiSdk.GetFeePerByte(), await GetNonce());
    }

    public async Task<WalletResponse> ProposeChangeVidaOwnerTxnFeeShare(string title, string description, ulong feeShare, ulong feePerByte, uint nonce) {
        var response = await MakeSurePublicKeyIsSet();
        if (response != null && !response.Success) return response;

        var signed = GetSignedTxn(await _txnBuilder.GetChangeVidaOwnerTxnFeeShareProposalTxn(
            title, description, feeShare, nonce, await _apiSdk.GetChainId(), feePerByte, GetAddress()));
        return CreateWalletResponse(await _apiSdk.BroadcastTxn(signed), signed);
    }
    public async Task<WalletResponse> ProposeChangeVidaOwnerTxnFeeShare(string title, string description, ulong feeShare, ulong feePerByte) {
        return await ProposeChangeVidaOwnerTxnFeeShare(title, description, feeShare, feePerByte, await GetNonce());
    }
    public async Task<WalletResponse> ProposeChangeVidaOwnerTxnFeeShare(string title, string description, ulong feeShare) {
        return await ProposeChangeVidaOwnerTxnFeeShare(title, description, feeShare, await _apiSdk.GetFeePerByte(), await GetNonce());
    }

    public async Task<WalletResponse> ProposeOther(string title, string description, ulong feePerByte, uint nonce) {
        var response = await MakeSurePublicKeyIsSet();
        if (response != null && !response.Success) return response;

        var signed = GetSignedTxn(await _txnBuilder.GetOtherProposalTxn(
            title, description, nonce, await _apiSdk.GetChainId(), feePerByte, GetAddress()));
        return CreateWalletResponse(await _apiSdk.BroadcastTxn(signed), signed);
    }
    public async Task<WalletResponse> ProposeOther(string title, string description, ulong feePerByte) {
        return await ProposeOther(title, description, feePerByte, await GetNonce());
    }
    public async Task<WalletResponse> ProposeOther(string title, string description) {
        return await ProposeOther(title, description, await _apiSdk.GetFeePerByte(), await GetNonce());
    }

    public async Task<WalletResponse> VoteOnProposal(string proposalHash, byte vote, ulong feePerByte, uint nonce) {
        var response = await MakeSurePublicKeyIsSet();
        if (response != null && !response.Success) return response;

        var signed = GetSignedTxn(await _txnBuilder.GetVoteOnProposalTxn(
            proposalHash, vote, nonce, await _apiSdk.GetChainId(), feePerByte, GetAddress()));
        return CreateWalletResponse(await _apiSdk.BroadcastTxn(signed), signed);
    }
    public async Task<WalletResponse> VoteOnProposal(string proposalHash, byte vote, ulong feePerByte) {
        return await VoteOnProposal(proposalHash, vote, feePerByte, await GetNonce());
    }
    public async Task<WalletResponse> VoteOnProposal(string proposalHash, byte vote) {
        return await VoteOnProposal(proposalHash, vote, await _apiSdk.GetFeePerByte(), await GetNonce());
    }

    // --- Guardian Transactions ---
    public async Task<WalletResponse> GuardianApproval(List<byte[]> transactions, ulong feePerByte, uint nonce) {
        var response = await MakeSurePublicKeyIsSet();
        if (response != null && !response.Success) return response;

        var signed = GetSignedTxn(await _txnBuilder.GetGuardianApprovalTxn(
            transactions, nonce, await _apiSdk.GetChainId(), feePerByte, GetAddress()));
        return CreateWalletResponse(await _apiSdk.BroadcastTxn(signed), signed);
    }
    public async Task<WalletResponse> GuardianApproval(List<byte[]> transactions, ulong feePerByte) {
        return await GuardianApproval(transactions, feePerByte, await GetNonce());
    }
    public async Task<WalletResponse> GuardianApproval(List<byte[]> transactions) {
        return await GuardianApproval(transactions, await _apiSdk.GetFeePerByte(), await GetNonce());
    }

    public async Task<WalletResponse> RemoveGuardian(ulong feePerByte, uint nonce) {
        var response = await MakeSurePublicKeyIsSet();
        if (response != null && !response.Success) return response;

        var signed = GetSignedTxn(await _txnBuilder.GetRemoveGuardianTxn(
            nonce, await _apiSdk.GetChainId(), feePerByte, GetAddress()));
        return CreateWalletResponse(await _apiSdk.BroadcastTxn(signed), signed);
    }
    public async Task<WalletResponse> RemoveGuardian(ulong feePerByte) {
        return await RemoveGuardian(feePerByte, await GetNonce());
    }
    public async Task<WalletResponse> RemoveGuardian() {
        return await RemoveGuardian(await _apiSdk.GetFeePerByte(), await GetNonce());
    }

    public async Task<WalletResponse> SetGuardian(ulong expiryDate, string guardianAddress, ulong feePerByte, uint nonce) {
        var response = await MakeSurePublicKeyIsSet();
        if (response != null && !response.Success) return response;

        var signed = GetSignedTxn(await _txnBuilder.GetSetGuardianTxn(
            expiryDate, guardianAddress, nonce, await _apiSdk.GetChainId(), feePerByte, GetAddress()));
        return CreateWalletResponse(await _apiSdk.BroadcastTxn(signed), signed);
    }
    public async Task<WalletResponse> SetGuardian(ulong expiryDate, string guardianAddress, ulong feePerByte) {
        return await SetGuardian(expiryDate, guardianAddress, feePerByte, await GetNonce());
    }
    public async Task<WalletResponse> SetGuardian(ulong expiryDate, string guardianAddress) {
        return await SetGuardian(expiryDate, guardianAddress, await _apiSdk.GetFeePerByte(), await GetNonce());
    }

    // --- Staking Transactions ---
    public async Task<WalletResponse> MoveStake(ulong sharesAmount, string fromValidator, string toValidator, ulong feePerByte, uint nonce) {
        var response = await MakeSurePublicKeyIsSet();
        if (response != null && !response.Success) return response;

        var signed = GetSignedTxn(await _txnBuilder.GetMoveStakeTxn(
            sharesAmount, fromValidator, toValidator, nonce, await _apiSdk.GetChainId(), feePerByte, GetAddress()));
        return CreateWalletResponse(await _apiSdk.BroadcastTxn(signed), signed);
    }
    public async Task<WalletResponse> MoveStake(ulong sharesAmount, string fromValidator, string toValidator, ulong feePerByte) {
        return await MoveStake(sharesAmount, fromValidator, toValidator, feePerByte, await GetNonce());
    }
    public async Task<WalletResponse> MoveStake(ulong sharesAmount, string fromValidator, string toValidator) {
        return await MoveStake(sharesAmount, fromValidator, toValidator, await _apiSdk.GetFeePerByte(), await GetNonce());
    }

    public async Task<WalletResponse> RemoveValidator(string validatorAddress, ulong feePerByte, uint nonce) {
        var response = await MakeSurePublicKeyIsSet();
        if (response != null && !response.Success) return response;

        var signed = GetSignedTxn(await _txnBuilder.GetRemoveValidatorTxn(
            validatorAddress, nonce, await _apiSdk.GetChainId(), feePerByte, GetAddress()));
        return CreateWalletResponse(await _apiSdk.BroadcastTxn(signed), signed);
    }
    public async Task<WalletResponse> RemoveValidator(string validatorAddress, ulong feePerByte) {
        return await RemoveValidator(validatorAddress, feePerByte, await GetNonce());
    }
    public async Task<WalletResponse> RemoveValidator(string validatorAddress) {
        return await RemoveValidator(validatorAddress, await _apiSdk.GetFeePerByte(), await GetNonce());
    }

    public async Task<WalletResponse> Withdraw(ulong sharesAmount, string validator, ulong feePerByte, uint nonce) {
        var response = await MakeSurePublicKeyIsSet();
        if (response != null && !response.Success) return response;

        var signed = GetSignedTxn(await _txnBuilder.GetWithdrawTxn(
            sharesAmount, validator, nonce, await _apiSdk.GetChainId(), feePerByte, GetAddress()));
        return CreateWalletResponse(await _apiSdk.BroadcastTxn(signed), signed);
    }
    public async Task<WalletResponse> Withdraw(ulong sharesAmount, string validator, ulong feePerByte) {
        return await Withdraw(sharesAmount, validator, feePerByte, await GetNonce());
    }
    public async Task<WalletResponse> Withdraw(ulong sharesAmount, string validator) {
        return await Withdraw(sharesAmount, validator, await _apiSdk.GetFeePerByte(), await GetNonce());
    }

    // --- VIDA Transactions ---
    public async Task<WalletResponse> ClaimVidaId(ulong vidaId, ulong feePerByte, uint nonce) {
        var response = await MakeSurePublicKeyIsSet();
        if (response != null && !response.Success) return response;

        var signed = GetSignedTxn(await _txnBuilder.GetClaimVidaIdTxn(
            vidaId, nonce, await _apiSdk.GetChainId(), feePerByte, GetAddress()));
        return CreateWalletResponse(await _apiSdk.BroadcastTxn(signed), signed);
    }
    public async Task<WalletResponse> ClaimVidaId(ulong vidaId, ulong feePerByte) {
        return await ClaimVidaId(vidaId, feePerByte, await GetNonce());
    }
    public async Task<WalletResponse> ClaimVidaId(ulong vidaId) {
        return await ClaimVidaId(vidaId, await _apiSdk.GetFeePerByte(), await GetNonce());
    }

    public async Task<WalletResponse> ConduitApproval(ulong vidaId, List<byte[]> transactions, ulong feePerByte, uint nonce) {
        var response = await MakeSurePublicKeyIsSet();
        if (response != null && !response.Success) return response;

        var signed = GetSignedTxn(await _txnBuilder.GetConduitApprovalTxn(
            vidaId, transactions, nonce, await _apiSdk.GetChainId(), feePerByte, GetAddress()));
        return CreateWalletResponse(await _apiSdk.BroadcastTxn(signed), signed);
    }
    public async Task<WalletResponse> ConduitApproval(ulong vidaId, List<byte[]> transactions, ulong feePerByte) {
        return await ConduitApproval(vidaId, transactions, feePerByte, await GetNonce());
    }
    public async Task<WalletResponse> ConduitApproval(ulong vidaId, List<byte[]> transactions) {
        return await ConduitApproval(vidaId, transactions, await _apiSdk.GetFeePerByte(), await GetNonce());
    }

    public async Task<WalletResponse> SendPayableVidaData(ulong vidaId, byte[] data, ulong value, ulong feePerByte, uint nonce) {
        var response = await MakeSurePublicKeyIsSet();
        if (response != null && !response.Success) return response;

        var signed = GetSignedTxn(await _txnBuilder.GetPayableVidaDataTxn(
            vidaId, data, value, nonce, await _apiSdk.GetChainId(), feePerByte, GetAddress()));
        return CreateWalletResponse(await _apiSdk.BroadcastTxn(signed), signed);
    }
    public async Task<WalletResponse> SendPayableVidaData(ulong vidaId, byte[] data, ulong value, ulong feePerByte) {
        return await SendPayableVidaData(vidaId, data, value, feePerByte, await GetNonce());
    }
    public async Task<WalletResponse> SendPayableVidaData(ulong vidaId, byte[] data, ulong value) {
        return await SendPayableVidaData(vidaId, data, value, await _apiSdk.GetFeePerByte(), await GetNonce());
    }

    public async Task<WalletResponse> SendVidaData(ulong vidaId, byte[] data, ulong feePerByte, uint nonce) {
        return await SendPayableVidaData(vidaId, data, 0, feePerByte, nonce);
    }
    public async Task<WalletResponse> SendVidaData(ulong vidaId, byte[] data, ulong feePerByte) {
        return await SendVidaData(vidaId, data, feePerByte, await GetNonce());
    }
    public async Task<WalletResponse> SendVidaData(ulong vidaId, byte[] data) {
        return await SendVidaData(vidaId, data, await _apiSdk.GetFeePerByte(), await GetNonce());
    }

    public async Task<WalletResponse> RemoveConduits(ulong vidaId, List<string> conduits, ulong feePerByte, uint nonce) {
        var response = await MakeSurePublicKeyIsSet();
        if (response != null && !response.Success) return response;

        var signed = GetSignedTxn(await _txnBuilder.GetRemoveConduitsTxn(
            vidaId, conduits, nonce, await _apiSdk.GetChainId(), feePerByte, GetAddress()));
        return CreateWalletResponse(await _apiSdk.BroadcastTxn(signed), signed);
    }
    public async Task<WalletResponse> RemoveConduits(ulong vidaId, List<string> conduits, ulong feePerByte) {
        return await RemoveConduits(vidaId, conduits, feePerByte, await GetNonce());
    }
    public async Task<WalletResponse> RemoveConduits(ulong vidaId, List<string> conduits) {
        return await RemoveConduits(vidaId, conduits, await _apiSdk.GetFeePerByte(), await GetNonce());
    }

    public async Task<WalletResponse> SetConduitMode(ulong vidaId, byte mode, ulong conduitThreshold, List<string> conduits, List<(string, ulong)> conduitsWithVotingPower, ulong feePerByte, uint nonce) {
        var response = await MakeSurePublicKeyIsSet();
        if (response != null && !response.Success) return response;

        var signed = GetSignedTxn(await _txnBuilder.GetSetConduitModeTxn(
            vidaId, mode, conduitThreshold, conduits, conduitsWithVotingPower, nonce, await _apiSdk.GetChainId(), feePerByte, GetAddress()));
        return CreateWalletResponse(await _apiSdk.BroadcastTxn(signed), signed);
    }
    public async Task<WalletResponse> SetConduitMode(ulong vidaId, byte mode, ulong conduitThreshold, List<string> conduits, List<(string, ulong)> conduitsWithVotingPower, ulong feePerByte) {
        return await SetConduitMode(vidaId, mode, conduitThreshold, conduits, conduitsWithVotingPower, feePerByte, await GetNonce());
    }
    public async Task<WalletResponse> SetConduitMode(ulong vidaId, byte mode, ulong conduitThreshold, List<string> conduits, List<(string, ulong)> conduitsWithVotingPower) {
        return await SetConduitMode(vidaId, mode, conduitThreshold, conduits, conduitsWithVotingPower, await _apiSdk.GetFeePerByte(), await GetNonce());
    }

    public async Task<WalletResponse> SetVidaPrivateState(ulong vidaId, bool privateState, ulong feePerByte, uint nonce) {
        var response = await MakeSurePublicKeyIsSet();
        if (response != null && !response.Success) return response;

        var signed = GetSignedTxn(await _txnBuilder.GetSetVidaPrivateStateTxn(
            vidaId, privateState, nonce, await _apiSdk.GetChainId(), feePerByte, GetAddress()));
        return CreateWalletResponse(await _apiSdk.BroadcastTxn(signed), signed);
    }
    public async Task<WalletResponse> SetVidaPrivateState(ulong vidaId, bool privateState, ulong feePerByte) {
        return await SetVidaPrivateState(vidaId, privateState, feePerByte, await GetNonce());
    }
    public async Task<WalletResponse> SetVidaPrivateState(ulong vidaId, bool privateState) {
        return await SetVidaPrivateState(vidaId, privateState, await _apiSdk.GetFeePerByte(), await GetNonce());
    }

    public async Task<WalletResponse> SetVidaToAbsolutePublic(ulong vidaId, ulong feePerByte, uint nonce) {
        var response = await MakeSurePublicKeyIsSet();
        if (response != null && !response.Success) return response;

        var signed = GetSignedTxn(await _txnBuilder.GetSetVidaToAbsolutePublicTxn(
            vidaId, nonce, await _apiSdk.GetChainId(), feePerByte, GetAddress()));
        return CreateWalletResponse(await _apiSdk.BroadcastTxn(signed), signed);
    }
    public async Task<WalletResponse> SetVidaToAbsolutePublic(ulong vidaId, ulong feePerByte) {
        return await SetVidaToAbsolutePublic(vidaId, feePerByte, await GetNonce());
    }
    public async Task<WalletResponse> SetVidaToAbsolutePublic(ulong vidaId) {
        return await SetVidaToAbsolutePublic(vidaId, await _apiSdk.GetFeePerByte(), await GetNonce());
    }

    public async Task<WalletResponse> AddVidaSponsoredAddresses(ulong vidaId, List<string> sponsoredAddresses, ulong feePerByte, uint nonce) {
        var response = await MakeSurePublicKeyIsSet();
        if (response != null && !response.Success) return response;

        var signed = GetSignedTxn(await _txnBuilder.GetAddVidaSponsoredAddressesTxn(
            vidaId, sponsoredAddresses, nonce, await _apiSdk.GetChainId(), feePerByte, GetAddress()));
        return CreateWalletResponse(await _apiSdk.BroadcastTxn(signed), signed);
    }
    public async Task<WalletResponse> AddVidaSponsoredAddresses(ulong vidaId, List<string> sponsoredAddresses, ulong feePerByte) {
        return await AddVidaSponsoredAddresses(vidaId, sponsoredAddresses, feePerByte, await GetNonce());
    }
    public async Task<WalletResponse> AddVidaSponsoredAddresses(ulong vidaId, List<string> sponsoredAddresses) {
        return await AddVidaSponsoredAddresses(vidaId, sponsoredAddresses, await _apiSdk.GetFeePerByte(), await GetNonce());
    }

    public async Task<WalletResponse> AddVidaAllowedSenders(ulong vidaId, List<string> allowedSenders, ulong feePerByte, uint nonce) {
        var response = await MakeSurePublicKeyIsSet();
        if (response != null && !response.Success) return response;

        var signed = GetSignedTxn(await _txnBuilder.GetAddVidaAllowedSendersTxn(
            vidaId, allowedSenders, nonce, await _apiSdk.GetChainId(), feePerByte, GetAddress()));
        return CreateWalletResponse(await _apiSdk.BroadcastTxn(signed), signed);
    }
    public async Task<WalletResponse> AddVidaAllowedSenders(ulong vidaId, List<string> allowedSenders, ulong feePerByte) {
        return await AddVidaAllowedSenders(vidaId, allowedSenders, feePerByte, await GetNonce());
    }
    public async Task<WalletResponse> AddVidaAllowedSenders(ulong vidaId, List<string> allowedSenders) {
        return await AddVidaAllowedSenders(vidaId, allowedSenders, await _apiSdk.GetFeePerByte(), await GetNonce());
    }

    public async Task<WalletResponse> RemoveVidaAllowedSenders(ulong vidaId, List<string> allowedSenders, ulong feePerByte, uint nonce) {
        var response = await MakeSurePublicKeyIsSet();
        if (response != null && !response.Success) return response;

        var signed = GetSignedTxn(await _txnBuilder.GetRemoveVidaAllowedSendersTxn(
            vidaId, allowedSenders, nonce, await _apiSdk.GetChainId(), feePerByte, GetAddress()));
        return CreateWalletResponse(await _apiSdk.BroadcastTxn(signed), signed);
    }
    public async Task<WalletResponse> RemoveVidaAllowedSenders(ulong vidaId, List<string> allowedSenders, ulong feePerByte) {
        return await RemoveVidaAllowedSenders(vidaId, allowedSenders, feePerByte, await GetNonce());
    }
    public async Task<WalletResponse> RemoveVidaAllowedSenders(ulong vidaId, List<string> allowedSenders) {
        return await RemoveVidaAllowedSenders(vidaId, allowedSenders, await _apiSdk.GetFeePerByte(), await GetNonce());
    }

    public async Task<WalletResponse> RemoveSponsoredAddresses(ulong vidaId, List<string> sponsoredAddresses, ulong feePerByte, uint nonce) {
        var response = await MakeSurePublicKeyIsSet();
        if (response != null && !response.Success) return response;

        var signed = GetSignedTxn(await _txnBuilder.GetRemoveSponsoredAddressesTxn(
            vidaId, sponsoredAddresses, nonce, await _apiSdk.GetChainId(), feePerByte, GetAddress()));
        return CreateWalletResponse(await _apiSdk.BroadcastTxn(signed), signed);
    }
    public async Task<WalletResponse> RemoveSponsoredAddresses(ulong vidaId, List<string> sponsoredAddresses, ulong feePerByte) {
        return await RemoveSponsoredAddresses(vidaId, sponsoredAddresses, feePerByte, await GetNonce());
    }
    public async Task<WalletResponse> RemoveSponsoredAddresses(ulong vidaId, List<string> sponsoredAddresses) {
        return await RemoveSponsoredAddresses(vidaId, sponsoredAddresses, await _apiSdk.GetFeePerByte(), await GetNonce());
    }

    public async Task<WalletResponse> SetPWRTransferRights(ulong vidaId, bool ownerCanTransferPwr, ulong feePerByte, uint nonce) {
        var response = await MakeSurePublicKeyIsSet();
        if (response != null && !response.Success) return response;

        var signed = GetSignedTxn(await _txnBuilder.GetSetPwrTransferRightsTxn(
            vidaId, ownerCanTransferPwr, nonce, await _apiSdk.GetChainId(), feePerByte, GetAddress()));
        return CreateWalletResponse(await _apiSdk.BroadcastTxn(signed), signed);
    }
    public async Task<WalletResponse> SetPWRTransferRights(ulong vidaId, bool ownerCanTransferPwr, ulong feePerByte) {
        return await SetPWRTransferRights(vidaId, ownerCanTransferPwr, feePerByte, await GetNonce());
    }
    public async Task<WalletResponse> SetPWRTransferRights(ulong vidaId, bool ownerCanTransferPwr) {
        return await SetPWRTransferRights(vidaId, ownerCanTransferPwr, await _apiSdk.GetFeePerByte(), await GetNonce());
    }

    public async Task<WalletResponse> TransferPWRFromVida(ulong vidaId, string receiver, ulong amount, ulong feePerByte, uint nonce) {
        var response = await MakeSurePublicKeyIsSet();
        if (response != null && !response.Success) return response;

        var signed = GetSignedTxn(await _txnBuilder.GetTransferPwrFromVidaTxn(
            vidaId, receiver, amount, nonce, await _apiSdk.GetChainId(), feePerByte, GetAddress()));
        return CreateWalletResponse(await _apiSdk.BroadcastTxn(signed), signed);
    }
    public async Task<WalletResponse> TransferPWRFromVida(ulong vidaId, string receiver, ulong amount, ulong feePerByte) {
        return await TransferPWRFromVida(vidaId, receiver, amount, feePerByte, await GetNonce());
    }
    public async Task<WalletResponse> TransferPWRFromVida(ulong vidaId, string receiver, ulong amount) {
        return await TransferPWRFromVida(vidaId, receiver, amount, await _apiSdk.GetFeePerByte(), await GetNonce());
    }

    public RPC GetRpc() {
        return _apiSdk;
    }

    private async Task<WalletResponse?> MakeSurePublicKeyIsSet() {
        uint nonce = await GetNonce();

        if (nonce == 0) {
            return await SetPublicKey();
        }

        return null;
    }
}
