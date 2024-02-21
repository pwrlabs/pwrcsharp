using Newtonsoft.Json;

namespace PWRCS.Models;

public class VmDataTxn : Transaction
{
    
    [JsonProperty("vmId")]
    public long VmId { get; }

    [JsonProperty("data")]
    public string Data { get; }
    public VmDataTxn(int size, long blockNumber, int positionInTheBlock, long fee, string type, string fromAddress, string to, int nonce, string hash, long timestamp,long vmId,string data) 
    : base(size, blockNumber, positionInTheBlock, fee, type, fromAddress, to, nonce, hash, timestamp)
    {
        this.VmId = vmId;
        this.Data = data;
    }


   
}