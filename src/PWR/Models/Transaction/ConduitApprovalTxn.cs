using Newtonsoft.Json;

namespace PWR.Models;

public class ConduitApprovalTxn : Transaction
{
    
    [JsonProperty("vidaId")]
    public ulong VidaId { get; }

    [JsonProperty("transactions")]
    public List<string> Transactions {get;}

    
    public ConduitApprovalTxn(uint size, ulong blockNumber, uint positionintheBlock, ulong fee, string type, string sender, string receiver, uint nonce, string hash,ulong value, ulong timestamp,ulong vidaId,List<string> transactions) 
    : base(size, blockNumber, positionintheBlock, fee, type, sender, receiver, nonce, hash,value, timestamp)
    {
        this.VidaId = vidaId;
        this.Transactions = transactions;
    }

    public override string ToString()
    {
        string txnInfo = "";
        foreach(string txn in Transactions){
            txnInfo += txn;
            txnInfo += Environment.NewLine;
        }
        return $"Transaction: Size={Size}, BlockNumber={BlockNumber}, PositionintheBlock={PositionintheBlock}, Fee={Fee}, Type={Type}, Sender={Sender}, Receiver={Receiver}, Nonce={Nonce}, Hash={Hash}, Value={Value}, TimeStamp={TimeStamp}, VidaId={VidaId}, Transactions={txnInfo}";
    }
}