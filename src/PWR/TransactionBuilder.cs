using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using PWR.Models;
using PWR.Utils;

namespace PWR;

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
	/// Constructs the base of a transaction.
	/// </summary>
	/// <param name="identifier">The identifier for the transaction.</param>
	/// <param name="nonce">The nonce value of the transaction.</param>
	/// <returns>The base of the transaction.</returns>
	public async Task<byte[]> GetTxnBase(uint identifier, uint nonce,
										byte chainId, ulong feePerByte, string address) {
		MemoryStream stream = new MemoryStream(37);

		stream.Write(GetBigEndianBytes(identifier)); // 4 bytes
		stream.WriteByte(chainId); // 1 byte
		stream.Write(GetBigEndianBytes(nonce)); // 4 bytes
		stream.Write(GetBigEndianBytes((ulong)feePerByte)); // 8 bytes
		stream.Write(Extensions.HexStringToByteArray(address)); // 20 bytes

		return stream.ToArray();
	}

	public async Task<byte[]> GetSetPublicKeyTxn(string publicKey,
												uint nonce, byte chainId, ulong feePerByte, string address) {
		byte[] txnBase = await GetTxnBase(1001, nonce, chainId, feePerByte, address);

		byte[] publicKeyBytes = Extensions.HexStringToByteArray(publicKey);

		using (MemoryStream stream = new MemoryStream()) {
			stream.Write(txnBase);
			stream.Write(GetBigEndianBytes((ushort)publicKeyBytes.Length));
			stream.Write(publicKeyBytes);
			return stream.ToArray();
		}
	}

	public async Task<byte[]> GetJoinAsValidatorTxn(string ip,
												uint nonce, byte chainId, ulong feePerByte, string address) {
		byte[] txnBase = await GetTxnBase(1002, nonce, chainId, feePerByte, address);

		byte[] ipBytes = Encoding.UTF8.GetBytes(ip);

		using (MemoryStream stream = new MemoryStream()) {
			stream.Write(txnBase);
			stream.Write(GetBigEndianBytes((ushort)ipBytes.Length));
			stream.Write(ipBytes);
			return stream.ToArray();
		}
	}

	public async Task<byte[]> GetDelegateTxn(string to, ulong amount,
												uint nonce, byte chainId, ulong feePerByte, string address) {
		ValidateAddress(to);

		to = to.Substring(2);
		byte[] txnBase = await GetTxnBase(1003, nonce, chainId, feePerByte, address);
		using(MemoryStream stream = new MemoryStream(txnBase.Length + 8 + 20)) {
			stream.Write(txnBase);
			stream.Write(Extensions.HexStringToByteArray(to));
			stream.Write(GetBigEndianBytes(amount));
			return stream.ToArray();
		}
	}

	public async Task<byte[]> GetChangeIpTxn(string ip,
												uint nonce, byte chainId, ulong feePerByte, string address) {
		byte[] txnBase = await GetTxnBase(1004, nonce, chainId, feePerByte, address);

		byte[] ipBytes = Encoding.UTF8.GetBytes(ip);

		using (MemoryStream stream = new MemoryStream()) {
			stream.Write(txnBase);
			stream.Write(GetBigEndianBytes((ushort)ipBytes.Length));
			stream.Write(ipBytes);
			return stream.ToArray();
		}
	}

	public async Task<byte[]> GetClaimActiveNodeSpotTxn(uint nonce, byte chainId, ulong feePerByte, string address) {
		byte[] txnBase = await GetTxnBase(1005, nonce, chainId, feePerByte, address);

		return txnBase;
	}

	/// <summary>
	/// Constructs a transaction for transferring PWR tokens.
	/// </summary>
	/// <param name="to">The recipient's address.</param>
	/// <param name="amount">The amount of tokens to transfer.</param>
	/// <param name="nonce">The nonce value of the transaction.</param>
	/// <returns>The constructed transaction.</returns>
	public async Task<byte[]> GetTransferPWRTxn(string to, ulong amount,
												uint nonce, byte chainId, ulong feePerByte, string address) {
		ValidateAddress(to);

		to = to.Substring(2);
		byte[] txnBase = await GetTxnBase(1006, nonce, chainId, feePerByte, address);
		using(MemoryStream stream = new MemoryStream(txnBase.Length + 8 + 20)) {
			stream.Write(txnBase);
			stream.Write(Extensions.HexStringToByteArray(to));
			stream.Write(GetBigEndianBytes(amount));
			return stream.ToArray();
		}
	}

	public async Task<byte[]> GetChangeEarlyWithdrawPenaltyProposalTxn(string title, string description, ulong withdrawPenaltyTime, ulong withdrawPenalty,
												uint nonce, byte chainId, ulong feePerByte, string address) {
		byte[] txnBase = await GetTxnBase(1009, nonce, chainId, feePerByte, address);
		byte[] titleBytes = Encoding.UTF8.GetBytes(title);
		byte[] descriptionBytes = Encoding.UTF8.GetBytes(description);

		using (MemoryStream stream = new MemoryStream()) {
			stream.Write(txnBase);
			stream.Write(GetBigEndianBytes((uint)titleBytes.Length));
			stream.Write(titleBytes);
			stream.Write(GetBigEndianBytes(withdrawPenaltyTime));
			stream.Write(GetBigEndianBytes(withdrawPenalty));
			stream.Write(descriptionBytes);
			return stream.ToArray();
		}
	}

	public async Task<byte[]> GetChangeFeePerByteProposalTxn(string title, string description, ulong newFeePerByte,
												uint nonce, byte chainId, ulong feePerByte, string address) {
		byte[] txnBase = await GetTxnBase(1010, nonce, chainId, feePerByte, address);
		byte[] titleBytes = Encoding.UTF8.GetBytes(title);
		byte[] descriptionBytes = Encoding.UTF8.GetBytes(description);

		using (MemoryStream stream = new MemoryStream()) {
			stream.Write(txnBase);
			stream.Write(GetBigEndianBytes((uint)titleBytes.Length));
			stream.Write(titleBytes);
			stream.Write(GetBigEndianBytes(newFeePerByte));
			stream.Write(descriptionBytes);
			return stream.ToArray();
		}
	}

	public async Task<byte[]> GetChangeMaxBlockSizeProposalTxn(string title, string description, ulong maxBlockSize,
												uint nonce, byte chainId, ulong feePerByte, string address) {
		byte[] txnBase = await GetTxnBase(1011, nonce, chainId, feePerByte, address);
		byte[] titleBytes = Encoding.UTF8.GetBytes(title);
		byte[] descriptionBytes = Encoding.UTF8.GetBytes(description);

		using (MemoryStream stream = new MemoryStream()) {
			stream.Write(txnBase);
			stream.Write(GetBigEndianBytes((uint)titleBytes.Length));
			stream.Write(titleBytes);
			stream.Write(GetBigEndianBytes(maxBlockSize));
			stream.Write(descriptionBytes);
			return stream.ToArray();
		}
	}

	public async Task<byte[]> GetChangeMaxTxnSizeProposalTxn(string title, string description, ulong maxTxnSize,
												uint nonce, byte chainId, ulong feePerByte, string address) {
		byte[] txnBase = await GetTxnBase(1012, nonce, chainId, feePerByte, address);
		byte[] titleBytes = Encoding.UTF8.GetBytes(title);
		byte[] descriptionBytes = Encoding.UTF8.GetBytes(description);

		using (MemoryStream stream = new MemoryStream()) {
			stream.Write(txnBase);
			stream.Write(GetBigEndianBytes((uint)titleBytes.Length));
			stream.Write(titleBytes);
			stream.Write(GetBigEndianBytes(maxTxnSize));
			stream.Write(descriptionBytes);
			return stream.ToArray();
		}
	}

	public async Task<byte[]> GetChangeOverallBurnPercentageProposalTxn(string title, string description, ulong burnPercentage,
												uint nonce, byte chainId, ulong feePerByte, string address) {
		byte[] txnBase = await GetTxnBase(1013, nonce, chainId, feePerByte, address);
		byte[] titleBytes = Encoding.UTF8.GetBytes(title);
		byte[] descriptionBytes = Encoding.UTF8.GetBytes(description);

		using (MemoryStream stream = new MemoryStream()) {
			stream.Write(txnBase);
			stream.Write(GetBigEndianBytes((uint)titleBytes.Length));
			stream.Write(titleBytes);
			stream.Write(GetBigEndianBytes(burnPercentage));
			stream.Write(descriptionBytes);
			return stream.ToArray();
		}
	}

	public async Task<byte[]> GetChangeRewardPerYearProposalTxn(string title, string description, ulong rewardPerYear,
												uint nonce, byte chainId, ulong feePerByte, string address) {
		byte[] txnBase = await GetTxnBase(1014, nonce, chainId, feePerByte, address);
		byte[] titleBytes = Encoding.UTF8.GetBytes(title);
		byte[] descriptionBytes = Encoding.UTF8.GetBytes(description);

		using (MemoryStream stream = new MemoryStream()) {
			stream.Write(txnBase);
			stream.Write(GetBigEndianBytes((uint)titleBytes.Length));
			stream.Write(titleBytes);
			stream.Write(GetBigEndianBytes(rewardPerYear));
			stream.Write(descriptionBytes);
			return stream.ToArray();
		}
	}

	public async Task<byte[]> GetChangeValidatorCountLimitProposalTxn(string title, string description, ulong validatorCountLimit,
												uint nonce, byte chainId, ulong feePerByte, string address) {
		byte[] txnBase = await GetTxnBase(1015, nonce, chainId, feePerByte, address);
		byte[] titleBytes = Encoding.UTF8.GetBytes(title);
		byte[] descriptionBytes = Encoding.UTF8.GetBytes(description);

		using (MemoryStream stream = new MemoryStream()) {
			stream.Write(txnBase);
			stream.Write(GetBigEndianBytes((uint)titleBytes.Length));
			stream.Write(titleBytes);
			stream.Write(GetBigEndianBytes(validatorCountLimit));
			stream.Write(descriptionBytes);
			return stream.ToArray();
		}
	}

	public async Task<byte[]> GetChangeValidatorJoiningFeeProposalTxn(string title, string description, ulong joiningFee,
												uint nonce, byte chainId, ulong feePerByte, string address) {
		byte[] txnBase = await GetTxnBase(1016, nonce, chainId, feePerByte, address);
		byte[] titleBytes = Encoding.UTF8.GetBytes(title);
		byte[] descriptionBytes = Encoding.UTF8.GetBytes(description);

		using (MemoryStream stream = new MemoryStream()) {
			stream.Write(txnBase);
			stream.Write(GetBigEndianBytes((uint)titleBytes.Length));
			stream.Write(titleBytes);
			stream.Write(GetBigEndianBytes(joiningFee));
			stream.Write(descriptionBytes);
			return stream.ToArray();
		}
	}

	public async Task<byte[]> GetChangeVidaIdClaimingFeeProposalTxn(string title, string description, ulong claimingFee,
												uint nonce, byte chainId, ulong feePerByte, string address) {
		byte[] txnBase = await GetTxnBase(1017, nonce, chainId, feePerByte, address);
		byte[] titleBytes = Encoding.UTF8.GetBytes(title);
		byte[] descriptionBytes = Encoding.UTF8.GetBytes(description);

		using (MemoryStream stream = new MemoryStream()) {
			stream.Write(txnBase);
			stream.Write(GetBigEndianBytes((uint)titleBytes.Length));
			stream.Write(titleBytes);
			stream.Write(GetBigEndianBytes(claimingFee));
			stream.Write(descriptionBytes);
			return stream.ToArray();
		}
	}

	public async Task<byte[]> GetChangeVidaOwnerTxnFeeShareProposalTxn(string title, string description, ulong feeShare,
												uint nonce, byte chainId, ulong feePerByte, string address) {
		byte[] txnBase = await GetTxnBase(1018, nonce, chainId, feePerByte, address);
		byte[] titleBytes = Encoding.UTF8.GetBytes(title);
		byte[] descriptionBytes = Encoding.UTF8.GetBytes(description);

		using (MemoryStream stream = new MemoryStream()) {
			stream.Write(txnBase);
			stream.Write(GetBigEndianBytes((uint)titleBytes.Length));
			stream.Write(titleBytes);
			stream.Write(GetBigEndianBytes(feeShare));
			stream.Write(descriptionBytes);
			return stream.ToArray();
		}
	}

	public async Task<byte[]> GetOtherProposalTxn(string title, string description,
												uint nonce, byte chainId, ulong feePerByte, string address) {
		byte[] txnBase = await GetTxnBase(1019, nonce, chainId, feePerByte, address);
		byte[] titleBytes = Encoding.UTF8.GetBytes(title);
		byte[] descriptionBytes = Encoding.UTF8.GetBytes(description);

		using (MemoryStream stream = new MemoryStream()) {
			stream.Write(txnBase);
			stream.Write(GetBigEndianBytes((uint)titleBytes.Length));
			stream.Write(titleBytes);
			stream.Write(descriptionBytes);
			return stream.ToArray();
		}
	}

	public async Task<byte[]> GetVoteOnProposalTxn(string proposalHash, byte vote,
												uint nonce, byte chainId, ulong feePerByte, string address) {
		ValidateAddress(proposalHash);
		byte[] txnBase = await GetTxnBase(1020, nonce, chainId, feePerByte, address);
		
		using (MemoryStream stream = new MemoryStream()) {
			stream.Write(txnBase);
			stream.Write(Extensions.HexStringToByteArray(proposalHash.Substring(2)));
			stream.WriteByte(vote);
			return stream.ToArray();
		}
	}

	public async Task<byte[]> GetGuardianApprovalTxn(List<byte[]> transactions,
												uint nonce, byte chainId, ulong feePerByte, string address) {
		byte[] txnBase = await GetTxnBase(1021, nonce, chainId, feePerByte, address);
		
		using (MemoryStream stream = new MemoryStream()) {
			stream.Write(txnBase);
			foreach (var tx in transactions) {
				stream.Write(GetBigEndianBytes((uint)tx.Length));
				stream.Write(tx);
			}
			return stream.ToArray();
		}
	}

	public async Task<byte[]> GetRemoveGuardianTxn(uint nonce, byte chainId, ulong feePerByte, string address) {
		byte[] txnBase = await GetTxnBase(1022, nonce, chainId, feePerByte, address);
		return txnBase;
	}

	public async Task<byte[]> GetSetGuardianTxn(ulong guardianExpiryDate, string guardian,
												uint nonce, byte chainId, ulong feePerByte, string address) {
		ValidateAddress(guardian);
		byte[] txnBase = await GetTxnBase(1023, nonce, chainId, feePerByte, address);
		
		using (MemoryStream stream = new MemoryStream()) {
			stream.Write(txnBase);
			stream.Write(GetBigEndianBytes(guardianExpiryDate));
			stream.Write(Extensions.HexStringToByteArray(guardian.Substring(2)));
			return stream.ToArray();
		}
	}

	public async Task<byte[]> GetMoveStakeTxn(ulong sharesAmount, string fromValidator, string toValidator,
												uint nonce, byte chainId, ulong feePerByte, string address) {
		ValidateAddress(fromValidator);
		ValidateAddress(toValidator);
		byte[] txnBase = await GetTxnBase(1024, nonce, chainId, feePerByte, address);
		
		using (MemoryStream stream = new MemoryStream()) {
			stream.Write(txnBase);
			stream.Write(GetBigEndianBytes(sharesAmount));
			stream.Write(Extensions.HexStringToByteArray(fromValidator.Substring(2)));
			stream.Write(Extensions.HexStringToByteArray(toValidator.Substring(2)));
			return stream.ToArray();
		}
	}

	public async Task<byte[]> GetRemoveValidatorTxn(string validator,
												uint nonce, byte chainId, ulong feePerByte, string address) {
		ValidateAddress(validator);
		byte[] txnBase = await GetTxnBase(1025, nonce, chainId, feePerByte, address);
		
		using (MemoryStream stream = new MemoryStream()) {
			stream.Write(txnBase);
			stream.Write(Extensions.HexStringToByteArray(validator.Substring(2)));
			return stream.ToArray();
		}
	}

	public async Task<byte[]> GetWithdrawTxn(ulong shares, string validator,
												uint nonce, byte chainId, ulong feePerByte, string address) {
		ValidateAddress(validator);
		byte[] txnBase = await GetTxnBase(1026, nonce, chainId, feePerByte, address);
		
		using (MemoryStream stream = new MemoryStream()) {
			stream.Write(txnBase);
			stream.Write(GetBigEndianBytes(shares));
			stream.Write(Extensions.HexStringToByteArray(validator.Substring(2)));
			return stream.ToArray();
		}
	}

	public async Task<byte[]> GetClaimVidaIdTxn(ulong vidaId,
												uint nonce, byte chainId, ulong feePerByte, string address) {
		byte[] txnBase = await GetTxnBase(1028, nonce, chainId, feePerByte, address);
		
		using (MemoryStream stream = new MemoryStream()) {
			stream.Write(txnBase);
			stream.Write(GetBigEndianBytes(vidaId));
			return stream.ToArray();
		}
	}

	public async Task<byte[]> GetConduitApprovalTxn(ulong vidaId, List<byte[]> transactions,
												uint nonce, byte chainId, ulong feePerByte, string address) {
		byte[] txnBase = await GetTxnBase(1029, nonce, chainId, feePerByte, address);
		
		using (MemoryStream stream = new MemoryStream()) {
			stream.Write(txnBase);
			stream.Write(GetBigEndianBytes(vidaId));
			foreach (var tx in transactions) {
				stream.Write(GetBigEndianBytes((uint)tx.Length));
				stream.Write(tx);
			}
			return stream.ToArray();
		}
	}

	public async Task<byte[]> GetPayableVidaDataTxn(ulong vidaId, byte[] data, ulong value,
												uint nonce, byte chainId, ulong feePerByte, string address) {
		byte[] txnBase = await GetTxnBase(1030, nonce, chainId, feePerByte, address);
		
		using (MemoryStream stream = new MemoryStream()) {
			stream.Write(txnBase);
			stream.Write(GetBigEndianBytes(vidaId));
			stream.Write(GetBigEndianBytes((uint)data.Length));
			stream.Write(data);
			stream.Write(GetBigEndianBytes(value));
			return stream.ToArray();
		}
	}

	public async Task<byte[]> GetRemoveConduitsTxn(ulong vidaId, List<string> conduits,
												uint nonce, byte chainId, ulong feePerByte, string address) {
		byte[] txnBase = await GetTxnBase(1031, nonce, chainId, feePerByte, address);
		
		using (MemoryStream stream = new MemoryStream()) {
			stream.Write(txnBase);
			stream.Write(GetBigEndianBytes(vidaId));
			foreach (var conduit in conduits) {
				ValidateAddress(conduit);
				stream.Write(Extensions.HexStringToByteArray(conduit.Substring(2)));
			}
			return stream.ToArray();
		}
	}

	public async Task<byte[]> GetSetConduitModeTxn(ulong vidaId, byte mode, ulong conduitThreshold, 
												List<string> conduits, List<(string, ulong)> conduitsWithVotingPower,
												uint nonce, byte chainId, ulong feePerByte, string address) {
		byte[] txnBase = await GetTxnBase(1033, nonce, chainId, feePerByte, address);
		
		using (MemoryStream stream = new MemoryStream()) {
			stream.Write(txnBase);
			stream.Write(GetBigEndianBytes(vidaId));
			stream.WriteByte(mode);
			stream.Write(GetBigEndianBytes(conduitThreshold));

			if (conduits != null && conduits.Count > 0) {
				stream.Write(GetBigEndianBytes((uint)conduits.Count));
				foreach (var conduit in conduits) {
					ValidateAddress(conduit);
					stream.Write(Extensions.HexStringToByteArray(conduit.Substring(2)));
				}
			} else if (conduitsWithVotingPower != null && conduitsWithVotingPower.Count > 0) {
				stream.Write(GetBigEndianBytes((uint)conduitsWithVotingPower.Count));
				foreach (var (conduit, votingPower) in conduitsWithVotingPower) {
					ValidateAddress(conduit);
					stream.Write(Extensions.HexStringToByteArray(conduit.Substring(2)));
					stream.Write(GetBigEndianBytes(votingPower));
				}
			} else {
				stream.Write(GetBigEndianBytes(0u));
			}
			return stream.ToArray();
		}
	}

	public async Task<byte[]> GetSetVidaPrivateStateTxn(ulong vidaId, bool privateState,
												uint nonce, byte chainId, ulong feePerByte, string address) {
		byte[] txnBase = await GetTxnBase(1034, nonce, chainId, feePerByte, address);
		
		using (MemoryStream stream = new MemoryStream()) {
			stream.Write(txnBase);
			stream.Write(GetBigEndianBytes(vidaId));
			stream.WriteByte((byte)(privateState ? 1 : 0));
			return stream.ToArray();
		}
	}

	public async Task<byte[]> GetSetVidaToAbsolutePublicTxn(ulong vidaId,
												uint nonce, byte chainId, ulong feePerByte, string address) {
		byte[] txnBase = await GetTxnBase(1035, nonce, chainId, feePerByte, address);
		
		using (MemoryStream stream = new MemoryStream()) {
			stream.Write(txnBase);
			stream.Write(GetBigEndianBytes(vidaId));
			return stream.ToArray();
		}
	}

	public async Task<byte[]> GetAddVidaSponsoredAddressesTxn(ulong vidaId, List<string> sponsoredAddresses,
												uint nonce, byte chainId, ulong feePerByte, string address) {
		byte[] txnBase = await GetTxnBase(1036, nonce, chainId, feePerByte, address);
		
		using (MemoryStream stream = new MemoryStream()) {
			stream.Write(txnBase);
			stream.Write(GetBigEndianBytes(vidaId));
			foreach (var sponsoredAddr in sponsoredAddresses) {
				ValidateAddress(sponsoredAddr);
				stream.Write(Extensions.HexStringToByteArray(sponsoredAddr.Substring(2)));
			}
			return stream.ToArray();
		}
	}

	public async Task<byte[]> GetAddVidaAllowedSendersTxn(ulong vidaId, List<string> allowedSenders,
												uint nonce, byte chainId, ulong feePerByte, string address) {
		byte[] txnBase = await GetTxnBase(1037, nonce, chainId, feePerByte, address);
		
		using (MemoryStream stream = new MemoryStream()) {
			stream.Write(txnBase);
			stream.Write(GetBigEndianBytes(vidaId));
			foreach (var senderAddr in allowedSenders) {
				ValidateAddress(senderAddr);
				stream.Write(Extensions.HexStringToByteArray(senderAddr.Substring(2)));
			}
			return stream.ToArray();
		}
	}

	public async Task<byte[]> GetRemoveVidaAllowedSendersTxn(ulong vidaId, List<string> allowedSenders,
												uint nonce, byte chainId, ulong feePerByte, string address) {
		byte[] txnBase = await GetTxnBase(1038, nonce, chainId, feePerByte, address);
		
		using (MemoryStream stream = new MemoryStream()) {
			stream.Write(txnBase);
			stream.Write(GetBigEndianBytes(vidaId));
			foreach (var sender in allowedSenders) {
				ValidateAddress(sender);
				stream.Write(Extensions.HexStringToByteArray(sender.Substring(2)));
			}
			return stream.ToArray();
		}
	}

	public async Task<byte[]> GetRemoveSponsoredAddressesTxn(ulong vidaId, List<string> sponsoredAddresses,
												uint nonce, byte chainId, ulong feePerByte, string address) {
		byte[] txnBase = await GetTxnBase(1039, nonce, chainId, feePerByte, address);
		
		using (MemoryStream stream = new MemoryStream()) {
			stream.Write(txnBase);
			stream.Write(GetBigEndianBytes(vidaId));
			foreach (var sponsoredAddr in sponsoredAddresses) {
				ValidateAddress(sponsoredAddr);
				stream.Write(Extensions.HexStringToByteArray(sponsoredAddr.Substring(2)));
			}
			return stream.ToArray();
		}
	}

	public async Task<byte[]> GetSetPwrTransferRightsTxn(ulong vidaId, bool ownerCanTransferPwr,
												uint nonce, byte chainId, ulong feePerByte, string address) {
		byte[] txnBase = await GetTxnBase(1040, nonce, chainId, feePerByte, address);
		
		using (MemoryStream stream = new MemoryStream()) {
			stream.Write(txnBase);
			stream.Write(GetBigEndianBytes(vidaId));
			stream.WriteByte((byte)(ownerCanTransferPwr ? 1 : 0));
			return stream.ToArray();
		}
	}

	public async Task<byte[]> GetTransferPwrFromVidaTxn(ulong vidaId, string receiver, ulong amount,
												uint nonce, byte chainId, ulong feePerByte, string address) {
		ValidateAddress(receiver);
		byte[] txnBase = await GetTxnBase(1041, nonce, chainId, feePerByte, address);
		
		using (MemoryStream stream = new MemoryStream()) {
			stream.Write(txnBase);
			stream.Write(GetBigEndianBytes(vidaId));
			stream.Write(Extensions.HexStringToByteArray(receiver.Substring(2)));
			stream.Write(GetBigEndianBytes(amount));
			return stream.ToArray();
		}
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
		AddressValidator.ValidateAddress(address);
	}
}