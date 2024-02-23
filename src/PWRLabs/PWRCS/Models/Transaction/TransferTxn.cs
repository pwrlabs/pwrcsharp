namespace PWRCS.Models;

public class TransferTxn : Transaction
{
    public TransferTxn(uint size, ulong blockNumber, uint positionuintheBlock, ulong fee, string type, string fromAddress, string to, uint nonce, string hash,ulong value, ulong timestamp) 
    : base(size, blockNumber, positionuintheBlock, fee, type, fromAddress, to, nonce, hash,value, timestamp)
    {
    }
    
}