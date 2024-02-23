using System;
namespace PWRCS.Models;

public class ClaimVlmdTxn : Transaction
{
    public ulong VmId {get;}
    public ClaimVlmdTxn(uint size, ulong blockNumber, uint positionuintheBlock, ulong fee, string type, string fromAddress, string to, uint nonce, string hash,ulong value, ulong timestamp,ulong vmId)
     : base(size, blockNumber, positionuintheBlock, fee, type, fromAddress, to, nonce, hash,value, timestamp)
    {
        this.VmId = vmId;
    }
}