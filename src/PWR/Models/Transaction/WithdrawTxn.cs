using System;
using Newtonsoft.Json;
namespace PWR.Models;

public class WithdrawTxn : Transaction
{
    [JsonProperty("validator")]
    public string Validator {get;}
    [JsonProperty("shares")]
    public ulong Shares{get;}

    public WithdrawTxn(uint size, ulong blockNumber, uint positionuintheBlock, ulong fee, string type, string sender, string receiver, uint nonce, string hash,ulong value, ulong timestamp,string validator,ulong shares)
     : base(size, blockNumber, positionuintheBlock, fee, type, sender, receiver, nonce, hash,value, timestamp)
    {
        this.Validator = validator;
        this.Shares = shares;
    }
    public override string ToString()
        {
            return $"Transaction: Size={Size}, BlockNumber={BlockNumber}, PositionintheBlock={PositionintheBlock}, Fee={Fee}, Type={Type}, Sender={Sender}, Receiver={Receiver}, Nonce={Nonce}, Hash={Hash}, Value={Value}, TimeStamp={TimeStamp}, Validator={Validator}, Shares={Shares}";
        }
}