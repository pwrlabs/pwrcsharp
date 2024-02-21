namespace PWRCS.Models;

public class TransferTxn : Transaction
{
      public decimal Value { get; }
    public TransferTxn(int size, long blockNumber, int positionInTheBlock, long fee, string type, string fromAddress, string to, int nonce, string hash, long timestamp) 
    : base(size, blockNumber, positionInTheBlock, fee, type, fromAddress, to, nonce, hash, timestamp)
    {
    }
    
}