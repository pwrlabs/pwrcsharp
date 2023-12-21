namespace PWRPY.Models;

public class WalletResponse
{
    public bool Success { get; }
    public string? TxnHash { get; }
    public string? Error { get; }

    public WalletResponse(bool success, string? txnHash, string? error = null)
    {
        Success = success;
        TxnHash = txnHash;
        Error = error;
    }
}