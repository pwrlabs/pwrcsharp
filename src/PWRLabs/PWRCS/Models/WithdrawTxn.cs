using System;
namespace PWRCS.Models;

public class WithdrawTxn : Transaction
{
    public string Validator {get;}
    public long Shares{get;}

    public WithdrawTxn(int size, long blockNumber, int positionInTheBlock, long fee, string type, string fromAddress, string to, int nonce, string hash, long timestamp,string validator,long shares)
     : base(size, blockNumber, positionInTheBlock, fee, type, fromAddress, to, nonce, hash, timestamp)
    {
        this.Validator = validator;
        this.Shares = shares;
    }
}