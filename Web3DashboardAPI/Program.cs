using Serilog;
using Web3DashboardAPI.Infrastructure.Configuration;
using Web3DashboardAPI.Application.Services;
using Web3DashboardAPI.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Logging setup
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// Configuration
var web3Config = builder.Configuration.GetSection("Web3").Get<Web3Configuration>();
if (web3Config == null)
    throw new InvalidOperationException("Web3 configuration is missing");

// Add services
builder.Services.AddSingleton(web3Config);
builder.Services.AddScoped<IWeb3Service, Web3Service>();
builder.Services.AddScoped<IWalletService, WalletService>();
builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddScoped<ISmartContractService, SmartContractService>();

// Database
builder.Services.AddDbContext<Web3DbContext>();

// API & Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Web3 Dashboard API",
        Version = "v1",
        Description = "Ethereum wallet and smart contract interaction API (Learning Project)",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "Web3 Learning Hub"
        }
    });
});

// CORS for local development
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalDev", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Database initialization
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<Web3DbContext>();
    dbContext.Database.EnsureCreated();
}

// Middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Web3 Dashboard API v1");
    });
}

app.UseHttpsRedirection();
app.UseCors("AllowLocalDev");
app.MapControllers();

try
{
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
