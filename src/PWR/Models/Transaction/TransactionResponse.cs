using Newtonsoft.Json;

namespace PWR.Models;

public class TransactionResponse
{
    [JsonProperty("identifier")]
    public uint Identifier { get; }

    [JsonProperty("paidTotalFee")]
    public ulong PaidTotalFee { get; }

    [JsonProperty("amount")]
    public ulong Amount { get; }

    [JsonProperty("paidActionFee")]
    public ulong PaidActionFee { get; }

    [JsonProperty("nonce")]
    public uint Nonce { get; }

    [JsonProperty("transactionHash")]
    public string TransactionHash { get; }

    [JsonProperty("timeStamp")]
    public ulong TimeStamp { get; }

    [JsonProperty("feePerByte")]
    public ulong FeePerByte { get; }

    [JsonProperty("size")]
    public uint Size { get; }

    [JsonProperty("sender")]
    public string Sender { get; }

    [JsonProperty("success")]
    public bool Success { get; }

    [JsonProperty("blockNumber")]
    public uint BlockNumber { get; }

    [JsonProperty("positionInBlock")]
    public uint PositionInTheBlock { get; }

    [JsonProperty("vidaId")]
    public ulong VidaId { get; }

    [JsonProperty("receiver")]
    public string Receiver { get; }

    [JsonProperty("data")]
    public byte[] Data { get; }

    [JsonProperty("type")]
    public string Type { get; }

    [JsonProperty("signature")]
    public string Signature { get; }

    [JsonProperty("publicKey")]
    public string PublicKey { get; }

    [JsonProperty("guardian")]
    public string Guardian { get; }

    [JsonProperty("guardianSignature")]
    public string GuardianSignature { get; }

    [JsonProperty("guardianPublicKey")]
    public string GuardianPublicKey { get; }

    [JsonProperty("guardianNonce")]
    public uint GuardianNonce { get; }

    [JsonProperty("guardianTimeStamp")]
    public ulong GuardianTimeStamp { get; }

    [JsonProperty("guardianTransactionHash")]
    public string GuardianTransactionHash { get; }

    [JsonProperty("guardianFeePerByte")]
    public ulong GuardianFeePerByte { get; }

    [JsonProperty("guardianSize")]
    public uint GuardianSize { get; }

    [JsonProperty("guardianSender")]
    public string GuardianSender { get; }

    [JsonProperty("guardianSuccess")]
    public bool GuardianSuccess { get; }

    [JsonProperty("guardianBlockNumber")]
    public uint GuardianBlockNumber { get; }

    [JsonProperty("guardianPositionInTheBlock")]
    public uint GuardianPositionInTheBlock { get; }

    [JsonProperty("guardianVidaId")]
    public ulong GuardianVidaId { get; }

    [JsonProperty("guardianReceiver")]
    public string GuardianReceiver { get; }

    [JsonProperty("guardianData")]
    public byte[] GuardianData { get; }

    public TransactionResponse(
        uint identifier,
        ulong paidTotalFee,
        ulong amount,
        ulong paidActionFee,
        uint nonce,
        string transactionHash,
        ulong timeStamp,
        ulong feePerByte,
        uint size,
        string sender,
        bool success,
        uint blockNumber,
        uint positionInTheBlock,
        ulong vidaId,
        string receiver,
        byte[] data,
        string type = "",
        string signature = "",
        string publicKey = "",
        string guardian = "",
        string guardianSignature = "",
        string guardianPublicKey = "",
        uint guardianNonce = 0,
        ulong guardianTimeStamp = 0,
        string guardianTransactionHash = "",
        ulong guardianFeePerByte = 0,
        uint guardianSize = 0,
        string guardianSender = "",
        bool guardianSuccess = false,
        uint guardianBlockNumber = 0,
        uint guardianPositionInTheBlock = 0,
        ulong guardianVidaId = 0,
        string guardianReceiver = "",
        byte[]? guardianData = null)
    {
        Identifier = identifier;
        PaidTotalFee = paidTotalFee;
        Amount = amount;
        PaidActionFee = paidActionFee;
        Nonce = nonce;
        TransactionHash = transactionHash;
        TimeStamp = timeStamp;
        FeePerByte = feePerByte;
        Size = size;
        Sender = sender;
        Success = success;
        BlockNumber = blockNumber;
        PositionInTheBlock = positionInTheBlock;
        VidaId = vidaId;
        Receiver = receiver;
        Data = data;
        Type = type;
        Signature = signature;
        PublicKey = publicKey;
        Guardian = guardian;
        GuardianSignature = guardianSignature;
        GuardianPublicKey = guardianPublicKey;
        GuardianNonce = guardianNonce;
        GuardianTimeStamp = guardianTimeStamp;
        GuardianTransactionHash = guardianTransactionHash;
        GuardianFeePerByte = guardianFeePerByte;
        GuardianSize = guardianSize;
        GuardianSender = guardianSender;
        GuardianSuccess = guardianSuccess;
        GuardianBlockNumber = guardianBlockNumber;
        GuardianPositionInTheBlock = guardianPositionInTheBlock;
        GuardianVidaId = guardianVidaId;
        GuardianReceiver = guardianReceiver;
        GuardianData = guardianData ?? Array.Empty<byte>();
    }

    public override string ToString()
    {
        var dataStr = Data.Length > 0 ? Convert.ToBase64String(Data) : "";
        return $"Transaction: Hash={TransactionHash}, BlockNumber={BlockNumber}, PositionInBlock={PositionInTheBlock}, " +
               $"Sender={Sender}, Receiver={Receiver}, Amount={Amount}, Fee={PaidTotalFee}, " +
               $"Success={Success}, TimeStamp={TimeStamp}, Type={Type}, VidaId={VidaId}, " +
               $"Data={dataStr}";
    }
} 