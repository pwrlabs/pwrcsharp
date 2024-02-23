using System;
namespace PWRCS.Models;

public class JoinTxn : Transaction
{
    public string Validator {get;}
   
    public JoinTxn(uint size, ulong blockNumber, uint positionuintheBlock, ulong fee, string type, string fromAddress, string to, uint nonce, string hash,ulong value,ulong timestamp,string validator)
     : base(size, blockNumber, positionuintheBlock, fee, type, fromAddress, to, nonce, hash,value, timestamp)
    {
        this.Validator = validator;
    }
}