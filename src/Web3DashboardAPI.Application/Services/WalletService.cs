using Web3DashboardAPI.Domain.Dtos;
using Web3DashboardAPI.Domain.Entities;
using Web3DashboardAPI.Infrastructure.Data;
using Web3DashboardAPI.Infrastructure.Ethereum;
using Web3DashboardAPI.Infrastructure.Utilities;
using Web3DashboardAPI.Infrastructure.Validation;
using Microsoft.EntityFrameworkCore;
using System.Numerics;

namespace Web3DashboardAPI.Application.Services;

/// <summary>
/// Interface for wallet operations.
/// </summary>
public interface IWalletService
{
    /// <summary>
    /// Adds a new wallet or retrieves an existing one.
    /// </summary>
    Task<WalletResponse> AddOrGetWalletAsync(string address, string? name = null);

    /// <summary>
    /// Gets wallet details including ETH balance and token balances.
    /// </summary>
    Task<WalletResponse> GetWalletAsync(string address);

    /// <summary>
    /// Syncs wallet balances with the blockchain.
    /// </summary>
    Task SyncWalletBalancesAsync(string address);

    /// <summary>
    /// Adds a token to track for a wallet.
    /// </summary>
    Task AddTokenAsync(string walletAddress, string tokenContractAddress);

    /// <summary>
    /// Gets transaction history for a wallet.
    /// </summary>
    Task<List<TransactionResponse>> GetTransactionHistoryAsync(string address, int limit = 50);
}

/// <summary>
/// Service for managing wallet operations.
/// </summary>
public class WalletService : IWalletService
{
    private readonly Web3DbContext _dbContext;
    private readonly IEthereumService _ethereumService;

    public WalletService(Web3DbContext dbContext, IEthereumService ethereumService)
    {
        _dbContext = dbContext;
        _ethereumService = ethereumService;
    }

    public async Task<WalletResponse> AddOrGetWalletAsync(string address, string? name = null)
    {
        ValidateAddress(address);

        var normalizedAddress = AddressValidator.NormalizeAddress(address);
        var sanitizedName = InputValidator.SanitizeName(name);

        var wallet = await _dbContext.Wallets
            .Include(w => w.TokenBalances)
            .FirstOrDefaultAsync(w => w.Address == normalizedAddress);

        if (wallet == null)
        {
            wallet = new Wallet
            {
                Address = normalizedAddress,
                Name = sanitizedName,
                CreatedAt = DateTime.UtcNow,
                LastSyncedAt = DateTime.UtcNow
            };

            _dbContext.Wallets.Add(wallet);
            await _dbContext.SaveChangesAsync();

            // Sync balances immediately
            await SyncWalletBalancesAsync(normalizedAddress);
        }
        else if (!string.IsNullOrEmpty(sanitizedName) && wallet.Name != sanitizedName)
        {
            wallet.Name = sanitizedName;
            await _dbContext.SaveChangesAsync();
        }

        return await GetWalletAsync(normalizedAddress);
    }

    public async Task<WalletResponse> GetWalletAsync(string address)
    {
        ValidateAddress(address);

        var normalizedAddress = AddressValidator.NormalizeAddress(address);

        var wallet = await _dbContext.Wallets
            .Include(w => w.TokenBalances)
            .FirstOrDefaultAsync(w => w.Address == normalizedAddress);

        if (wallet == null)
            throw new InvalidOperationException($"Wallet {address} not found");

        var ethBalance = await _ethereumService.GetEthBalanceAsync(address);

        return new WalletResponse
        {
            Address = wallet.Address,
            Name = wallet.Name,
            EthBalance = ethBalance,
            FormattedEthBalance = FormatBalance(ethBalance, 18),
            TokenBalances = wallet.TokenBalances
                .Select(tb => new TokenBalanceResponse
                {
                    ContractAddress = tb.ContractAddress,
                    Name = tb.Name,
                    Symbol = tb.Symbol,
                    Decimals = tb.Decimals,
                    Balance = tb.Balance,
                    FormattedBalance = tb.FormattedBalance
                })
                .ToList(),
            LastSyncedAt = wallet.LastSyncedAt
        };
    }

