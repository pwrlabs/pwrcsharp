namespace PWRPY.Models;

public class Block
{
    public int TransactionCount { get; }
    public int Size { get; }
    public int Number { get; }
    public decimal Reward { get; }
    public long Timestamp { get; }
    public string Hash { get; }
    public string Submitter { get; }
    public bool Success { get; }
    public List<Transaction> Transactions { get; }

    public Block(int transactionCount, int size, int number, decimal reward, long timestamp, string hash, string submitter, bool success, List<Transaction> transactions)
    {
        TransactionCount = transactionCount;
        Size = size;
        Number = number;
        Reward = reward;
        Timestamp = timestamp;
        Hash = hash;
        Submitter = submitter;
        Success = success;
        Transactions = transactions;
    }
}