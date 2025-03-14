using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Model;
using Nethereum.Signer;
using Nethereum.Util;
using PWRCS.Models;

namespace PWRCS;

public class TransactionBuilder {
	/// <summary>
	/// Constructs the base of a transaction.
	/// </summary>
	/// <param name="identifier">The identifier for the transaction.</param>
	/// <param name="nonce">The nonce value of the transaction.</param>
	/// <returns>The base of the transaction.</returns>
	public async Task<byte[]> GetTxnBase(uint identifier, uint nonce,
										byte chainId) {
		MemoryStream stream = new MemoryStream(9);

		stream.Write(GetBigEndianBytes(identifier));
		stream.WriteByte(chainId);
		stream.Write(GetBigEndianBytes(nonce));

		return stream.ToArray();
	}
 
	/// <summary>
	/// Constructs a transaction for transferring PWR tokens.
	/// </summary>
	/// <param name="to">The recipient's address.</param>
	/// <param name="amount">The amount of tokens to transfer.</param>
	/// <param name="nonce">The nonce value of the transaction.</param>
	/// <returns>The constructed transaction.</returns>
	public async Task<byte[]> GetTransferPWRTxn(string to, ulong amount,
												uint nonce, byte chainId) {
		ValidateAddress(to);

		to = to.Substring(2);
		byte[] txnBase = await GetTxnBase(0, nonce, chainId);
		using(MemoryStream stream = new MemoryStream(txnBase.Length + 8 + 20)) {
			stream.Write(txnBase);
			stream.Write(GetBigEndianBytes(amount));
			stream.Write(Extensions.HexStringToByteArray(to));
			return stream.ToArray();
		}
	}

	/// <summary>
	/// Retrieves a transaction for joining a node.
	/// </summary>
	/// <param name="ip">The IP address of the node to join.</param>
	/// <param name="nonce">The nonce value of the transaction.</param>
	/// <returns>The constructed transaction.</returns>
	public async Task<byte[]> GetJoinAsValidatorTxn(string ip, uint nonce, byte chainId) {
		byte[] txnBase = await GetTxnBase(1, nonce, chainId);
		byte[] ipBytes = Encoding.UTF8.GetBytes(ip);

		using MemoryStream stream =
			new MemoryStream(txnBase.Length + ipBytes.Length);
		stream.Write(txnBase, 0, txnBase.Length);
		stream.Write(ipBytes, 0, ipBytes.Length);
		return stream.ToArray();
	}

	public async Task<byte[]> GetClaimActiveNodeSpotTxn(uint nonce,
														byte chaindId) {
		byte[] txnBase = await GetTxnBase(2, nonce, chaindId);
		return txnBase;
	}

	/// <summary>
	/// Constructs a transaction for delegating PWR tokens.
	/// </summary>
	/// <param name="to">The recipient's address.</param>
	/// <param name="amount">The amount of tokens to delegate.</param>
	/// <param name="nonce">The nonce value of the transaction.</param>
	/// <returns>The constructed transaction.</returns>
	public async Task<byte[]> GetDelegateTxn(string to, ulong amount, uint nonce,
											byte chainId) {
		ValidateAddress(to);
		to = to.Substring(2);

		byte[] txnBase = await GetTxnBase(3, nonce, chainId);
		byte[] toBytes = Extensions.HexStringToByteArray(to);

		using(MemoryStream stream =
				new MemoryStream(txnBase.Length + 8 + toBytes.Length)) {
			stream.Write(txnBase, 0, txnBase.Length);
			stream.Write(GetBigEndianBytes(amount));
			stream.Write(toBytes);
			return stream.ToArray();
		}
	}

