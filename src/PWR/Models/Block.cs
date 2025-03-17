using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
namespace PWR.Models;

public class Block
{
    [JsonProperty("transactionCount")]
    public uint TransactionCount { get; }
    [JsonProperty("blockSize")]
    public uint Size { get; }
    [JsonProperty("blockNumber")]
    public ulong Number { get; }
    [JsonProperty("blockReward")]
    public ulong Reward { get; }
    [JsonProperty("timestamp")]
    public ulong Timestamp { get; }
    [JsonProperty("blockHash")]
    public string Hash { get; }
    [JsonProperty("blockSubmitter")]
    public string Submitter { get; }
    [JsonProperty("success")]
    public bool Success { get; }
    [JsonProperty("transactions")]    
    [JsonConverter(typeof(TransactionConverter))]
    public List<Transaction> Transactions { get; }

    public Block(uint transactionCount, uint size, uint number, ulong reward, ulong timestamp, string hash, string submitter, bool success, List<Transaction> transactions)
    {
        TransactionCount = transactionCount;
        Size = size;
        Number = number;
        Reward = reward;
        Timestamp = timestamp;
        Hash = hash;
        Submitter = submitter;
        Success = success;
        Transactions = transactions;
        foreach(var txn in transactions){
            txn.TimeStamp = timestamp;
            txn.BlockNumber = number;
        }
    }

    public override string ToString()
        {
            string txnInfo = "";
            foreach(Transaction tx in Transactions){
                txnInfo += tx.ToString();
                txnInfo += Environment.NewLine;
            }
           
            return $"Block: Number={Number}, Hash={Hash}, Size={Size}, Reward={Reward}, Timestamp={Timestamp}, Submitter={Submitter}, Success={Success}, TransactionCount={TransactionCount}{Environment.NewLine}Transactions:{Environment.NewLine}{txnInfo}";
        }


}