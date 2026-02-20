namespace Web3DashboardAPI.Domain.Entities;

/// <summary>
/// Represents a blockchain wallet with its address and associated data.
/// </summary>
public class Wallet
{
    /// <summary>
    /// Unique identifier for the wallet record.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Ethereum wallet address (case-insensitive, stored as lowercase).
    /// </summary>
    public string Address { get; set; } = string.Empty;

    /// <summary>
    /// Friendly name for the wallet (optional).
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Last time the wallet data was synced.
    /// </summary>
    public DateTime LastSyncedAt { get; set; }

    /// <summary>
    /// When this wallet record was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Associated ERC20 token balances.
    /// </summary>
    public ICollection<TokenBalance> TokenBalances { get; set; } = new List<TokenBalance>();

    /// <summary>
    /// Associated transaction history.
    /// </summary>
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