	/// <summary>
	/// Constructs a transaction for withdrawing shares from the specified
	/// address.
	/// </summary>
	/// <param name="from">The sender's address.</param>
	/// <param name="sharesAmount">The amount of shares to withdraw.</param>
	/// <param name="nonce">The nonce value of the transaction.</param>
	/// <returns>The constructed transaction.</returns>
	public async Task<byte[]> GetWithdrawTxn(string from, ulong sharesAmount,
											uint nonce, byte chainId) {
		ValidateAddress(from);
		from = from.Substring(2);

		byte[] txnBase = await GetTxnBase(4, nonce, chainId);
		byte[] fromBytes = Extensions.HexStringToByteArray(from);

		using(MemoryStream stream =
				new MemoryStream(txnBase.Length + 8 + fromBytes.Length)) {
			stream.Write(txnBase);
			stream.Write(GetBigEndianBytes(sharesAmount));
			stream.Write(fromBytes);
			return stream.ToArray();
		}
	}

	/// <summary>
	/// Constructs a transaction for sending data to a virtual machine.
	/// </summary>
	/// <param name="vmId">The ID of the virtual machine.</param>
	/// <param name="data">The data to send.</param>
	/// <param name="nonce">The nonce value of the transaction.</param>
	/// <returns>The constructed transaction.</returns>
	public async Task<byte[]> GetVmDataTxn(ulong vmId, byte[] data, uint nonce,
											byte chainId) {
		byte[] txnBase = await GetTxnBase(5, nonce, chainId);

		using(MemoryStream stream = new MemoryStream(txnBase.Length + 8 + data.Length)) {
			stream.Write(txnBase);
			stream.Write(GetBigEndianBytes(vmId));
			stream.Write(GetBigEndianBytes(data.Length));
			stream.Write(data);
			return stream.ToArray();
		}
	}

	/// <summary>
	/// Constructs a transaction for claiming a virtual machine ID.
	/// </summary>
	/// <param name="vmId">The ID of the virtual machine.</param>
	/// <param name="nonce">The nonce value of the transaction.</param>
	/// <returns>The constructed transaction.</returns>
	public async Task<byte[]> GetClaimVmIdTxn(ulong vmId, uint nonce,
												byte chainId) {
		byte[] txnBase = await GetTxnBase(6, nonce, chainId);

		using(MemoryStream stream = new MemoryStream(txnBase.Length + 8)) {
			stream.Write(txnBase);
			stream.Write(GetBigEndianBytes(vmId));
			return stream.ToArray();
		}
	}

	/// <summary>
	/// Constructs a transaction for removing a validator.
	/// </summary>
	/// <param name="validator">The address of the validator to remove.</param>
	/// <param name="nonce">The nonce value of the transaction.</param>
	/// <returns>The constructed transaction.</returns>
	public async Task<byte[]> GetValidatorRemoveTxn(string validator, uint nonce,
													byte chainId) {
		ValidateAddress(validator);
		validator = validator.Substring(2);

		byte[] txnBase = await GetTxnBase(7, nonce, chainId);

		using(MemoryStream stream = new MemoryStream(txnBase.Length + 20)) {
			stream.Write(txnBase);
			stream.Write(Extensions.HexStringToByteArray(validator));
			return stream.ToArray();
		}
	}

	/// <summary>
	/// Constructs a transaction for setting a guardian for the specified address.
	/// </summary>
	/// <param name="guardianAddress">The guardian's address.</param>
	/// <param name="expiryDate">The expiry date of the guardian.</param>
	/// <param name="nonce">The nonce value of the transaction.</param>
	/// <returns>The constructed transaction.</returns>
	public async Task<byte[]> GetSetGuardianTxn(string guardianAddress,
												ulong expiryDate, uint nonce,
												byte chainId) {
		ValidateAddress(guardianAddress);
		if (expiryDate < (ulong) DateTimeOffset.Now.ToUnixTimeSeconds()) {
			throw new ArgumentException("Expiry date cannot be in the past");
		}

		guardianAddress = guardianAddress.Substring(2);

		byte[] txnBase = await GetTxnBase(8, nonce, chainId);
		byte[] guardianAddressBytes = Extensions.HexStringToByteArray(guardianAddress);
		
		using(MemoryStream stream = new MemoryStream(txnBase.Length + 20 +
												guardianAddressBytes.Length)) {
			stream.Write(txnBase);
			stream.Write(GetBigEndianBytes(expiryDate));
			stream.Write(guardianAddressBytes);
			return stream.ToArray();
		}
	}

