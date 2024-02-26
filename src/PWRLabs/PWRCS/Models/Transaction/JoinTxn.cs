using System;
using Newtonsoft.Json;
namespace PWRCS.Models;

public class JoinTxn : Transaction
{
    [JsonProperty("validator")]
    public string Validator {get;}
   
    public JoinTxn(uint size, ulong blockNumber, uint positionintheBlock, ulong fee, string type, string fromAddress, string to, uint nonce, string hash,ulong value,ulong timestamp,string validator)
     : base(size, blockNumber, positionintheBlock, fee, type, fromAddress, to, nonce, hash,value, timestamp)
    {
        this.Validator = validator;
    }
}