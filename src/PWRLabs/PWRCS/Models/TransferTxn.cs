namespace PWRCS.Models;

public class TransferTxn : Transaction
{
    public decimal Value { get; }

    public TransferTxn(decimal value, int size, int positionInTheBlock, decimal fee, string type, string fromAddress, string to, string nonceOrValidationHash, string hash)
        : base(size, positionInTheBlock, fee, type, fromAddress, to, nonceOrValidationHash, hash)
    {
        Value = value;
    }
}