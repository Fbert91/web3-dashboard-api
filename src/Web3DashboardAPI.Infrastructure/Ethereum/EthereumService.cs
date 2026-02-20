using Nethereum.Web3;
using Nethereum.Util;
using System.Numerics;
using Microsoft.Extensions.Logging;
using Web3DashboardAPI.Infrastructure.Utilities;
using Web3DashboardAPI.Infrastructure.RateLimiting;

namespace Web3DashboardAPI.Infrastructure.Ethereum;

/// <summary>
/// Interface for Ethereum blockchain operations.
/// </summary>
public interface IEthereumService
{
    /// <summary>
    /// Validates if an address is a valid Ethereum address with checksum validation.
    /// </summary>
    bool IsValidAddress(string address);

    /// <summary>
    /// Gets the ETH balance for an address.
    /// </summary>
    Task<string> GetEthBalanceAsync(string address);

    /// <summary>
    /// Gets the balance of an ERC20 token for an address.
    /// </summary>
    Task<string> GetErc20BalanceAsync(string address, string contractAddress);

    /// <summary>
    /// Gets ERC20 token details (name, symbol, decimals).
    /// </summary>
    Task<(string Name, string Symbol, int Decimals)> GetErc20TokenDetailsAsync(string contractAddress);

    /// <summary>
    /// Gets the current gas price.
    /// </summary>
    Task<string> GetGasPriceAsync();
}

/// <summary>
/// Implementation of Ethereum operations using Nethereum with retry logic, caching, and circuit breaker.
/// </summary>
public class EthereumService : IEthereumService
{
    private readonly IWeb3 _web3;
    private readonly string _rpcUrl;
    private readonly ICache _cache;
    private readonly RetryPolicy _retryPolicy;
    private readonly CircuitBreaker _circuitBreaker;
    private readonly IRateLimiter _rateLimiter;
    private readonly ILogger<EthereumService> _logger;

    /// <summary>
    /// Initializes a new instance of the EthereumService.
    /// </summary>
    /// <param name="rpcUrl">RPC endpoint URL (e.g., Infura, Alchemy, local node)</param>
    public EthereumService(
        string rpcUrl,
        ICache? cache = null,
        RetryPolicy? retryPolicy = null,
        CircuitBreaker? circuitBreaker = null,
        IRateLimiter? rateLimiter = null,
        ILogger<EthereumService>? logger = null)
    {
        _rpcUrl = rpcUrl;
        _web3 = new Web3(rpcUrl);
        _cache = cache ?? new MemoryCache();
        
        // Default retry policy
        _retryPolicy = retryPolicy ?? new RetryPolicy(
            new RetryOptions { MaxRetries = 3, InitialDelayMs = 100 },
            logger ?? new NullLogger<RetryPolicy>());
        
        // Default circuit breaker
        _circuitBreaker = circuitBreaker ?? new CircuitBreaker(
            new CircuitBreakerOptions { FailureThreshold = 5, TimeoutMs = 30000 },
            logger ?? new NullLogger<CircuitBreaker>());
        
        // Default rate limiter (no-op if not provided)
        _rateLimiter = rateLimiter ?? new NoOpRateLimiter();
        
        _logger = logger ?? new NullLogger<EthereumService>();
    }

