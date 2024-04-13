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

  public PwrWallet(PwrApiSdk apiSdk)
      : this(apiSdk, EthECKey.GenerateKey().GetPrivateKeyAsBytes().ToHex()) {
    _apiSdk = apiSdk;
  }

  public PwrWallet(PwrApiSdk apiSdk, string? privateKeyHex = null) {
    _apiSdk = apiSdk;

    try {
      _ecKey = new EthECKey(privateKeyHex);

      PrivateKeyHex = _ecKey.GetPrivateKeyAsBytes().ToHex();
      PublicKeyHex = _ecKey.GetPubKey().ToHex();

      PublicAddress = _ecKey.GetPublicAddress();

      Console.WriteLine("Private Key as Hex : " + PrivateKeyHex);
      Console.WriteLine("Public Key as Hex : " + PublicKeyHex);
      Console.WriteLine("Public Address : " + PublicAddress);
    } catch (Exception ex) {
      Console.WriteLine("Error generating keys: " + ex.Message);
    }
  }

  public PwrWallet(PwrApiSdk apiSdk, BigInteger privateKey)
      : this(apiSdk, BitConverter.ToString(privateKey.ToByteArray())
                         .Replace("-", "")
                         .ToLower()) {}

  public PwrWallet(PwrApiSdk apiSdk, EthECKey key) {
    _apiSdk = apiSdk;
    _ecKey = key;
    PrivateKeyHex = key.GetPrivateKeyAsBytes().ToHex();
    PublicKeyHex = key.GetPubKey().ToHex();

    PublicAddress = key.GetPublicAddress();
  }

  public PwrWallet(PwrApiSdk apiSdk, byte[] privateKey)
      : this(apiSdk,
             BitConverter.ToString(privateKey).Replace("-", "").ToLower()) {}

  public string PrivateKeyHex { get; }

  public string PublicKeyHex { get; }

  public string PublicAddress { get; }

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
  /// Constructs a signed transaction for transferring PWR tokens.
  /// </summary>
  /// <param name="to">The recipient's address.</param>
  /// <param name="amount">The amount of tokens to transfer.</param>
  /// <param name="nonce">The nonce value of the transaction.</param>
  /// <returns>The signed transaction.</returns>
  public async Task<byte[]> GetSignedTransferPWRTxn(string to, ulong amount,
                                                    uint nonce) {
    return GetSignedTxn(await _txnBuilder.GetTransferPWRTxn(
        to, amount, nonce, await _apiSdk.GetChainId()));
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
    var signed = await GetSignedTransferPWRTxn(to, amount, nonce);
    return CreateWalletResponse(await _apiSdk.BroadcastTxn(signed), signed);
  }
  public async Task<WalletResponse> TransferPWR(string to, ulong amount) {
    return await TransferPWR(to, amount, await GetNonce());
  }

  /// <summary>
  /// Constructs a signed transaction for joining a node.
  /// </summary>
  /// <param name="ip">The IP address of the node to join.</param>
  /// <param name="nonce">The nonce value of the transaction.</param>
  /// <returns>The signed transaction.</returns>
  public async Task<byte[]> GetSignedJointxn(string ip, uint nonce) {
    return GetSignedTxn(
        await _txnBuilder.GetJointxn(ip, nonce, await _apiSdk.GetChainId()));
  }
  /// <summary>
  /// Joins a node using the specified IP address.
  /// </summary>
  /// <param name="ip">The IP address of the node to join.</param>
  /// <param name="nonce">The nonce value of the transaction.</param>
  /// <returns>The response of the join operation.</returns>
  public async Task<WalletResponse> Join(string ip, uint nonce) {
    byte[] signed = await GetSignedJointxn(ip, nonce);
    return CreateWalletResponse(await _apiSdk.BroadcastTxn(signed), signed);
  }
  /// <summary>
  /// Joins a node using the specified IP address.
  /// </summary>
  /// <param name="ip">The IP address of the node to join.</param>
  /// <returns>The response of the join operation.</returns>
  public async Task<WalletResponse> Join(string ip) {
    return await Join(ip, await GetNonce());
  }

  public async Task<byte[]> GetSignedClaimActiveNodeSpotTxn(uint nonce) {
    return GetSignedTxn(await _txnBuilder.GetClaimActiveNodeSpotTxn(
        nonce, await _apiSdk.GetChainId()));
  }
  /// <summary>
  /// Claims an active node spot.
  /// </summary>
  /// <param name="nonce">The nonce value of the transaction.</param>
  /// <returns>The response of the claim operation.</returns>
  public async Task<WalletResponse> ClaimActiveNodeSpot(uint nonce) {
    byte[] signed = await GetSignedClaimActiveNodeSpotTxn(nonce);
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
  /// Constructs a signed transaction for delegating PWR tokens.
  /// </summary>
  /// <param name="to">The recipient's address.</param>
  /// <param name="amount">The amount of tokens to delegate.</param>
  /// <param name="nonce">The nonce value of the transaction.</param>
  /// <returns>The signed transaction.</returns>
  public async Task<byte[]> GetSignedDelegateTxn(string to, ulong amount,
                                                 uint nonce) {
    return GetSignedTxn(await _txnBuilder.GetDelegateTxn(
        to, amount, nonce, await _apiSdk.GetChainId()));
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
    var signed = await GetSignedDelegateTxn(to, amount, nonce);
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
  /// Constructs a signed transaction for withdrawing shares from the specified
  /// address.
  /// </summary>
  /// <param name="from">The sender's address.</param>
  /// <param name="sharesAmount">The amount of shares to withdraw.</param>
  /// <param name="nonce">The nonce value of the transaction.</param>
  /// <returns>The signed transaction.</returns>
  public async Task<byte[]> GetSignedWithdrawTxn(string from,
                                                 ulong sharesAmount,
                                                 uint nonce) {
    return GetSignedTxn(await _txnBuilder.GetWithdrawTxn(
        from, sharesAmount, nonce, await _apiSdk.GetChainId()));
  }
  /// <summary>
  /// Withdraws PWR tokens from the specified address.
  /// </summary>
  /// <param name="to">The recipient's address.</param>
  /// <param name="amount">The amount of tokens to withdraw.</param>
  /// <param name="nonce">The nonce value of the transaction.</param>
  /// <returns>The response of the withdrawal operation.</returns>
  public async Task<WalletResponse> WithDraw(string to, ulong amount,
                                             uint nonce) {
    byte[] signed = await GetSignedWithdrawTxn(to, amount, nonce);
    return CreateWalletResponse(await _apiSdk.BroadcastTxn(signed), signed);
  }
  /// <summary>
  /// Withdraws PWR tokens from the specified address with the current nonce
  /// value.
  /// </summary>
  /// <param name="to">The recipient's address.</param>
  /// <param name="amount">The amount of tokens to withdraw.</param>
  /// <returns>The response of the withdrawal operation.</returns>
  public async Task<WalletResponse> WithDraw(string to, ulong amount) {
    return await WithDraw(to, amount, await GetNonce());
  }

  /// <summary>
  /// Constructs a signed transaction for withdrawing PWR tokens from the
  /// specified address.
  /// </summary>
  /// <param name="from">The sender's address.</param>
  /// <param name="pwrAmount">The amount of PWR tokens to withdraw.</param>
  /// <param name="nonce">The nonce value of the transaction.</param>
  /// <returns>The signed transaction.</returns>
  public async Task<byte[]> GetSignedWithdrawPWRTxnAsync(string from,
                                                         ulong pwrAmount,
                                                         uint nonce) {
    return GetSignedTxn(await _txnBuilder.GetWithdrawPWRTxn(
        from, pwrAmount, nonce, await _apiSdk.GetChainId()));
  }
  /// <summary>
  /// Withdraws PWR tokens from the specified address.
  /// </summary>
  /// <param name="to">The recipient's address.</param>
  /// <param name="amount">The amount of tokens to withdraw.</param>
  /// <param name="nonce">The nonce value of the transaction.</param>
  /// <returns>The response of the withdrawal operation.</returns>
  public async Task<WalletResponse> WithDrawPWR(string to, ulong amount,
                                                uint nonce) {
    byte[] signed = await GetSignedWithdrawTxn(to, amount, nonce);
    return CreateWalletResponse(await _apiSdk.BroadcastTxn(signed), signed);
  }
  /// <summary>
  /// Withdraws PWR tokens from the specified address with the current nonce
  /// value.
  /// </summary>
  /// <param name="to">The recipient's address.</param>
  /// <param name="amount">The amount of tokens to withdraw.</param>
  /// <returns>The response of the withdrawal operation.</returns>
  public async Task<WalletResponse> WithDrawPWR(string to, ulong amount) {
    return await WithDraw(to, amount, await GetNonce());
  }

  /// <summary>
  /// Constructs a signed transaction for sending data to a virtual machine.
  /// </summary>
  /// <param name="vmId">The ID of the virtual machine.</param>
  /// <param name="data">The data to send.</param>
  /// <param name="nonce">The nonce value of the transaction.</param>
  /// <returns>The signed transaction.</returns>
  public async Task<byte[]> GetSignedVmDataTxn(ulong vmId, byte[] data,
                                               uint nonce) {
    return GetSignedTxn(await _txnBuilder.GetVmDataTxn(
        vmId, data, nonce, await _apiSdk.GetChainId()));
  }
  /// <summary>
  /// Sends data to a virtual machine.
  /// </summary>
  /// <param name="vmId">The ID of the virtual machine.</param>
  /// <param name="data">The data to send.</param>
  /// <param name="nonce">The nonce value of the transaction.</param>
  /// <returns>The response of the transaction operation.</returns>
  public async Task<WalletResponse> SendVmDataTxn(ulong vmId, byte[] data,
                                                  uint nonce) {
    byte[] signed = await GetSignedVmDataTxn(vmId, data, nonce);
    return CreateWalletResponse(await _apiSdk.BroadcastTxn(signed), signed);
  }
  /// <summary>
  /// Sends data to a virtual machine with the current nonce value.
  /// </summary>
  /// <param name="vmId">The ID of the virtual machine.</param>
  /// <param name="data">The data to send.</param>
  /// <returns>The response of the transaction operation.</returns>
  public async Task<WalletResponse> SendVmDataTxn(ulong vmId, byte[] data) {
    return await SendVmDataTxn(vmId, data, await GetNonce());
  }

  /// <summary>
  /// Constructs a signed transaction for claiming a virtual machine ID.
  /// </summary>
  /// <param name="vmId">The ID of the virtual machine.</param>
  /// <param name="nonce">The nonce value of the transaction.</param>
  /// <returns>The signed transaction.</returns>
  public async Task<byte[]> GetSignedClaimVmIdTxn(ulong vmId, uint nonce) {
    return GetSignedTxn(await _txnBuilder.GetClaimVmIdTxn(
        vmId, nonce, await _apiSdk.GetChainId()));
  }
  /// <summary>
  /// Claims a virtual machine ID.
  /// </summary>
  /// <param name="vmId">The ID of the virtual machine to claim.</param>
  /// <param name="nonce">The nonce value of the transaction.</param>
  /// <returns>The response of the claiming operation.</returns>
  public async Task<WalletResponse> ClaimVmId(ulong vmid, uint nonce) {
    byte[] signed = await GetSignedClaimVmIdTxn(vmid, nonce);
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
  /// Constructs a signed transaction for sending a conduit transaction.
  /// </summary>
  /// <param name="vmId">The ID of the virtual machine.</param>
  /// <param name="txn">The transaction data.</param>
  /// <param name="nonce">The nonce value of the transaction.</param>
  /// <returns>The signed transaction.</returns>
  public async Task<byte[]> GetSignedSendConduitTransactionTxn(ulong vmId,
                                                               byte[] txn,
                                                               uint nonce) {
    return GetSignedTxn(await _txnBuilder.GetSendConduitTransactionTxn(
        vmId, txn, nonce, await _apiSdk.GetChainId()));
  }
  /// <summary>
  /// Sends a conduit transaction to a virtual machine.
  /// </summary>
  /// <param name="vmId">The ID of the virtual machine.</param>
  /// <param name="txn">The transaction data.</param>
  /// <param name="nonce">The nonce value of the transaction.</param>
  /// <returns>The response of the transaction operation.</returns>
  public async Task<WalletResponse> SendConduitTransaction(ulong vmId,
                                                           byte[] txn,
                                                           uint nonce) {
    byte[] signed = await GetSignedSendConduitTransactionTxn(vmId, txn, nonce);
    return CreateWalletResponse(await _apiSdk.BroadcastTxn(signed), signed);
  }
  /// <summary>
  /// Sends a conduit transaction to a virtual machine with the current nonce
  /// value.
  /// </summary>
  /// <param name="vmId">The ID of the virtual machine.</param>
  /// <param name="txn">The transaction data.</param>
  /// <returns>The response of the transaction operation.</returns>
  public async Task<WalletResponse> SendConduitTransaction(ulong vmId,
                                                           byte[] txn) {
    return await SendConduitTransaction(vmId, txn, await GetNonce());
  }

  /// <summary>
  /// Constructs a signed transaction for setting a guardian for the specified
  /// address.
  /// </summary>
  /// <param name="guardianAddress">The guardian's address.</param>
  /// <param name="expiryDate">The expiry date of the guardian.</param>
  /// <param name="nonce">The nonce value of the transaction.</param>
  /// <returns>The signed transaction.</returns>
  public async Task<byte[]> GetSignedSetGuardianTxn(string guardianAddress,
                                                    ulong expiryDate,
                                                    uint nonce) {
    return GetSignedTxn(await _txnBuilder.GetSetGuardianTxn(
        guardianAddress, expiryDate, nonce, await _apiSdk.GetChainId()));
  }
  /// <summary>
  /// Constructs a signed transaction for setting a guardian for the specified
  /// address with the current nonce value.
  /// </summary>
  /// <param name="guardianAddress">The guardian's address.</param>
  /// <param name="expiryDate">The expiry date of the guardian.</param>
  /// <returns>The signed transaction.</returns>
  public async Task<byte[]> GetSignedSetGuardianTxn(string guardianAddress,
                                                    ulong expiryDate) {
    return await GetSignedSetGuardianTxn(guardianAddress, expiryDate,
                                         await GetNonce());
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
    byte[] signed =
        await GetSignedSetGuardianTxn(guardianAddress, expiryDate, nonce);
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
  /// Constructs a signed transaction for removing a guardian.
  /// </summary>
  /// <param name="nonce">The nonce value of the transaction.</param>
  /// <returns>The signed transaction.</returns>
  public async Task<byte[]> GetSignedRemoveGuardianTxn(uint nonce) {
    return GetSignedTxn(await _txnBuilder.GetRemoveGuardianTxn(
        nonce, await _apiSdk.GetChainId()));
  }
  /// <summary>
  /// Constructs a signed transaction for removing a guardian.
  /// </summary>
  /// <returns>The signed transaction.</returns>
  public async Task<byte[]> GetSignedRemoveGuardianTxn() {
    return GetSignedTxn(await _txnBuilder.GetRemoveGuardianTxn(
        await GetNonce(), await _apiSdk.GetChainId()));
  }
  /// <summary>
  /// Removes a guardian.
  /// </summary>
  /// <param name="nonce">The nonce value of the transaction.</param>
  /// <returns>The response of the removing guardian operation.</returns>
  public async Task<WalletResponse> RemoveGuardian(uint nonce) {
    byte[] signed = await GetSignedRemoveGuardianTxn(nonce);
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
  /// Constructs a signed transaction for sending a guardian wrapped
  /// transaction.
  /// </summary>
  /// <param name="txn">The transaction data.</param>
  /// <param name="nonce">The nonce value of the transaction.</param>
  /// <returns>The signed transaction.</returns>
  public async Task<byte[]> GetSignedGuardianApprovalTransactionTxn(
      List<byte[]> txns, uint nonce) {
    return GetSignedTxn(await _txnBuilder.GetGuardianApprovalTxn (
        txns, nonce, await _apiSdk.GetChainId()));
  }
  /// <summary>
  /// Sends a guardian wrapped transaction.
  /// </summary>
  /// <param name="txn">The transaction data.</param>
  /// <param name="nonce">The nonce value of the transaction.</param>
  /// <returns>The response of the transaction operation.</returns>
  public async Task<WalletResponse> SendGuardianApprovalTransaction(List<byte[]> txns,
                                                                   uint nonce) {
    byte[] signed =
        await GetSignedGuardianApprovalTransactionTxn(txns, nonce);
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
  /// Constructs a signed transaction for removing a validator.
  /// </summary>
  /// <param name="validator">The address of the validator to remove.</param>
  /// <param name="nonce">The nonce value of the transaction.</param>
  /// <returns>The signed transaction.</returns>
  public async Task<byte[]> GetSignedSendValidatorRemoveTxn(string validator,
                                                            uint nonce) {
    return GetSignedTxn(await _txnBuilder.GetValidatorRemoveTxn(
        validator, nonce, await _apiSdk.GetChainId()));
  }
  /// <summary>
  /// Sends a transaction to remove a validator.
  /// </summary>
  /// <param name="validator">The address of the validator to remove.</param>
  /// <param name="nonce">The nonce value of the transaction.</param>
  /// <returns>The response of the transaction operation.</returns>
  public async Task<WalletResponse> SendValidatorRemoveTxn(string validator,
                                                           uint nonce) {
    byte[] signed = await GetSignedSendValidatorRemoveTxn(validator, nonce);
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

  public async Task<byte[]> GetSignedConduitApprovalTxn(ulong vmId,
                                                        List<byte[]> txns,
                                                        uint nonce) {
    return GetSignedTxn(await _txnBuilder.GetConduitApprovalTxn(
        vmId, txns, nonce, await _apiSdk.GetChainId()));
  }

  public async Task<ApiResponse> ConduitApprove(ulong vmId,
                                                List<byte[]> transactions,
                                                uint nonce) {
    return await _apiSdk.BroadcastTxn(
        await GetSignedConduitApprovalTxn(vmId, transactions, nonce));
  }

  public async Task<ApiResponse> ConduitApprove(ulong vmId,
                                                List<byte[]> transactions) {
    return await _apiSdk.BroadcastTxn(await GetSignedConduitApprovalTxn(
        vmId, transactions, await GetNonce()));
  }

  public async Task<byte[]> GetSignedSetConduitsTxn(ulong vmId,
                                                    List<byte[]> conduits,
                                                    uint nonce) {
    return GetSignedTxn(await _txnBuilder.GetSetConduitsTxn(
        vmId, conduits, nonce, await _apiSdk.GetChainId()));
  }

  public async Task<ApiResponse> SetConduits(ulong vmId, List<byte[]> conduits,
                                             uint nonce) {
    return await _apiSdk.BroadcastTxn(
        await GetSignedSetConduitsTxn(vmId, conduits, nonce));
  }

  public async Task<ApiResponse> SetConduits(ulong vmId,
                                             List<byte[]> conduits) {
    return await _apiSdk.BroadcastTxn(
        await GetSignedSetConduitsTxn(vmId, conduits, await GetNonce()));
  }

  public async Task<byte[]> GetSignedAddConduitsTxn(ulong vmId,
                                                    List<byte[]> conduits,
                                                    uint nonce) {
    return GetSignedTxn(await _txnBuilder.GetAddConduitsTxn(
        vmId, conduits, nonce, await _apiSdk.GetChainId()));
  }
  public async Task<ApiResponse> AddConduits(ulong vmId, List<byte[]> conduits,
                                             uint nonce) {
    return await _apiSdk.BroadcastTxn(
        await GetSignedAddConduitsTxn(vmId, conduits, nonce));
  }

  public async Task<ApiResponse> AddConduits(ulong vmId,
                                             List<byte[]> conduits) {
    return await _apiSdk.BroadcastTxn(
        await GetSignedAddConduitsTxn(vmId, conduits, await GetNonce()));
  }

  public async Task<byte[]> GetSignedRemoveConduitsTxn(ulong vmId,
                                                       List<byte[]> conduits,
                                                       uint nonce) {
    return GetSignedTxn(await _txnBuilder.GetRemoveConduitsTxn(
        vmId, conduits, nonce, await _apiSdk.GetChainId()));
  }

  public async Task<ApiResponse> RemoveConduits(ulong vmId,
                                                List<byte[]> conduits,
                                                uint nonce) {
    return await _apiSdk.BroadcastTxn(
        await GetSignedRemoveConduitsTxn(vmId, conduits, nonce));
  }
  public async Task<ApiResponse> RemoveConduits(ulong vmId,
                                                List<byte[]> conduits) {
    return await _apiSdk.BroadcastTxn(
        await GetSignedRemoveConduitsTxn(vmId, conduits, await GetNonce()));
  }

  public async Task<WalletResponse> SendPayableVmDataTxn(ulong vmId,
                                                         ulong value,
                                                         byte[] data,
                                                         uint nonce) {
    byte[] signed = await GetSignedPayableVmDataTxn(vmId, value, data, nonce);
    return CreateWalletResponse(await _apiSdk.BroadcastTxn(signed), signed);
  }

  public async Task<byte[]> GetSignedPayableVmDataTxn(ulong vmId, ulong value,
                                                      byte[] data, uint nonce) {
    return GetSignedTxn(await _txnBuilder.GetPayableVmDataTxn(vmId, value, data, nonce, await _apiSdk.GetChainId()));
  }

  public async Task<ApiResponse> MoveStake(ulong sharesAmount,
                                           String fromValidator,
                                           String toValidator, uint nonce) {
    return await _apiSdk.BroadcastTxn(await GetSignedMoveStakeTxn(
        sharesAmount, fromValidator, toValidator, nonce));
  }

  public async Task<byte[]> GetSignedMoveStakeTxn(ulong sharesAmount,
                                                  String fromValidator,
                                                  String toValidator,
                                                  uint nonce) {
    return GetSignedTxn(await _txnBuilder.GetMoveStakeTxn(
        sharesAmount, fromValidator, toValidator, nonce,
        await _apiSdk.GetChainId()));
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
