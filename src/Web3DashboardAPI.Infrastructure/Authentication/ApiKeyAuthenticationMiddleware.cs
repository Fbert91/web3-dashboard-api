namespace Web3DashboardAPI.Infrastructure.Authentication;

/// <summary>
/// Interface for API key validation.
/// </summary>
public interface IApiKeyValidator
{
    Task<bool> ValidateKeyAsync(string apiKey);
    Task<string?> GetUserIdFromKeyAsync(string apiKey);
}

/// <summary>
/// Simple in-memory API key validator (can be replaced with database lookup).
/// </summary>
public class ApiKeyValidator : IApiKeyValidator
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ApiKeyValidator> _logger;

    // Simple in-memory storage - in production, use a database
    private readonly Dictionary<string, string> _validKeys;

    public ApiKeyValidator(IConfiguration configuration, ILogger<ApiKeyValidator> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _validKeys = new();

        // Load API keys from configuration
        var keysSection = _configuration.GetSection("ApiKeys:Valid");
        if (keysSection.Exists())
        {
            var keys = keysSection.Get<Dictionary<string, string>>();
            if (keys != null)
            {
                _validKeys = keys;
            }
        }

        // Add a default test key for development
        if (!_validKeys.Any())
        {
            _validKeys["test-key-12345"] = "test-user";
        }
    }

    public Task<bool> ValidateKeyAsync(string apiKey)
    {
        var isValid = _validKeys.ContainsKey(apiKey);
        if (!isValid)
        {
            _logger.LogWarning($"Invalid API key attempt: {apiKey[..10]}...");
        }
        return Task.FromResult(isValid);
    }

    public Task<string?> GetUserIdFromKeyAsync(string apiKey)
    {
        _validKeys.TryGetValue(apiKey, out var userId);
        return Task.FromResult(userId);
    }
}

/// <summary>
/// Middleware for API key authentication.
/// </summary>
public class ApiKeyAuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiKeyAuthenticationMiddleware> _logger;
    private const string ApiKeyHeaderName = "X-API-Key";

    public ApiKeyAuthenticationMiddleware(RequestDelegate next, ILogger<ApiKeyAuthenticationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IApiKeyValidator apiKeyValidator)
    {
        // Skip authentication for health check and swagger
        if (context.Request.Path.StartsWithSegments("/api/health") ||
            context.Request.Path.StartsWithSegments("/swagger") ||
            context.Request.Path.StartsWithSegments("/api/auth"))
        {
            await _next(context);
            return;
        }

        // Check for API key
        if (!context.Request.Headers.TryGetValue(ApiKeyHeaderName, out var apiKeyValue))
        {
            _logger.LogWarning("Request without API key");
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { error = "API key is required" });
            return;
        }

        var apiKey = apiKeyValue.ToString();
        if (!await apiKeyValidator.ValidateKeyAsync(apiKey))
        {
            _logger.LogWarning("Request with invalid API key");
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { error = "Invalid API key" });
            return;
        }

        var userId = await apiKeyValidator.GetUserIdFromKeyAsync(apiKey);
        context.Items["UserId"] = userId;

        await _next(context);
    }
}

/// <summary>
/// Extension methods for API key authentication.
/// </summary>
public static class ApiKeyAuthenticationExtensions
{
    public static IServiceCollection AddApiKeyAuthentication(this IServiceCollection services)
    {
        services.AddScoped<IApiKeyValidator, ApiKeyValidator>();
        return services;
    }

    public static IApplicationBuilder UseApiKeyAuthentication(this IApplicationBuilder app)
    {
        app.UseMiddleware<ApiKeyAuthenticationMiddleware>();
        return app;
    }
}
