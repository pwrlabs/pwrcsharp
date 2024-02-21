namespace PWRCS.Models;

public class Transaction
{
    public int Size { get; }
    public long BlockNumber {get;}
    public int PositionInTheBlock { get; }
    public decimal Fee { get; }
    public string Type { get; }
    public string FromAddress { get; }
    public string To { get; }
    public string NonceOrValidationHash { get; }
    public string Hash { get; }
    private long TimeStamp {get;}


    public Transaction(int size,
    long blockNumber,
     int positionInTheBlock,
      decimal fee,
       string type,
        string fromAddress,
         string to,
          string nonceOrValidationHash,
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
        NonceOrValidationHash = nonceOrValidationHash;
        Hash = hash;
        TimeStamp = timestamp;
    }
}