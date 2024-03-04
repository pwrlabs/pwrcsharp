using Newtonsoft.Json;

namespace PWRCS.Models;

public class GuardianApprovalTxn : Transaction
{
    
    [JsonProperty("transactions")]
    public List<String> Transactions {get;}

    public GuardianApprovalTxn(uint size, ulong blockNumber, uint positionintheBlock, ulong fee, string type, string sender, string receiver, uint nonce, string hash,ulong value, ulong timestamp,List<String> transactions) 
    : base(size, blockNumber, positionintheBlock, fee, type, sender, receiver, nonce, hash,value, timestamp)
    {
        this.Transactions = transactions;
    }

    public override string ToString()
    {
        string txnInfo = "";
        foreach(string txn in Transactions){
            txnInfo += txn;
            txnInfo += Environment.NewLine;
        }
        return $"Transaction: Size={Size}, BlockNumber={BlockNumber}, PositionintheBlock={PositionintheBlock}, Fee={Fee}, Type={Type}, Sender={Sender}, Receiver={Receiver}, Nonce={Nonce}, Hash={Hash}, Value={Value}, TimeStamp={TimeStamp}, Transactions={txnInfo}";
    }
}