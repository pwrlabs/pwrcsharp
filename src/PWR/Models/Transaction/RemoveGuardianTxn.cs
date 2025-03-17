using Newtonsoft.Json;

namespace PWR.Models;

public class  RemoveGuardianTxn : Transaction
{
    

    public RemoveGuardianTxn(uint size, ulong blockNumber, uint positionintheBlock, ulong fee, string type, string sender, string receiver, uint nonce, string hash,ulong value, ulong timestamp) 
    : base(size, blockNumber, positionintheBlock, fee, type, sender, receiver, nonce, hash,value, timestamp)
    {
    }

    public override string ToString()
        {
            return $"Transaction: Size={Size}, BlockNumber={BlockNumber}, PositionintheBlock={PositionintheBlock}, Fee={Fee}, Type={Type}, Sender={Sender}, Receiver={Receiver}, Nonce={Nonce}, Hash={Hash}, Value={Value}";
        }
}