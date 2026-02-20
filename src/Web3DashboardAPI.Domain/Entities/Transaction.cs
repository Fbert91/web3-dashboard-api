namespace Web3DashboardAPI.Domain.Entities;

/// <summary>
/// Represents a blockchain transaction.
/// </summary>
public class Transaction
{
    /// <summary>
    /// Unique identifier for this transaction record.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Foreign key to the wallet.
    /// </summary>
    public int WalletId { get; set; }

    /// <summary>
    /// The wallet this transaction belongs to.
    /// </summary>
    public Wallet Wallet { get; set; } = null!;

    /// <summary>
    /// Transaction hash on the blockchain.
    /// </summary>
    public string TransactionHash { get; set; } = string.Empty;

    /// <summary>
    /// Block number where the transaction was mined.
    /// </summary>
    public string? BlockNumber { get; set; }

    /// <summary>
    /// Sender address.
    /// </summary>
    public string From { get; set; } = string.Empty;

    /// <summary>
    /// Recipient address (null for contract creation).
    /// </summary>
    public string? To { get; set; }

    /// <summary>
    /// Transaction value in wei.
    /// </summary>
    public string Value { get; set; } = "0";

    /// <summary>
    /// Gas used by the transaction.
    /// </summary>
    public string? Gas { get; set; }

    /// <summary>
    /// Gas price in wei.
    /// </summary>
    public string? GasPrice { get; set; }

    /// <summary>
    /// Input data (function call data or contract creation bytecode).
    /// </summary>
    public string? Input { get; set; }

    /// <summary>
    /// Transaction status: 1 = success, 0 = failed.
    /// </summary>
    public int? Status { get; set; }

    /// <summary>
    /// When the transaction was mined (blockchain timestamp).
    /// </summary>
    public DateTime? BlockTimestamp { get; set; }

    /// <summary>
    /// When this transaction record was synced to the database.
    /// </summary>
    public DateTime SyncedAt { get; set; }

    /// <summary>
    /// Transaction type: "sent", "received", "contract_interaction".
    /// </summary>
    public string TransactionType { get; set; } = "sent";
}
