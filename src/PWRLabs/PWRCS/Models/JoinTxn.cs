using System;
namespace PWRCS.Models;

public class JoinTxn : Transaction
{
    public string Validator {get;}
   
    public JoinTxn(int size, long blockNumber, int positionInTheBlock, long fee, string type, string fromAddress, string to, int nonce, string hash, long timestamp,string validator)
     : base(size, blockNumber, positionInTheBlock, fee, type, fromAddress, to, nonce, hash, timestamp)
    {
        this.Validator = validator;
    }
}