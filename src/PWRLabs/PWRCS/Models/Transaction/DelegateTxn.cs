using System;
namespace PWRCS.Models;

public class DelegateTxn : Transaction
{
    public string Validator {get;}
    public ulong amount {get;}
    public DelegateTxn(uint size, ulong blockNumber, uint positionuintheBlock, ulong fee, string type, string fromAddress, string to, uint nonce , string hash,ulong value, ulong timestamp,string validator,ulong amount)
     : base(size, blockNumber, positionuintheBlock, fee, type, fromAddress, to, nonce, hash,value, timestamp)
    {
        this.Validator = validator;
        this.amount = amount ;
    }
}