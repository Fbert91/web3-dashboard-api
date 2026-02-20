using Web3DashboardAPI.Application.DTOs;
using Web3DashboardAPI.Infrastructure.Persistence;

namespace Web3DashboardAPI.Application.Services;

/// <summary>
/// Transaction history and details service
/// </summary>
public interface ITransactionService
{
    Task<WalletTransactionsDto> GetTransactionHistoryAsync(string walletAddress, int limit = 10);
    Task<TransactionDto> GetTransactionByHashAsync(string transactionHash);
    Task RecordTransactionAsync(TransactionDto transaction);
}

public class TransactionService : ITransactionService
{
    private readonly IWeb3Service _web3Service;
    private readonly Web3DbContext _dbContext;
    private readonly ILogger<TransactionService> _logger;

    public TransactionService(IWeb3Service web3Service, Web3DbContext dbContext, ILogger<TransactionService> logger)
    {
        _web3Service = web3Service;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<WalletTransactionsDto> GetTransactionHistoryAsync(string walletAddress, int limit = 10)
    {
        _logger.LogInformation("Getting transaction history for: {Address} (limit: {Limit})", walletAddress, limit);

        var result = new WalletTransactionsDto { Address = walletAddress };

        try
        {
            var web3 = _web3Service.GetWeb3Instance();
            var blockNumber = await web3.Eth.Blocks.GetBlockNumber.SendRequestAsync();

            // This is a simplified example - in production, use an indexing service like The Graph
            // For now, we'll demonstrate transaction retrieval structure
            _logger.LogInformation("Current block number: {BlockNumber}", blockNumber);

            // TODO: Query transactions from RPC or indexing service
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting transaction history for: {Address}", walletAddress);
            throw;
        }
    }

    public async Task<TransactionDto> GetTransactionByHashAsync(string transactionHash)
    {
        _logger.LogInformation("Getting transaction details for: {Hash}", transactionHash);

        try
        {
            var web3 = _web3Service.GetWeb3Instance();
            var tx = await web3.Eth.Transactions.GetTransactionByHash.SendRequestAsync(transactionHash);

            if (tx == null)
                throw new InvalidOperationException("Transaction not found");

            return new TransactionDto
            {
                Hash = tx.TransactionHash,
                From = tx.From,
                To = tx.To,
                Value = tx.Value?.ToString() ?? "0",
                GasPrice = tx.GasPrice?.ToString() ?? "0",
                Gas = tx.Gas?.ToString() ?? "0",
                Timestamp = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting transaction: {Hash}", transactionHash);
            throw;
        }
    }

    public async Task RecordTransactionAsync(TransactionDto transaction)
    {
        _logger.LogInformation("Recording transaction: {Hash}", transaction.Hash);

        try
        {
            var record = new Domain.Models.TransactionRecord
            {
                Hash = transaction.Hash,
                From = transaction.From,
                To = transaction.To,
                Value = decimal.TryParse(transaction.Value, out var value) ? value : 0,
                GasPrice = long.TryParse(transaction.GasPrice, out var gasPrice) ? gasPrice : 0,
                Gas = long.TryParse(transaction.Gas, out var gas) ? gas : 0,
                Status = transaction.Status,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.Transactions.Add(record);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Transaction recorded: {Hash}", transaction.Hash);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording transaction: {Hash}", transaction.Hash);
            throw;
        }
    }
}
