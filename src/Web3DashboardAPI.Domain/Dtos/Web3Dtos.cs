namespace Web3DashboardAPI.Domain.Dtos;

/// <summary>
/// Request to add or validate a wallet.
/// </summary>
public class AddWalletRequest
{
    /// <summary>
    /// Ethereum wallet address.
    /// </summary>
    public string Address { get; set; } = string.Empty;

    /// <summary>
    /// Optional friendly name for the wallet.
    /// </summary>
    public string? Name { get; set; }
}

/// <summary>
/// Response containing wallet information.
/// </summary>
public class WalletResponse
{
    /// <summary>
    /// The wallet address.
    /// </summary>
    public string Address { get; set; } = string.Empty;

    /// <summary>
    /// Friendly name (if set).
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// ETH balance in wei.
    /// </summary>
    public string EthBalance { get; set; } = "0";

    /// <summary>
    /// ETH balance formatted as decimal (in ETH).
    /// </summary>
    public string FormattedEthBalance { get; set; } = "0";

    /// <summary>
    /// Associated token balances.
    /// </summary>
    public List<TokenBalanceResponse> TokenBalances { get; set; } = new();

    /// <summary>
    /// When the wallet data was last synced.
    /// </summary>
    public DateTime LastSyncedAt { get; set; }
}

/// <summary>
/// Response for a token balance.
/// </summary>
public class TokenBalanceResponse
{
    /// <summary>
    /// Token contract address.
    /// </summary>
    public string ContractAddress { get; set; } = string.Empty;

    /// <summary>
    /// Token name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Token symbol.
    /// </summary>
    public string Symbol { get; set; } = string.Empty;

    /// <summary>
    /// Token decimals.
    /// </summary>
    public int Decimals { get; set; }

    /// <summary>
    /// Balance (raw, in smallest unit).
    /// </summary>
    public string Balance { get; set; } = "0";

    /// <summary>
    /// Human-readable balance.
    /// </summary>
    public string FormattedBalance { get; set; } = "0";
}

/// <summary>
/// Response for transaction history.
/// </summary>
public class TransactionResponse
{
    /// <summary>
    /// Transaction hash.
    /// </summary>
    public string TransactionHash { get; set; } = string.Empty;

    /// <summary>
    /// Block number.
    /// </summary>
    public string? BlockNumber { get; set; }

    /// <summary>
    /// From address.
    /// </summary>
    public string From { get; set; } = string.Empty;

    /// <summary>
    /// To address.
    /// </summary>
    public string? To { get; set; }

    /// <summary>
    /// Value transferred (in wei).
    /// </summary>
    public string Value { get; set; } = "0";

    /// <summary>
    /// Value formatted as decimal (in ETH).
    /// </summary>
    public string FormattedValue { get; set; } = "0";

    /// <summary>
    /// Transaction status.
    /// </summary>
    public int? Status { get; set; }

    /// <summary>
    /// When the transaction was mined.
    /// </summary>
    public DateTime? BlockTimestamp { get; set; }

    /// <summary>
    /// Transaction type.
    /// </summary>
    public string TransactionType { get; set; } = "sent";
}