    public async Task SyncWalletBalancesAsync(string address)
    {
        ValidateAddress(address);

        var normalizedAddress = AddressValidator.NormalizeAddress(address);

        var wallet = await _dbContext.Wallets
            .Include(w => w.TokenBalances)
            .FirstOrDefaultAsync(w => w.Address == normalizedAddress);

        if (wallet == null)
            throw new InvalidOperationException($"Wallet {address} not found");

        // Update all tracked token balances
        foreach (var tokenBalance in wallet.TokenBalances)
        {
            try
            {
                var balance = await _ethereumService.GetErc20BalanceAsync(address, tokenBalance.ContractAddress);
                tokenBalance.Balance = balance;
                tokenBalance.FormattedBalance = FormatBalance(balance, tokenBalance.Decimals);
                tokenBalance.LastUpdatedAt = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to sync token {tokenBalance.Symbol}: {ex.Message}");
            }
        }

        wallet.LastSyncedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();
    }

    public async Task AddTokenAsync(string walletAddress, string tokenContractAddress)
    {
        ValidateAddress(walletAddress);
        ValidateAddress(tokenContractAddress);

        var normalizedWalletAddress = AddressValidator.NormalizeAddress(walletAddress);
        var normalizedTokenAddress = AddressValidator.NormalizeAddress(tokenContractAddress);

        var wallet = await _dbContext.Wallets
            .Include(w => w.TokenBalances)
            .FirstOrDefaultAsync(w => w.Address == normalizedWalletAddress);

        if (wallet == null)
            throw new InvalidOperationException($"Wallet {walletAddress} not found");

        // Check if token already added
        if (wallet.TokenBalances.Any(tb => tb.ContractAddress == normalizedTokenAddress))
            return;

        try
        {
            var (name, symbol, decimals) = await _ethereumService.GetErc20TokenDetailsAsync(tokenContractAddress);
            var balance = await _ethereumService.GetErc20BalanceAsync(walletAddress, tokenContractAddress);

            var tokenBalance = new TokenBalance
            {
                WalletId = wallet.Id,
                ContractAddress = normalizedTokenAddress,
                Name = name,
                Symbol = symbol,
                Decimals = decimals,
                Balance = balance,
                FormattedBalance = FormatBalance(balance, decimals),
                LastUpdatedAt = DateTime.UtcNow
            };

            _dbContext.TokenBalances.Add(tokenBalance);
            await _dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to add token {tokenContractAddress}", ex);
        }
    }

    public async Task<List<TransactionResponse>> GetTransactionHistoryAsync(string address, int limit = 50)
    {
        ValidateAddress(address);

        var normalizedAddress = AddressValidator.NormalizeAddress(address);

        var wallet = await _dbContext.Wallets
            .FirstOrDefaultAsync(w => w.Address == normalizedAddress);

        if (wallet == null)
            return new List<TransactionResponse>();

        var transactions = await _dbContext.Transactions
            .Where(t => t.WalletId == wallet.Id)
            .OrderByDescending(t => t.BlockTimestamp)
            .Take(limit)
            .ToListAsync();

        return transactions
            .Select(t => new TransactionResponse
            {
                TransactionHash = t.TransactionHash,
                BlockNumber = t.BlockNumber,
                From = t.From,
                To = t.To,
                Value = t.Value,
                FormattedValue = FormatBalance(t.Value, 18),
                Status = t.Status,
                BlockTimestamp = t.BlockTimestamp,
                TransactionType = t.TransactionType
            })
            .ToList();
    }

    private void ValidateAddress(string address)
    {
        if (!AddressValidator.IsValidAddress(address))
            throw new ArgumentException($"Invalid Ethereum address: {address}", nameof(address));
    }

    private string FormatBalance(string balance, int decimals)
    {
        if (!BigInteger.TryParse(balance, out var bigBalance))
            return "0";

        if (bigBalance == 0)
            return "0";

        var divisor = BigInteger.Pow(10, decimals);
        var whole = bigBalance / divisor;
        var remainder = bigBalance % divisor;

        if (remainder == 0)
            return whole.ToString();

        var remainderStr = remainder.ToString().PadLeft(decimals, '0').TrimEnd('0');
        return $"{whole}.{remainderStr}";
    }
}
