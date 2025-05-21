namespace PWR.Models;

public class WalletResponse
{
    public bool Success { get; }
    public string? Hash { get; }
    public string? Error { get; }

    public WalletResponse(bool success, string? txnHash, string? error = null)
    {
        Success = success;
        Hash = txnHash;
        Error = error;
    }
}