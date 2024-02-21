namespace PWRCS.Models;

public class Transaction
{
    public int Size { get; }
    public long BlockNumber {get;}
    public int PositionInTheBlock { get; }
    public long Fee { get; }
    public string Type { get; }
    public string FromAddress { get; }
    public string To { get; }
    public int Nonce { get; }
    public string Hash { get; }
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