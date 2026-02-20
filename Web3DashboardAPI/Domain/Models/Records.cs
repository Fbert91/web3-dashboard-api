namespace Web3DashboardAPI.Domain.Models;

/// <summary>
/// Represents a tracked Ethereum wallet
/// </summary>
public class WalletRecord
{
    public int Id { get; set; }
    public string Address { get; set; } = null!;
    public string? Label { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Represents a blockchain transaction
/// </summary>
public class TransactionRecord
{
    public int Id { get; set; }
    public string Hash { get; set; } = null!;
    public string From { get; set; } = null!;
    public string? To { get; set; }
    public decimal Value { get; set; }
    public long GasPrice { get; set; }
    public long Gas { get; set; }
    public string? Status { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Represents an ERC20 token
/// </summary>
public class TokenRecord
{
    public int Id { get; set; }
    public string ContractAddress { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Symbol { get; set; } = null!;
    public int Decimals { get; set; }
}
