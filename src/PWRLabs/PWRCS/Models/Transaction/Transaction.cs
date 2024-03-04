using Newtonsoft.Json;

namespace PWRCS.Models;

public class Transaction
{

    [JsonProperty("size")]
    public uint Size { get; }

    [JsonProperty("blockNumber")]
    public ulong BlockNumber {get;set;}
    [JsonProperty("positionInTheBlock")]

    public uint PositionintheBlock { get; }
    [JsonProperty("fee")]

    public ulong Fee { get; }
    [JsonProperty("type")]

    public string Type { get; }
    [JsonProperty("sender")]

    public string Sender { get; }
    [JsonProperty("receiver")]

    public string Receiver { get; }
    [JsonProperty("nonce")]

    public uint Nonce { get; }
    [JsonProperty("hash")]

    public string Hash { get; }

    [JsonProperty("value")]
    public ulong Value {get;}

    [JsonProperty("timestamp")]
    public ulong TimeStamp {get;set;}

    public Transaction(uint size,
    ulong blockNumber,
     uint positionintheBlock,
      ulong fee,
       string type,
        string sender,
         string receiver,
          uint nonce,
           string hash,
           ulong value,
           ulong timestamp)
    {
        
        Size = size;
        BlockNumber = blockNumber;
        PositionintheBlock = positionintheBlock;
        Fee = fee;
        Type = type;
        Sender = sender;
        Receiver = receiver;
        Nonce = nonce;
        Hash = hash;
        Value = value;
        TimeStamp = timestamp;
    }

    
    public override string ToString()
        {
            return $"Transaction: Size={Size}, BlockNumber={BlockNumber}, PositionintheBlock={PositionintheBlock}, Fee={Fee}, Type={Type}, Sender={Sender}, Receiver={Receiver}, Nonce={Nonce}, Hash={Hash}, Value={Value}, TimeStamp={TimeStamp}";
        }
}