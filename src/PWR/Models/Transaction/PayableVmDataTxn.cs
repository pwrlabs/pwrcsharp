using Newtonsoft.Json;

namespace PWR.Models;

public class   PayableVmDataTxn : Transaction
{
    
    [JsonProperty("vmId")]
    public ulong VmId { get; }

    [JsonProperty("data")]
    public string Data { get; }


    public PayableVmDataTxn(uint size, ulong blockNumber, uint positionintheBlock, ulong fee, string type, string sender, string receiver, uint nonce, string hash,ulong value, ulong timestamp,ulong vmId,string data) 
    : base(size, blockNumber, positionintheBlock, fee, type, sender, receiver, nonce, hash,value, timestamp)
    {
        this.VmId = vmId;
        this.Data = data;
    }

    public override string ToString()
        {
            return $"Transaction: Size={Size}, BlockNumber={BlockNumber}, PositionintheBlock={PositionintheBlock}, Fee={Fee}, Type={Type}, Sender={Sender}, Receiver={Receiver}, Nonce={Nonce}, Hash={Hash}, Value={Value}, TimeStamp={TimeStamp}, VmId={VmId}, Data={Data}";
        }
}