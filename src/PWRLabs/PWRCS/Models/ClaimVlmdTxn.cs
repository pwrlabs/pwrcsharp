using System;
namespace PWRCS.Models;

public class ClaimVlmdTxn : Transaction
{
    public long VmId {get;}
    public ClaimVlmdTxn(int size, long blockNumber, int positionInTheBlock, long fee, string type, string fromAddress, string to, int nonce, string hash, long timestamp,long vmId)
     : base(size, blockNumber, positionInTheBlock, fee, type, fromAddress, to, nonce, hash, timestamp)
    {
        this.VmId = vmId;
    }
}