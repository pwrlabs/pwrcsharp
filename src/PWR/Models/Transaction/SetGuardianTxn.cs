using Newtonsoft.Json;

namespace PWR.Models;

public class SetGuardianTxn : Transaction
{
    
    [JsonProperty("guardian")]
    public string Guardian {get;}
    [JsonProperty("expiryDate")]

    public ulong ExpiryDate {get;}
    public SetGuardianTxn(uint size, ulong blockNumber, uint positionintheBlock, ulong fee, string type, string sender, string receiver, uint nonce, string hash,ulong value, ulong timestamp,ulong expiryDate,string guardian) 
    : base(size, blockNumber, positionintheBlock, fee, type, sender, receiver, nonce, hash,value, timestamp)
    {
       Guardian = guardian;
       ExpiryDate = expiryDate;
    }
    public override string ToString()
        {
            return $"Transaction: Size={Size}, BlockNumber={BlockNumber}, PositionintheBlock={PositionintheBlock}, Fee={Fee}, Type={Type}, Sender={Sender}, Receiver={Receiver}, Nonce={Nonce}, Hash={Hash}, Value={Value}, ExpiryDate={ExpiryDate}, Guardian={Guardian}";
        }
}