    /// <summary>
    /// Validates if an address is a valid Ethereum address with checksum validation.
    /// </summary>
    public bool IsValidAddress(string address)
    {
        if (string.IsNullOrWhiteSpace(address))
            return false;

        try
        {
            return AddressValidator.IsValidAddress(address);
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Address validation error: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Gets the ETH balance for an address with caching and retry logic.
    /// </summary>
    public async Task<string> GetEthBalanceAsync(string address)
    {
        if (!IsValidAddress(address))
            throw new ArgumentException("Invalid Ethereum address", nameof(address));

        var normalizedAddress = AddressValidator.NormalizeAddress(address);
        var cacheKey = CacheKeyBuilder.GetEthBalance(normalizedAddress);

        // Check cache first
        var cachedBalance = _cache.Get<string>(cacheKey);
        if (cachedBalance != null)
        {
            _logger.LogInformation($"ETH balance retrieved from cache for {normalizedAddress}");
            return cachedBalance;
        }

        // Check rate limit
        if (!_rateLimiter.IsAllowed($"eth_balance:{normalizedAddress}"))
        {
            throw new InvalidOperationException("Rate limit exceeded for balance query");
        }

        try
        {
            // Use circuit breaker + retry
            var balance = await _circuitBreaker.ExecuteAsync(
                async () => await _retryPolicy.ExecuteAsync(
                    async () =>
                    {
                        var bal = await _web3.Eth.GetBalance.SendRequestAsync(normalizedAddress);
                        return bal.Value.ToString();
                    },
                    $"GetEthBalance({normalizedAddress})"),
                $"GetEthBalance({normalizedAddress})");

            // Cache the result
            var cacheOptions = new CacheOptions();
            _cache.Set(cacheKey, balance, cacheOptions.BalanceCacheDurationSeconds);

            return balance;
        }
        catch (Exception ex) when (!(ex is InvalidOperationException))
        {
            _logger.LogError($"Failed to get ETH balance for {normalizedAddress}: {ex.Message}");
            throw new InvalidOperationException(
                $"Failed to get balance for {normalizedAddress}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Gets the balance of an ERC20 token for an address with caching and retry logic.
    /// </summary>
    public async Task<string> GetErc20BalanceAsync(string address, string contractAddress)
    {
        if (!IsValidAddress(address))
            throw new ArgumentException("Invalid wallet address", nameof(address));

        if (!IsValidAddress(contractAddress))
            throw new ArgumentException("Invalid contract address", nameof(contractAddress));

        var normalizedAddress = AddressValidator.NormalizeAddress(address);
        var normalizedToken = AddressValidator.NormalizeAddress(contractAddress);
        var cacheKey = CacheKeyBuilder.GetErc20Balance(normalizedAddress, normalizedToken);

        // Check cache first
        var cachedBalance = _cache.Get<string>(cacheKey);
        if (cachedBalance != null)
        {
            _logger.LogInformation($"Token balance retrieved from cache for {normalizedToken}");
            return cachedBalance;
        }

        // Check rate limit
        if (!_rateLimiter.IsAllowed($"token_balance:{normalizedAddress}:{normalizedToken}"))
        {
            throw new InvalidOperationException("Rate limit exceeded for token balance query");
        }

        try
        {
            // Use circuit breaker + retry
            var balance = await _circuitBreaker.ExecuteAsync(
                async () => await _retryPolicy.ExecuteAsync(
                    async () =>
                    {
                        var contract = _web3.Eth.GetContract(ERC20_ABI, normalizedToken);
                        var balanceOfFunction = contract.GetFunction("balanceOf");
                        var bal = await balanceOfFunction.CallAsync<BigInteger>(normalizedAddress);
                        return bal.ToString();
                    },
                    $"GetErc20Balance({normalizedToken})"),
                $"GetErc20Balance({normalizedToken})");

            // Cache the result
            var cacheOptions = new CacheOptions();
            _cache.Set(cacheKey, balance, cacheOptions.BalanceCacheDurationSeconds);

            return balance;
        }
        catch (Exception ex) when (!(ex is InvalidOperationException))
        {
            _logger.LogError($"Failed to get token balance for {normalizedAddress}: {ex.Message}");
            throw new InvalidOperationException(
                $"Failed to get token balance for {normalizedAddress}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Gets ERC20 token details (name, symbol, decimals) with caching and retry logic.
    /// </summary>
    public async Task<(string Name, string Symbol, int Decimals)> GetErc20TokenDetailsAsync(string contractAddress)
    {
        if (!IsValidAddress(contractAddress))
            throw new ArgumentException("Invalid contract address", nameof(contractAddress));

        var normalizedToken = AddressValidator.NormalizeAddress(contractAddress);
        var cacheKey = CacheKeyBuilder.GetTokenDetails(normalizedToken);

        // Check cache first (token details don't change often)
        var cachedDetails = _cache.Get<(string, string, int)>(cacheKey);
        if (cachedDetails != default)
        {
            _logger.LogInformation($"Token details retrieved from cache for {normalizedToken}");
            return cachedDetails;
        }

        // Check rate limit
        if (!_rateLimiter.IsAllowed($"token_details:{normalizedToken}"))
        {
            throw new InvalidOperationException("Rate limit exceeded for token details query");
        }

        try
        {
            // Use circuit breaker + retry
            var details = await _circuitBreaker.ExecuteAsync(
                async () => await _retryPolicy.ExecuteAsync(
                    async () =>
                    {
                        var contract = _web3.Eth.GetContract(ERC20_ABI, normalizedToken);

                        var nameFunction = contract.GetFunction("name");
                        var symbolFunction = contract.GetFunction("symbol");
                        var decimalsFunction = contract.GetFunction("decimals");

                        var name = await nameFunction.CallAsync<string>();
                        var symbol = await symbolFunction.CallAsync<string>();
                        var decimals = await decimalsFunction.CallAsync<int>();

                        return (name ?? "Unknown", symbol ?? "Unknown", decimals);
                    },
                    $"GetErc20TokenDetails({normalizedToken})"),
                $"GetErc20TokenDetails({normalizedToken})");

            // Cache the result (1 hour for token details)
            var cacheOptions = new CacheOptions();
            _cache.Set(cacheKey, details, cacheOptions.TokenDetailsCacheDurationSeconds);

            return details;
        }
        catch (Exception ex) when (!(ex is InvalidOperationException))
        {
            _logger.LogError($"Failed to get token details for {normalizedToken}: {ex.Message}");
            throw new InvalidOperationException(
                $"Failed to get token details for {normalizedToken}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Gets the current gas price with caching and retry logic.
    /// </summary>
    public async Task<string> GetGasPriceAsync()
    {
        var cacheKey = CacheKeyBuilder.GetGasPrice();

        // Check cache first (gas price changes frequently)
        var cachedPrice = _cache.Get<string>(cacheKey);
        if (cachedPrice != null)
        {
            return cachedPrice;
        }

        try
        {
            // Use circuit breaker + retry
            var price = await _circuitBreaker.ExecuteAsync(
                async () => await _retryPolicy.ExecuteAsync(
                    async () =>
                    {
                        var gasPrice = await _web3.Eth.GasPrice.SendRequestAsync();
                        return gasPrice.Value.ToString();
                    },
                    "GetGasPrice"),
                "GetGasPrice");

            // Cache for 10 seconds
            _cache.Set(cacheKey, price, 10);

            return price;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to get gas price: {ex.Message}");
            throw new InvalidOperationException($"Failed to get gas price: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Minimal ERC20 ABI containing required functions (balanceOf, name, symbol, decimals).
    /// </summary>
    private const string ERC20_ABI = @"[
        {
            'constant': true,
            'inputs': [{ 'name': '_owner', 'type': 'address' }],
            'name': 'balanceOf',
            'outputs': [{ 'name': 'balance', 'type': 'uint256' }],
            'type': 'function'
        },
        {
            'constant': true,
            'inputs': [],
            'name': 'name',
            'outputs': [{ 'name': '', 'type': 'string' }],
            'type': 'function'
        },
        {
            'constant': true,
            'inputs': [],
            'name': 'symbol',
            'outputs': [{ 'name': '', 'type': 'string' }],
            'type': 'function'
        },
        {
            'constant': true,
            'inputs': [],
            'name': 'decimals',
            'outputs': [{ 'name': '', 'type': 'uint8' }],
            'type': 'function'
        }
    ]";
}

/// <summary>
/// No-op rate limiter (for when rate limiting is disabled).
/// </summary>
internal class NoOpRateLimiter : IRateLimiter
{
    public bool IsAllowed(string identifier) => true;
}

