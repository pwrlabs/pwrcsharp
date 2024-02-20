namespace PWRCS.Models;

public class VmDataTxn : Transaction
{
    public string VmId { get; }
    public string Data { get; }

    public VmDataTxn(string vmId, string data, int size, int positionInTheBlock, decimal fee, string type, string fromAddress, string to, string nonceOrValidationHash, string hash)
        : base(size, positionInTheBlock, fee, type, fromAddress, to, nonceOrValidationHash, hash)
    {
        VmId = vmId;
        Data = data;
    }
}