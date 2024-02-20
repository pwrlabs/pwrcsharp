using System;
namespace PWRCS.Models;

public class ClaimVlmdTxn : Transaction
{
    public long VmId {get;}
    public ClaimVlmdTxn(int size, long blockNumber, int positionInTheBlock, decimal fee, string type, string fromAddress, string to, string nonceOrValidationHash, string hash, long timestamp,long vmId)
     : base(size, blockNumber, positionInTheBlock, fee, type, fromAddress, to, nonceOrValidationHash, hash, timestamp)
    {
        this.VmId = vmId;
    }
}