	/// <summary>
	/// Constructs a transaction for removing a guardian.
	/// </summary>
	/// <param name="nonce">The nonce value of the transaction.</param>
	/// <returns>The constructed transaction.</returns>
	public async Task<byte[]> GetRemoveGuardianTxn(uint nonce, byte chainId) {
		byte[] txnBase = await GetTxnBase(9, nonce, chainId);
		return txnBase;
	}

	/// <summary>
	/// Constructs a transaction for sending a guardian wrapped transaction.
	/// </summary>
	/// <param name="txn">The transaction data.</param>
	/// <param name="nonce">The nonce value of the transaction.</param>
	/// <returns>The constructed transaction.</returns>
	public async Task<byte[]> GetGuardianApprovalTxn(List<byte[]> txns,
													uint nonce, byte chainId) {
		int totalLength = 0;
		foreach (byte[] txn in txns) {
			totalLength += txn.Length;
		}

		byte[] txnBase = await GetTxnBase(10, nonce, chainId);

		using(MemoryStream stream = new MemoryStream(
				txnBase.Length + (txns.Count * 4) + totalLength)) {
			stream.Write(txnBase);
			foreach (byte[] txn in txns) {
				stream.Write(GetBigEndianBytes(txn.Length));
				stream.Write(txn);
			}
			return stream.ToArray();
		}
	}

	/// <summary>
	/// Constructs a transaction to send data to a virtual machine with an
	/// additional value transfer. This is used for transactions that require both
	/// data delivery and payments to the virtual machine.
	/// </summary>
	/// <param name="vmId">The ID of the virtual machine to which the data and
	/// value will be sent.</param> <param name="value">The amount of value (e.g.,
	/// in tokens or cryptocurrency) to be sent to the virtual machine.</param>
	/// <param name="data">The data in byte array format to be sent to the virtual
	/// machine.</param> <param name="nonce">The nonce value for this transaction,
	/// which helps prevent replay attacks.</param> <param name="chainId">The
	/// identifier of the blockchain on which this transaction will be
	/// executed.</param> <returns>A task that when completed returns a byte array
	/// of the transaction.</returns>

	public async Task<byte[]> GetPayableVmDataTxn(ulong vmId, ulong value,
													byte[] data, uint nonce,
													byte chainId) {
		byte[] txnBase = await GetTxnBase(11, nonce, chainId);

		using(MemoryStream stream = new MemoryStream(txnBase.Length + 16 + data.Length)) {
			stream.Write(txnBase);
			stream.Write(GetBigEndianBytes(vmId));
			stream.Write(GetBigEndianBytes(data.Length));
			stream.Write(data);
			stream.Write(GetBigEndianBytes(value));
			return stream.ToArray();
		}
	}

	/// <summary>
	/// Constructs a transaction to approve a set of transactions via a specified
	/// virtual machine ID (VM ID). This function ensures that each transaction
	/// within the batch is approved for execution by the conduit mechanisms.
	/// </summary>
	/// <param name="vmId">The unique identifier of the virtual machine that
	/// serves as a conduit.</param> <param name="txns">A list of transactions in
	/// byte array format that are to be approved.</param> <param name="nonce">The
	/// nonce value for this transaction to ensure uniqueness and avoid replay
	/// attacks.</param> <param name="chainId">The identifier of the blockchain
	/// where this transaction will be executed.</param> <returns>A task that upon
	/// completion provides the byte array representing the serialized form of the
	/// transaction.</returns
	public async Task<byte[]> GetConduitApprovalTxn(ulong vmId, List<byte[]> txns,
													uint nonce, byte chainId) {
		if (txns.Count == 0) throw new ArgumentException("Txns list is empty.");
		int totalTxnsLength = 0;
		foreach (byte[] txn in txns) {
			totalTxnsLength += txn.Length;
		}
		byte[] txnBase = await GetTxnBase(12, nonce, chainId);
		using MemoryStream stream = new MemoryStream(
			txnBase.Length + 8 + (txns.Count * 4) + totalTxnsLength);
		stream.Write(txnBase);
		stream.Write(GetBigEndianBytes(vmId));
		foreach (byte[] txn in txns) {
			stream.Write(GetBigEndianBytes(txn.Length));
			stream.Write(txn);
		}
		return stream.ToArray();
	}

