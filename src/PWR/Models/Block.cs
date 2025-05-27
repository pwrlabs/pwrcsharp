using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
namespace PWR.Models;

public class BlockTransaction{
    [JsonProperty("identifier")]
    public uint Identifier { get; }
    [JsonProperty("transactionHash")]
    public string TransactionHash { get; }

    public BlockTransaction(uint identifier, string transactionHash)
    {
        Identifier = identifier;
        TransactionHash = transactionHash;
    }

    public override string ToString()
    {
        return $"Transaction: Identifier={Identifier}, Hash={TransactionHash}";
    }
}

public class Block
{
    [JsonProperty("processedWithoutCriticalErrors")]
    public bool ProcessedWithoutCriticalErrors { get; }
    [JsonProperty("timeStamp")]
    public ulong TimeStamp { get; }
    [JsonProperty("blockHash")]
    public string BlockHash { get; }
    [JsonProperty("previousBlockHash")]
    public string PreviousBlockHash { get; }
    [JsonProperty("size")]
    public uint Size { get; }
    [JsonProperty("proposer")]
    public string Proposer { get; }
    [JsonProperty("blockNumber")]
    public ulong BlockNumber { get; }
    [JsonProperty("burnedFees")]
    public ulong BurnedFees { get; }
    [JsonProperty("rootHash")]
    public string RootHash { get; }
    [JsonProperty("blockReward")]
    public ulong BlockReward { get; }
    [JsonProperty("transactions")]
    public List<BlockTransaction> Transactions { get; }
    [JsonProperty("newSharesPerSpark")]
    public ulong NewSharesPerSpark { get; }

    public Block(bool processedWithoutCriticalErrors, ulong timeStamp, string blockHash, string previousBlockHash, uint size, string proposer, ulong blockNumber, ulong burnedFees, string rootHash, ulong blockReward, List<BlockTransaction> transactions, ulong newSharesPerSpark)
    {
        ProcessedWithoutCriticalErrors = processedWithoutCriticalErrors;
        TimeStamp = timeStamp;
        BlockHash = blockHash;
        PreviousBlockHash = previousBlockHash;
        Size = size;
        Proposer = proposer;
        BlockNumber = blockNumber;
        BurnedFees = burnedFees;
        RootHash = rootHash;
        BlockReward = blockReward;
        Transactions = transactions;
        NewSharesPerSpark = newSharesPerSpark;
    }

    public override string ToString()
    {
        string txnInfo = "";
        foreach(BlockTransaction tx in Transactions){
            txnInfo += tx.ToString();
            txnInfo += Environment.NewLine;
        }
       
        return $"Block: Number={BlockNumber}, Hash={BlockHash}, PrevHash={PreviousBlockHash}, Size={Size}, Reward={BlockReward}, BurnedFees={BurnedFees}, TimeStamp={TimeStamp}, Proposer={Proposer}, Success={ProcessedWithoutCriticalErrors}, RootHash={RootHash}, NewSharesPerSpark={NewSharesPerSpark}, TransactionCount={Transactions.Count}{Environment.NewLine}Transactions:{Environment.NewLine}{txnInfo}";
    }
}