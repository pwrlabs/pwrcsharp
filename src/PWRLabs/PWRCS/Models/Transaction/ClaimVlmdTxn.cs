using System;
using Newtonsoft.Json;
namespace PWRCS.Models;

public class ClaimVlmdTxn : Transaction
{
    [JsonProperty("vmId")]
    public ulong VmId {get;}
    public ClaimVlmdTxn(uint size, ulong blockNumber, uint positionintheBlock, ulong fee, string type, string fromAddress, string to, uint nonce, string hash,ulong value, ulong timestamp,ulong vmId)
     : base(size, blockNumber, positionintheBlock, fee, type, fromAddress, to, nonce, hash,value, timestamp)
    {
        this.VmId = vmId;
    }
}