	/// <summary>
	/// Constructs a transaction to set or replace the conduits associated with a
	/// specific virtual machine. This can change how the virtual machine
	/// processes transactions or interacts with other network components.
	/// </summary>
	/// <param name="vmId">The ID of the virtual machine whose conduits are to be
	/// set.</param> <param name="conduits">A list of byte arrays, each
	/// representing a conduit configuration to be set.</param> <param
	/// name="nonce">The nonce value for this transaction, used to maintain the
	/// order and integrity of transactions.</param> <param name="chainId">The
	/// blockchain identifier where this transaction will occur.</param>
	/// <returns>A task that when resolved returns a byte array of the constructed
	/// transaction.</returns>
	public async Task<byte[]> GetSetConduitsTxn(ulong vmId, List<byte[]> conduits,
												uint nonce, byte chainId) {
		if (conduits.Count == 0)
		throw new ArgumentException("Conduits list is empty.");
		int totalConduitsLenght = 0;

		foreach (byte[] txn in conduits) {
			totalConduitsLenght += txn.Length;
		}

		byte[] txnBase = await GetTxnBase(13, nonce, chainId);
		using MemoryStream stream = new MemoryStream(
			txnBase.Length + 8 + (conduits.Count * 4) + totalConduitsLenght);
		stream.Write(txnBase);
		stream.Write(GetBigEndianBytes(vmId));
		foreach (byte[] conduit in conduits) {
			stream.Write(GetBigEndianBytes(conduit.Length));
			stream.Write(conduit);
		}
		return stream.ToArray();
	}

	/// <summary>
	/// Constructs a transaction to add new conduits to a virtual machine. Adding
	/// conduits can enhance the VM's capabilities or modify its operational
	/// logic.
	/// </summary>
	/// <param name="vmId">The ID of the virtual machine to which conduits will be
	/// added.</param> <param name="conduits">A list of byte arrays, where each
	/// array represents a conduit to be added.</param> <param name="nonce">The
	/// nonce value for this transaction, ensuring transaction uniqueness across
	/// the network.</param> <param name="chainId">The identifier of the chain on
	/// which the transaction will be processed.</param> <returns>A task that
	/// provides the byte array of the serialized transaction upon
	/// completion.</returns>

	public async Task<byte[]> GetAddConduitsTxn(ulong vmId, List<byte[]> conduits,
												uint nonce, byte chainId) {
		if (conduits.Count == 0)
		throw new ArgumentException("Conduits list is empty.");

		byte[] txnBase = await GetTxnBase(14, nonce, chainId);
		using MemoryStream stream =
			new MemoryStream(txnBase.Length + 8 + (conduits.Count * 20));
		stream.Write(txnBase);
		stream.Write(GetBigEndianBytes(vmId));
		foreach (byte[] conduit in conduits) {
			stream.Write(conduit);
		}
		return stream.ToArray();
	}

