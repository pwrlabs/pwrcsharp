using Newtonsoft.Json;

namespace PWRCS.Models;

public class Transaction
{

    [JsonProperty("size")]
    public int Size { get; }

    [JsonProperty("blockNumber")]
    public long BlockNumber {get;}
    [JsonProperty("positionInTheBlock")]

    public int PositionInTheBlock { get; }
    [JsonProperty("fee")]

    public long Fee { get; }
    [JsonProperty("type")]

    public string Type { get; }
    [JsonProperty("sender")]

    public string FromAddress { get; }
    [JsonProperty("to")]

    public string To { get; }
    [JsonProperty("nonce")]

    public int Nonce { get; }
    [JsonProperty("hash")]

    public string Hash { get; }
    [JsonProperty("timestamp")]

    private long TimeStamp {get;}


   


    public Transaction(int size,
    long blockNumber,
     int positionInTheBlock,
      long fee,
       string type,
        string fromAddress,
         string to,
          int nonce,
           string hash,
           long timestamp)
    {
        
        Size = size;
        BlockNumber = blockNumber;
        PositionInTheBlock = positionInTheBlock;
        Fee = fee;
        Type = type;
        FromAddress = fromAddress;
        To = to;
        Nonce = nonce;
        Hash = hash;
        TimeStamp = timestamp;
    }
}