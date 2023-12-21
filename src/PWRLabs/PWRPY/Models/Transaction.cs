namespace PWRPY.Models;

public class Transaction
{
    public int Size { get; }
    public int PositionInTheBlock { get; }
    public decimal Fee { get; }
    public string Type { get; }
    public string FromAddress { get; }
    public string To { get; }
    public string NonceOrValidationHash { get; }
    public string Hash { get; }

    public Transaction(int size, int positionInTheBlock, decimal fee, string type, string fromAddress, string to, string nonceOrValidationHash, string hash)
    {
        Size = size;
        PositionInTheBlock = positionInTheBlock;
        Fee = fee;
        Type = type;
        FromAddress = fromAddress;
        To = to;
        NonceOrValidationHash = nonceOrValidationHash;
        Hash = hash;
    }
}