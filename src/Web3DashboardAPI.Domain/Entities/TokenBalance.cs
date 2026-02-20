namespace Web3DashboardAPI.Domain.Entities;

/// <summary>
/// Represents an ERC20 token balance for a wallet.
/// </summary>
public class TokenBalance
{
    /// <summary>
    /// Unique identifier for this token balance record.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Foreign key to the wallet.
    /// </summary>
    public int WalletId { get; set; }

    /// <summary>
    /// The wallet this token balance belongs to.
    /// </summary>
    public Wallet Wallet { get; set; } = null!;

    /// <summary>
    /// Smart contract address of the ERC20 token.
    /// </summary>
    public string ContractAddress { get; set; } = string.Empty;

    /// <summary>
    /// Token name (e.g., "Uniswap").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Token symbol (e.g., "UNI").
    /// </summary>
    public string Symbol { get; set; } = string.Empty;

    /// <summary>
    /// Token decimal places (typically 18 for ERC20).
    /// </summary>
    public int Decimals { get; set; }

    /// <summary>
    /// Balance in the smallest unit (wei-like).
    /// </summary>
    public string Balance { get; set; } = "0";

    /// <summary>
    /// Balance formatted as human-readable decimal string.
    /// </summary>
    public string FormattedBalance { get; set; } = "0";

    /// <summary>
    /// When this balance was last updated.
    /// </summary>
    public DateTime LastUpdatedAt { get; set; }
}
