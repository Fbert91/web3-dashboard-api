using Microsoft.EntityFrameworkCore;
using Web3DashboardAPI.Application.Services;
using Web3DashboardAPI.Infrastructure.Data;
using Web3DashboardAPI.Infrastructure.Ethereum;
using Web3DashboardAPI.Infrastructure.Utilities;
using Web3DashboardAPI.Infrastructure.RateLimiting;
using Web3DashboardAPI.Infrastructure.Authentication;

var builder = WebApplicationBuilder.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();

// Configure SQLite database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Data Source=Web3Dashboard.db";
builder.Services.AddDbContext<Web3DbContext>(options =>
    options.UseSqlite(connectionString));

// Register infrastructure services
builder.Services.AddSingleton<ICache, MemoryCache>();

// Register rate limiting
var rateLimitOptions = builder.Configuration.GetSection("RateLimiting").Get<RateLimitOptions>() 
    ?? new RateLimitOptions();
builder.Services.AddRateLimiting(rateLimitOptions);

// Register API key authentication
builder.Services.AddApiKeyAuthentication();

// Register application services
builder.Services.AddScoped<IWalletService, WalletService>();

// Register Ethereum service with RPC URL from configuration
var rpcUrl = builder.Configuration.GetSection("Ethereum:RpcUrl").Value 
    ?? "https://sepolia.infura.io/v3/YOUR_INFURA_KEY";
builder.Services.AddScoped<IEthereumService>(provider =>
{
    var cache = provider.GetRequiredService<ICache>();
    var logger = provider.GetRequiredService<ILogger<EthereumService>>();
    var rateLimiter = provider.GetRequiredService<IRateLimiter>();
    
    var retryOptions = new RetryOptions { MaxRetries = 3, InitialDelayMs = 100 };
    var retryPolicy = new RetryPolicy(retryOptions, provider.GetRequiredService<ILogger<RetryPolicy>>());
    
    var cbOptions = new CircuitBreakerOptions { FailureThreshold = 5, TimeoutMs = 30000 };
    var circuitBreaker = new CircuitBreaker(cbOptions, provider.GetRequiredService<ILogger<CircuitBreaker>>());
    
    return new EthereumService(rpcUrl, cache, retryPolicy, circuitBreaker, rateLimiter, logger);
});

// Add Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Web3 Dashboard API",
        Version = "v2.0",
        Description = "Ethereum wallet management and blockchain interaction API with enhanced security and reliability",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "Web3 Dashboard",
            Url = new Uri("https://github.com/yourusername/Web3DashboardAPI")
        }
    });

    // Add security definition for API key
    options.AddSecurityDefinition("ApiKey", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Name = "X-API-Key",
        Description = "API Key for authentication"
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "ApiKey"
                }
            },
            new string[] { }
        }
    });

    // Add XML comments to Swagger
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

// Add CORS with restricted origins in production
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policyBuilder =>
    {
        if (builder.Environment.IsDevelopment())
        {
            policyBuilder.AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader();
        }
        else
        {
            // In production, restrict CORS
            var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                ?? new[] { "https://yourdomain.com" };
            policyBuilder.WithOrigins(allowedOrigins)
                .AllowAnyMethod()
                .AllowAnyHeader();
        }
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Web3 Dashboard API V2");
        options.RoutePrefix = string.Empty; // Set Swagger UI at the root
    });
}

// Create database tables on startup
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<Web3DbContext>();
    dbContext.Database.Migrate();
}

app.UseHttpsRedirection();

// Apply middleware in correct order
app.UseCors("AllowAll");
app.UseRateLimiting();
app.UseApiKeyAuthentication();
app.UseAuthorization();

app.MapControllers();

// Health check endpoint (no auth required)
app.MapGet("/api/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

// API info endpoint (no auth required)
app.MapGet("/api/info", () => Results.Ok(new 
{ 
    name = "Web3 Dashboard API",
    version = "2.0",
    security = "API Key (X-API-Key header required)",
    timestamp = DateTime.UtcNow
}));

app.Run();
