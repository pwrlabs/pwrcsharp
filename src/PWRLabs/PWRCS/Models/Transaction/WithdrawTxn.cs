using System;
using Newtonsoft.Json;
namespace PWRCS.Models;

public class WithdrawTxn : Transaction
{
    [JsonProperty("validator")]
    public string Validator {get;}
    [JsonProperty("shares")]
    public ulong Shares{get;}

    public WithdrawTxn(uint size, ulong blockNumber, uint positionuintheBlock, ulong fee, string type, string fromAddress, string to, uint nonce, string hash,ulong value, ulong timestamp,string validator,ulong shares)
     : base(size, blockNumber, positionuintheBlock, fee, type, fromAddress, to, nonce, hash,value, timestamp)
    {
        this.Validator = validator;
        this.Shares = shares;
    }
}