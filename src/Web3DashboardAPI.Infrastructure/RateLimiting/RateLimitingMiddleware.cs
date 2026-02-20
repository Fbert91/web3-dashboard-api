namespace Web3DashboardAPI.Infrastructure.RateLimiting;

/// <summary>
/// Configuration for rate limiting.
/// </summary>
public class RateLimitOptions
{
    /// <summary>
    /// Maximum requests per window.
    /// </summary>
    public int RequestsPerWindow { get; set; } = 100;

    /// <summary>
    /// Time window in seconds.
    /// </summary>
    public int WindowDurationSeconds { get; set; } = 60;

    /// <summary>
    /// Whether to enable rate limiting.
    /// </summary>
    public bool Enabled { get; set; } = true;
}

/// <summary>
/// Token bucket for rate limiting.
/// </summary>
public class TokenBucket
{
    private int _tokens;
    private DateTime _lastRefillTime;
    private readonly int _maxTokens;
    private readonly int _refillRate;
    private readonly object _lock = new();

    public DateTime LastRefillTime => _lastRefillTime;

    public TokenBucket(int maxTokens, int refillIntervalMs)
    {
        _maxTokens = maxTokens;
        _tokens = maxTokens;
        _refillRate = maxTokens / (1000 / Math.Max(refillIntervalMs, 1));
        _lastRefillTime = DateTime.UtcNow;
    }

    public bool TryConsumeToken()
    {
        lock (_lock)
        {
            Refill();
            if (_tokens > 0)
            {
                _tokens--;
                return true;
            }
            return false;
        }
    }

    private void Refill()
    {
        var now = DateTime.UtcNow;
        var timeSinceLastRefill = (now - _lastRefillTime).TotalMilliseconds;

        if (timeSinceLastRefill > 1000)
        {
            _tokens = _maxTokens;
            _lastRefillTime = now;
        }
    }
}

/// <summary>
/// In-memory rate limiter with per-IP tracking.
/// </summary>
public interface IRateLimiter
{
    bool IsAllowed(string identifier);
}

/// <summary>
/// Rate limiter implementation.
/// </summary>
public class RateLimiter : IRateLimiter
{
    private readonly RateLimitOptions _options;
    private readonly Dictionary<string, TokenBucket> _buckets = new();
    private readonly object _lock = new();
    private readonly ILogger<RateLimiter> _logger;

    public RateLimiter(RateLimitOptions options, ILogger<RateLimiter> logger)
    {
        _options = options;
        _logger = logger;

        // Start cleanup task for old buckets
        _ = Task.Run(CleanupOldBuckets);
    }

    public bool IsAllowed(string identifier)
    {
        if (!_options.Enabled)
            return true;

        lock (_lock)
        {
            if (!_buckets.TryGetValue(identifier, out var bucket))
            {
                bucket = new TokenBucket(_options.RequestsPerWindow, _options.WindowDurationSeconds * 1000);
                _buckets[identifier] = bucket;
            }

            var allowed = bucket.TryConsumeToken();
            if (!allowed)
            {
                _logger.LogWarning($"Rate limit exceeded for {identifier}");
            }

            return allowed;
        }
    }

    private async Task CleanupOldBuckets()
    {
        while (true)
        {
            await Task.Delay(TimeSpan.FromMinutes(5));

            lock (_lock)
            {
                var now = DateTime.UtcNow;
                var keysToRemove = _buckets
                    .Where(kvp => (now - kvp.Value.LastRefillTime).TotalMinutes > 10)
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var key in keysToRemove)
                {
                    _buckets.Remove(key);
                }

                if (keysToRemove.Any())
                {
                    _logger.LogInformation($"Cleaned up {keysToRemove.Count} rate limit buckets");
                }
            }
        }
    }
}

/// <summary>
/// Rate limiting middleware.
/// </summary>
public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitingMiddleware> _logger;

    public RateLimitingMiddleware(RequestDelegate next, ILogger<RateLimitingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IRateLimiter rateLimiter)
    {
        // Get client identifier (IP address or user ID)
        var identifier = context.Items["UserId"]?.ToString() ?? 
                        context.Connection.RemoteIpAddress?.ToString() ?? 
                        "unknown";

        if (!rateLimiter.IsAllowed(identifier))
        {
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.Response.Headers.Add("Retry-After", "60");
            await context.Response.WriteAsJsonAsync(new { error = "Rate limit exceeded. Please try again later." });
            return;
        }

        await _next(context);
    }
}

/// <summary>
/// Extension methods for rate limiting.
/// </summary>
public static class RateLimitingExtensions
{
    public static IServiceCollection AddRateLimiting(this IServiceCollection services, RateLimitOptions options)
    {
        services.AddSingleton(options);
        services.AddSingleton<IRateLimiter, RateLimiter>();
        return services;
    }

    public static IApplicationBuilder UseRateLimiting(this IApplicationBuilder app)
    {
        app.UseMiddleware<RateLimitingMiddleware>();
        return app;
    }
}