	/// <summary>
	/// Constructs a transaction to remove one or more conduits from a virtual
	/// machine. This can be used to deactivate certain features or capabilities
	/// of the VM.
	/// </summary>
	/// <param name="vmId">The ID of the virtual machine from which conduits will
	/// be removed.</param> <param name="conduits">A list of byte arrays
	/// representing the conduits to be removed.</param> <param name="nonce">The
	/// nonce value for this transaction, critical for maintaining the integrity
	/// and order of transactions.</param> <param name="chainId">The blockchain
	/// identifier where this transaction will be executed.</param> <returns>A
	/// task that resolves to a byte array representing the serialized
	/// transaction.</returns>

	public async Task<byte[]> GetRemoveConduitsTxn(ulong vmId,
													List<byte[]> conduits,
													uint nonce, byte chainId) {
		if (conduits.Count == 0)
		throw new ArgumentException("Conduits list is empty.");
		int totalConduitsLenght = 0;

		foreach (byte[] txn in conduits) {
			totalConduitsLenght += txn.Length;
		}

		byte[] txnBase = await GetTxnBase(15, nonce, chainId);
		using MemoryStream stream = new MemoryStream(
			txnBase.Length + 8 + (conduits.Count * 4) + totalConduitsLenght);

		stream.Write(txnBase);
		byte[] vmIdArr = BitConverter.GetBytes(vmId);
		Array.Reverse(vmIdArr);
		foreach (byte[] conduit in conduits) {
			stream.Write(GetBigEndianBytes(conduit.Length));
			stream.Write(conduit);
		}
		return stream.ToArray();
	}

	/// <summary>
	/// Constructs a transaction to move stake from one validator to another. This
	/// is typically used in proof of stake networks to redelegate staking tokens
	/// from one node to another without returning to the wallet.
	/// </summary>
	/// <param name="sharesAmount">The amount of stake or shares to be
	/// moved.</param> <param name="fromValidator">The address of the validator
	/// from which the stake will be moved.</param> <param name="toValidator">The
	/// address of the validator to which the stake will be moved.</param> <param
	/// name="nonce">The nonce value for this transaction, ensuring its uniqueness
	/// in the blockchain ledger.</param> <param name="chainId">The blockchain
	/// identifier where this transaction will take place.</param> <returns>A task
	/// that provides the byte array of the serialized transaction once
	/// completed.</returns>

	public async Task<byte[]> GetMoveStakeTxn(ulong sharesAmount,
												String fromValidator,
												String toValidator, uint nonce,
												byte chainId) {
		ValidateAddress(fromValidator);
		ValidateAddress(toValidator);
		
		fromValidator = fromValidator.Substring(2);
		toValidator = toValidator.Substring(2);

		byte[] txnBase = await GetTxnBase((byte) 16, nonce, chainId);
		using MemoryStream stream = new MemoryStream(txnBase.Length + 48);
		stream.Write(txnBase, 0, txnBase.Length);
		stream.Write(GetBigEndianBytes(sharesAmount));
		stream.Write(Extensions.HexStringToByteArray(fromValidator));
		stream.Write(Extensions.HexStringToByteArray(toValidator));

		return stream.ToArray();
	}

	private byte[] GetBigEndianBytes(dynamic value) {
		byte[] bytes;

		if (value is uint) {
			bytes = BitConverter.GetBytes((uint)value); // Convert to uint
		}
		else if (value is ulong) {
			bytes = BitConverter.GetBytes((ulong)value); // Convert to ulong
		}
		else {
			throw new ArgumentException("Unsupported type");
		}

		if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
		return bytes;
	}

	private byte[] GetBigEndianBytes(int value) {
		byte[] bytes = BitConverter.GetBytes(value);
		if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
		return bytes;
	}

	public void ValidateAddress(string address) {
		if (string.IsNullOrEmpty(address)) {
			throw new ArgumentException("Address cannot be null or empty.");
		}
		if (address.Length != 42) {
			throw new ArgumentException("Invalid address format.");
		}

		string pattern = @"^0x[0-9a-fA-F]{40}$";

		if (!Regex.IsMatch(address, pattern)) {
			throw new ArgumentException("Invalid address format.");
		}
	}
}