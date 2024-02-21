using System;
namespace PWRCS.Models;

public class DelegateTxn : Transaction
{
    public string Validator {get;}
    public long amount {get;}
    public DelegateTxn(int size, long blockNumber, int positionInTheBlock, long fee, string type, string fromAddress, string to, int nonce , string hash, long timestamp,string validator,long amount)
     : base(size, blockNumber, positionInTheBlock, fee, type, fromAddress, to, nonce, hash, timestamp)
    {
        this.Validator = validator;
        this.amount = amount ;
    }
}