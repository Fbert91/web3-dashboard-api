namespace Web3DashboardAPI.Application.DTOs;

/// <summary>
/// DTO for wallet balance information
/// </summary>
public class WalletBalanceDto
{
    public string Address { get; set; } = null!;
    public string EthBalance { get; set; } = null!;
    public List<TokenBalanceDto> Tokens { get; set; } = new();
}

/// <summary>
/// DTO for individual token balance
/// </summary>
public class TokenBalanceDto
{
    public string ContractAddress { get; set; } = null!;
    public string Symbol { get; set; } = null!;
    public string Balance { get; set; } = null!;
    public int Decimals { get; set; }
    public string FormattedBalance { get; set; } = null!;
}

/// <summary>
/// DTO for transaction details
/// </summary>
public class TransactionDto
{
    public string Hash { get; set; } = null!;
    public string From { get; set; } = null!;
    public string? To { get; set; }
    public string Value { get; set; } = null!;
    public string GasPrice { get; set; } = null!;
    public string Gas { get; set; } = null!;
    public string? Status { get; set; }
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// DTO for wallet transactions
/// </summary>
public class WalletTransactionsDto
{
    public string Address { get; set; } = null!;
    public List<TransactionDto> Transactions { get; set; } = new();
}

/// <summary>
/// DTO for wallet validation response
/// </summary>
public class WalletValidationDto
{
    public string Address { get; set; } = null!;
    public bool IsValid { get; set; }
    public string? Message { get; set; }
}

/// <summary>
/// DTO for smart contract call result
/// </summary>
public class ContractCallResultDto
{
    public bool Success { get; set; }
    public string? Result { get; set; }
    public string? Error { get; set; }
}
