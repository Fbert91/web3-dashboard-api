namespace Web3DashboardAPI.Infrastructure.Utilities;

/// <summary>
/// Configuration for retry behavior.
/// </summary>
public class RetryOptions
{
    /// <summary>
    /// Maximum number of retry attempts.
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Initial delay in milliseconds.
    /// </summary>
    public int InitialDelayMs { get; set; } = 100;

    /// <summary>
    /// Maximum delay in milliseconds.
    /// </summary>
    public int MaxDelayMs { get; set; } = 5000;

    /// <summary>
    /// Exponential backoff multiplier.
    /// </summary>
    public double BackoffMultiplier { get; set; } = 2.0;

    /// <summary>
    /// Types of exceptions to retry on (null = retry all).
    /// </summary>
    public Type[]? RetryableExceptions { get; set; }
}

/// <summary>
/// Implements retry logic with exponential backoff.
/// </summary>
public class RetryPolicy
{
    private readonly RetryOptions _options;
    private readonly ILogger<RetryPolicy> _logger;

    public RetryPolicy(RetryOptions options, ILogger<RetryPolicy> logger)
    {
        _options = options;
        _logger = logger;
    }

    /// <summary>
    /// Executes an async operation with retry logic.
    /// </summary>
    public async Task<T> ExecuteAsync<T>(
        Func<Task<T>> operation,
        string operationName = "Operation")
    {
        var attempt = 0;
        var delay = _options.InitialDelayMs;

        while (true)
        {
            try
            {
                _logger.LogInformation($"Executing {operationName} (attempt {attempt + 1}/{_options.MaxRetries})");
                return await operation();
            }
            catch (Exception ex)
            {
                attempt++;

                if (!ShouldRetry(ex, attempt))
                {
                    _logger.LogError($"Operation {operationName} failed after {attempt} attempts: {ex.Message}");
                    throw;
                }

                _logger.LogWarning(
                    $"Operation {operationName} failed (attempt {attempt}), retrying in {delay}ms: {ex.Message}");

                await Task.Delay(delay);
                delay = CalculateNextDelay(delay);
            }
        }
    }

    /// <summary>
    /// Executes a sync operation with retry logic.
    /// </summary>
    public T Execute<T>(
        Func<T> operation,
        string operationName = "Operation")
    {
        var attempt = 0;
        var delay = _options.InitialDelayMs;

        while (true)
        {
            try
            {
                _logger.LogInformation($"Executing {operationName} (attempt {attempt + 1}/{_options.MaxRetries})");
                return operation();
            }
            catch (Exception ex)
            {
                attempt++;

                if (!ShouldRetry(ex, attempt))
                {
                    _logger.LogError($"Operation {operationName} failed after {attempt} attempts: {ex.Message}");
                    throw;
                }

                _logger.LogWarning(
                    $"Operation {operationName} failed (attempt {attempt}), retrying in {delay}ms: {ex.Message}");

                Task.Delay(delay).Wait();
                delay = CalculateNextDelay(delay);
            }
        }
    }

    private bool ShouldRetry(Exception ex, int attempt)
    {
        if (attempt >= _options.MaxRetries)
            return false;

        if (_options.RetryableExceptions == null)
            return true;

        return _options.RetryableExceptions.Any(t => t.IsInstanceOfType(ex));
    }

    private int CalculateNextDelay(int currentDelay)
    {
        var nextDelay = (int)(currentDelay * _options.BackoffMultiplier);
        return Math.Min(nextDelay, _options.MaxDelayMs);
    }
}
