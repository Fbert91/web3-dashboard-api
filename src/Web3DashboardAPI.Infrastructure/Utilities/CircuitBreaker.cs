namespace Web3DashboardAPI.Infrastructure.Utilities;

/// <summary>
/// Circuit breaker states.
/// </summary>
public enum CircuitState
{
    Closed,     // Normal operation
    Open,       // Circuit broken, rejecting requests
    HalfOpen    // Testing if service recovered
}

/// <summary>
/// Configuration for circuit breaker behavior.
/// </summary>
public class CircuitBreakerOptions
{
    /// <summary>
    /// Number of failures before opening circuit.
    /// </summary>
    public int FailureThreshold { get; set; } = 5;

    /// <summary>
    /// Timeout before attempting half-open state (milliseconds).
    /// </summary>
    public int TimeoutMs { get; set; } = 30000; // 30 seconds

    /// <summary>
    /// Number of successful calls in half-open before closing circuit.
    /// </summary>
    public int SuccessThreshold { get; set; } = 2;
}

/// <summary>
/// Implements circuit breaker pattern for resilient RPC calls.
/// </summary>
public class CircuitBreaker
{
    private readonly CircuitBreakerOptions _options;
    private readonly ILogger<CircuitBreaker> _logger;
    private CircuitState _state = CircuitState.Closed;
    private int _failureCount = 0;
    private int _successCount = 0;
    private DateTime _lastFailureTime = DateTime.MinValue;
    private readonly object _lock = new();

    public CircuitState State
    {
        get
        {
            lock (_lock)
            {
                return _state;
            }
        }
    }

    public CircuitBreaker(CircuitBreakerOptions options, ILogger<CircuitBreaker> logger)
    {
        _options = options;
        _logger = logger;
    }

    /// <summary>
    /// Executes an operation with circuit breaker protection.
    /// </summary>
    public async Task<T> ExecuteAsync<T>(
        Func<Task<T>> operation,
        string operationName = "Operation")
    {
        lock (_lock)
        {
            if (_state == CircuitState.Open)
            {
                if (DateTime.UtcNow - _lastFailureTime > TimeSpan.FromMilliseconds(_options.TimeoutMs))
                {
                    _logger.LogInformation($"Circuit breaker transitioning to HalfOpen for {operationName}");
                    _state = CircuitState.HalfOpen;
                    _successCount = 0;
                }
                else
                {
                    throw new InvalidOperationException(
                        $"Circuit breaker is open for {operationName}. Service temporarily unavailable.");
                }
            }
        }

        try
        {
            var result = await operation();
            OnSuccess();
            return result;
        }
        catch (Exception ex)
        {
            OnFailure();
            throw;
        }
    }

    private void OnSuccess()
    {
        lock (_lock)
        {
            _failureCount = 0;

            if (_state == CircuitState.HalfOpen)
            {
                _successCount++;
                if (_successCount >= _options.SuccessThreshold)
                {
                    _logger.LogInformation("Circuit breaker closing");
                    _state = CircuitState.Closed;
                    _successCount = 0;
                }
            }
        }
    }

    private void OnFailure()
    {
        lock (_lock)
        {
            _failureCount++;
            _lastFailureTime = DateTime.UtcNow;
            _successCount = 0;

            if (_failureCount >= _options.FailureThreshold)
            {
                _logger.LogWarning($"Circuit breaker opening after {_failureCount} failures");
                _state = CircuitState.Open;
            }
        }
    }

    /// <summary>
    /// Resets the circuit breaker state.
    /// </summary>
    public void Reset()
    {
        lock (_lock)
        {
            _state = CircuitState.Closed;
            _failureCount = 0;
            _successCount = 0;
            _logger.LogInformation("Circuit breaker reset");
        }
    }
}
