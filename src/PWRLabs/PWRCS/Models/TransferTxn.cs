namespace PWRCS.Models;

public class TransferTxn : Transaction
{
      public decimal Value { get; }
    public TransferTxn(int size, long blockNumber, int positionInTheBlock, decimal fee, string type, string fromAddress, string to, string nonceOrValidationHash, string hash, long timestamp) 
    : base(size, blockNumber, positionInTheBlock, fee, type, fromAddress, to, nonceOrValidationHash, hash, timestamp)
    {
    }
    
}