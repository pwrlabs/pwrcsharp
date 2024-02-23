using System;
namespace PWRCS.Models;

public class WithdrawTxn : Transaction
{
    public string Validator {get;}
    public ulong Shares{get;}

    public WithdrawTxn(uint size, ulong blockNumber, uint positionuintheBlock, ulong fee, string type, string fromAddress, string to, uint nonce, string hash,ulong value, ulong timestamp,string validator,ulong shares)
     : base(size, blockNumber, positionuintheBlock, fee, type, fromAddress, to, nonce, hash,value, timestamp)
    {
        this.Validator = validator;
        this.Shares = shares;
    }
}