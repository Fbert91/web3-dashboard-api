using Microsoft.AspNetCore.Mvc;
using Web3DashboardAPI.Application.DTOs;
using Web3DashboardAPI.Application.Services;

namespace Web3DashboardAPI.Presentation.Controllers;

/// <summary>
/// Transaction history and details API endpoints
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class TransactionController : ControllerBase
{
    private readonly ITransactionService _transactionService;
    private readonly ILogger<TransactionController> _logger;

    public TransactionController(ITransactionService transactionService, ILogger<TransactionController> logger)
    {
        _transactionService = transactionService;
        _logger = logger;
    }

    /// <summary>
    /// Get transaction history for a wallet
    /// </summary>
    /// <param name="address">Ethereum wallet address</param>
    /// <param name="limit">Number of transactions to return (default: 10)</param>
    [HttpGet("wallet/{address}")]
    public async Task<ActionResult<WalletTransactionsDto>> GetTransactionHistory(string address, int limit = 10)
    {
        _logger.LogInformation("GetTransactionHistory: {Address} (limit: {Limit})", address, limit);

        if (limit < 1 || limit > 100)
            return BadRequest(new { error = "Limit must be between 1 and 100" });

        try
        {
            var result = await _transactionService.GetTransactionHistoryAsync(address, limit);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting transaction history");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get details for a specific transaction
    /// </summary>
    /// <param name="hash">Transaction hash (0x...)</param>
    [HttpGet("{hash}")]
    public async Task<ActionResult<TransactionDto>> GetTransaction(string hash)
    {
        _logger.LogInformation("GetTransaction: {Hash}", hash);

        if (!hash.StartsWith("0x") || hash.Length != 66)
            return BadRequest(new { error = "Invalid transaction hash format" });

        try
        {
            var transaction = await _transactionService.GetTransactionByHashAsync(hash);
            return Ok(transaction);
        }
        catch (InvalidOperationException)
        {
            return NotFound(new { error = "Transaction not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting transaction");
            return BadRequest(new { error = ex.Message });
        }
    }
}
