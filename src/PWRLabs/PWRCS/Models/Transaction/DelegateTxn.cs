using System;
using Newtonsoft.Json;
namespace PWRCS.Models;

public class DelegateTxn : Transaction
{
    [JsonProperty("validator")]
    public string Validator {get;}
    [JsonProperty("amount")]
    public ulong amount {get;}
    public DelegateTxn(uint size, ulong blockNumber, uint positionintheBlock, ulong fee, string type, string fromAddress, string to, uint nonce , string hash,ulong value, ulong timestamp,string validator,ulong amount)
     : base(size, blockNumber, positionintheBlock, fee, type, fromAddress, to, nonce, hash,value, timestamp)
    {
        this.Validator = validator;
        this.amount = amount ;
    }
}