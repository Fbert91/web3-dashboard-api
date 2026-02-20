namespace Web3DashboardAPI.Infrastructure.Utilities;

/// <summary>
/// Configuration for cache behavior.
/// </summary>
public class CacheOptions
{
    /// <summary>
    /// Default cache duration in seconds.
    /// </summary>
    public int DefaultDurationSeconds { get; set; } = 300; // 5 minutes

    /// <summary>
    /// Balance cache duration in seconds.
    /// </summary>
    public int BalanceCacheDurationSeconds { get; set; } = 60; // 1 minute

    /// <summary>
    /// Token details cache duration in seconds.
    /// </summary>
    public int TokenDetailsCacheDurationSeconds { get; set; } = 3600; // 1 hour
}

/// <summary>
/// Simple in-memory cache implementation with expiration support.
/// </summary>
public interface ICache
{
    T? Get<T>(string key);
    void Set<T>(string key, T value, int durationSeconds);
    void Remove(string key);
    void Clear();
}

/// <summary>
/// In-memory cache implementation.
/// </summary>
public class MemoryCache : ICache
{
    private class CacheEntry
    {
        public object Value { get; set; } = null!;
        public DateTime ExpiresAt { get; set; }
    }

    private readonly Dictionary<string, CacheEntry> _cache = new();
    private readonly object _lock = new();

    public T? Get<T>(string key)
    {
        lock (_lock)
        {
            if (!_cache.TryGetValue(key, out var entry))
                return default;

            if (DateTime.UtcNow > entry.ExpiresAt)
            {
                _cache.Remove(key);
                return default;
            }

            return (T)entry.Value;
        }
    }

    public void Set<T>(string key, T value, int durationSeconds)
    {
        lock (_lock)
        {
            _cache[key] = new CacheEntry
            {
                Value = value!,
                ExpiresAt = DateTime.UtcNow.AddSeconds(durationSeconds)
            };
        }
    }

    public void Remove(string key)
    {
        lock (_lock)
        {
            _cache.Remove(key);
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            _cache.Clear();
        }
    }
}

/// <summary>
/// Cache key builder for consistent key generation.
/// </summary>
public static class CacheKeyBuilder
{
    public static string GetEthBalance(string address) => $"eth_balance:{address.ToLower()}";
    public static string GetErc20Balance(string address, string tokenAddress) => 
        $"erc20_balance:{address.ToLower()}:{tokenAddress.ToLower()}";
    public static string GetTokenDetails(string tokenAddress) => $"token_details:{tokenAddress.ToLower()}";
    public static string GetGasPrice() => "gas_price";
}
