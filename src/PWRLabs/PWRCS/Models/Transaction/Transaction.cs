using Newtonsoft.Json;

namespace PWRCS.Models;

public class Transaction
{

    [JsonProperty("size")]
    public uint Size { get; }

    [JsonProperty("blockNumber")]
    public ulong BlockNumber {get;}
    [JsonProperty("positionuintheBlock")]

    public uint PositionuintheBlock { get; }
    [JsonProperty("fee")]

    public ulong Fee { get; }
    [JsonProperty("type")]

    public string Type { get; }
    [JsonProperty("sender")]

    public string FromAddress { get; }
    [JsonProperty("to")]

    public string To { get; }
    [JsonProperty("nonce")]

    public uint Nonce { get; }
    [JsonProperty("hash")]

    public string Hash { get; }

    [JsonProperty("value")]
    public ulong Value {get;}

    [JsonProperty("timestamp")]
    public ulong TimeStamp {get;}

    



    public Transaction(uint size,
    ulong blockNumber,
     uint positionuintheBlock,
      ulong fee,
       string type,
        string fromAddress,
         string to,
          uint nonce,
           string hash,
           ulong value,
           ulong timestamp)
    {
        
        Size = size;
        BlockNumber = blockNumber;
        PositionuintheBlock = positionuintheBlock;
        Fee = fee;
        Type = type;
        FromAddress = fromAddress;
        To = to;
        Nonce = nonce;
        Hash = hash;
        Value = value;
        TimeStamp = timestamp;
    }

    public override string ToString()
        {
            return $"Transaction: Size={Size}, BlockNumber={BlockNumber}, PositionuintheBlock={PositionuintheBlock}, Fee={Fee}, Type={Type}, FromAddress={FromAddress}, To={To}, Nonce={Nonce}, Hash={Hash}, Value={Value}, TimeStamp={TimeStamp}";
        }
}