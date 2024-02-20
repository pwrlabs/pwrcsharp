using System;
namespace PWRCS.Models;

public class JoinTxn : Transaction
{
    public string Validator {get;}
   
    public JoinTxn(int size, long blockNumber, int positionInTheBlock, decimal fee, string type, string fromAddress, string to, string nonceOrValidationHash, string hash, long timestamp,string validator)
     : base(size, blockNumber, positionInTheBlock, fee, type, fromAddress, to, nonceOrValidationHash, hash, timestamp)
    {
        this.Validator = validator;
    }
}