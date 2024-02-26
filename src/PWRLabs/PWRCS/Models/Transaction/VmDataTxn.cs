using Newtonsoft.Json;

namespace PWRCS.Models;

public class VmDataTxn : Transaction
{
    
    [JsonProperty("vmId")]
    public ulong VmId { get; }

    [JsonProperty("data")]
    public string Data { get; }
    public VmDataTxn(uint size, ulong blockNumber, uint positionintheBlock, ulong fee, string type, string fromAddress, string to, uint nonce, string hash,ulong value, ulong timestamp,ulong vmId,string data) 
    : base(size, blockNumber, positionintheBlock, fee, type, fromAddress, to, nonce, hash,value, timestamp)
    {
        this.VmId = vmId;
        this.Data = data;
    }

    public VmDataTxn()
    {
    }
}