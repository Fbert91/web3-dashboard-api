# Architecture Documentation

This document explains the design and structure of the Web3 Dashboard API project.

## Clean Architecture Overview

```
┌─────────────────────────────────────────────┐
│          PRESENTATION LAYER                 │
│   (Controllers, HTTP, Swagger endpoints)    │
├─────────────────────────────────────────────┤
│          APPLICATION LAYER                  │
│   (Services, Business Logic, DTOs)          │
├─────────────────────────────────────────────┤
│          DOMAIN LAYER                       │
│   (Entities, Value Objects, Domain Logic)   │
├─────────────────────────────────────────────┤
│      INFRASTRUCTURE LAYER                   │
│  (Database, External Services, RPC calls)   │
└─────────────────────────────────────────────┘
```

### Why Clean Architecture?

✅ **Testability** - Easy to mock dependencies  
✅ **Maintainability** - Clear separation of concerns  
✅ **Scalability** - Easy to add new features  
✅ **Independence** - Loosely coupled layers  

---

## Project Structure

```
Web3DashboardAPI/
│
├── Web3DashboardAPI/                    # Main API Project
│   ├── Program.cs                       # ASP.NET Core setup, DI container
│   ├── appsettings.json                 # Configuration
│   │
│   ├── Application/                     # Application/Business Logic Layer
│   │   ├── Services/                    # Business services
│   │   │   ├── IWeb3Service.cs          # Ethereum RPC interface
│   │   │   ├── Web3Service.cs           # RPC connection management
│   │   │   ├── IWalletService.cs        # Wallet operations
│   │   │   ├── WalletService.cs         # Address validation, balance
│   │   │   ├── ITransactionService.cs   # Transaction queries
│   │   │   ├── TransactionService.cs    # Tx details, history
│   │   │   ├── ISmartContractService.cs # Contract interaction
│   │   │   └── SmartContractService.cs  # ERC20, ABI calls
│   │   └── DTOs/                        # Data Transfer Objects
│   │       └── Web3Dtos.cs              # Wallet, Tx, Contract DTOs
│   │
│   ├── Domain/                          # Domain/Business Model Layer
│   │   └── Models/                      # Domain entities
│   │       └── Records.cs               # WalletRecord, TransactionRecord, TokenRecord
│   │
│   ├── Infrastructure/                  # Infrastructure/Data Access Layer
│   │   ├── Configuration/               # External config
│   │   │   └── Web3Configuration.cs     # RPC, network settings
│   │   └── Persistence/                 # Data access
│   │       ├── Web3DbContext.cs         # EF Core DbContext
│   │       └── Migrations/              # EF Core migrations (future)
│   │
│   └── Presentation/                    # Presentation/API Layer
│       └── Controllers/                 # HTTP endpoints
│           ├── WalletController.cs      # GET /wallet/*
│           ├── TransactionController.cs # GET /transaction/*
│           ├── ContractController.cs    # GET /contract/*
│           └── HealthController.cs      # GET /health/*
│
├── Web3DashboardAPI.Tests/              # Test Project
│   └── Application/Services/
│       └── WalletServiceTests.cs        # Unit tests for WalletService
│
├── contracts/                           # Smart Contracts
│   └── SimpleToken.sol                  # Example ERC20 token
│
├── docs/                                # Documentation
│   ├── LEARNING.md                      # Web3 concepts explained
│   ├── POSTMAN.md                       # API testing guide
│   ├── DEPLOYMENT.md                    # Deploy contract guide
│   ├── GETTING_STARTED.md               # Quick start
│   └── ARCHITECTURE.md                  # This file
│
├── README.md                            # Project overview
├── .env.example                         # Environment configuration template
├── .gitignore                           # Git exclusions
└── Web3DashboardAPI.sln                 # Visual Studio solution
```

---

## Layer Responsibilities

### Presentation Layer (Controllers)

**Responsibility:** Handle HTTP requests/responses

```csharp
[ApiController]
[Route("api/[controller]")]
public class WalletController
{
    // Receives HTTP request
    // Calls service layer
    // Returns HTTP response
    
    [HttpGet("{address}/balance")]
    public async Task<ActionResult<WalletBalanceDto>> GetBalance(string address)
    {
        // 1. Validate input
        // 2. Call WalletService
        // 3. Return DTO (not database model!)
        // 4. Handle errors → HTTP status codes
    }
}
```

**Rules:**
- ✅ Receives/returns DTOs (not domain models)
- ✅ Validates HTTP inputs
- ✅ Maps HTTP status codes
- ❌ No business logic
- ❌ No database access

### Application Layer (Services)

**Responsibility:** Business logic and orchestration

```csharp
public interface IWalletService
{
    Task<WalletValidationDto> ValidateWalletAsync(string address);
    Task<WalletBalanceDto> GetWalletBalanceAsync(string address);
}

public class WalletService : IWalletService
{
    private readonly IWeb3Service _web3Service;
    
    // Validates address format
    // Calls Web3Service for blockchain data
    // Converts Wei to ETH
    // Throws domain exceptions (not HTTP errors)
}
```

**Rules:**
- ✅ Contains core business logic
- ✅ Uses dependency injection
- ✅ Calls other services
- ✅ Throws domain exceptions
- ❌ No HTTP knowledge
- ❌ No direct database access (through DbContext only)

### Domain Layer (Models)

**Responsibility:** Business entity definitions

```csharp
public class WalletRecord
{
    public int Id { get; set; }
    public string Address { get; set; }  // 0x...
    public string? Label { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class TransactionRecord
{
    public int Id { get; set; }
    public string Hash { get; set; }      // Tx hash
    public string From { get; set; }      // From address
    public decimal Value { get; set; }    // Amount
    // ... more properties
}
```

**Rules:**
- ✅ Pure data classes
- ✅ Represents business concepts
- ✅ No dependencies on other layers
- ❌ No service logic
- ❌ No HTTP/database specifics

### Infrastructure Layer (Database & Services)

**Responsibility:** External integrations

```csharp
public class Web3Service : IWeb3Service
{
    // Manages Nethereum connections
    // Makes JSON-RPC calls
    // Returns raw blockchain data
}

public class Web3DbContext : DbContext
{
    // Entity Framework configuration
    // Database mapping
    // Migrations
}
```

**Rules:**
- ✅ Implements interfaces from other layers
- ✅ No knowledge of HTTP/controllers
- ✅ Handles external services (Ethereum, database)
- ❌ Not called directly from controllers (use services!)

---

## Data Flow Example

### Request Flow: Get Wallet Balance

```
1. HTTP REQUEST
   GET /api/wallet/0x742d35Cc.../balance

2. PRESENTATION LAYER
   WalletController.GetBalance(address)
   └─> Validates input
   └─> Returns BadRequest if invalid

3. APPLICATION LAYER
   WalletService.GetWalletBalanceAsync(address)
   └─> Calls IWeb3Service.GetWeb3Instance()
   └─> Calls IWalletService.ValidateWalletAsync()
   └─> Calls Web3.Eth.GetBalance.SendRequestAsync()

4. INFRASTRUCTURE LAYER
   Web3Service.GetWeb3Instance()
   └─> Returns Nethereum IWeb3 instance
   └─> Makes JSON-RPC call to Ethereum

5. BLOCKCHAIN
   Returns balance in Wei

6. BACK THROUGH LAYERS
   Infrastructure → Converts Wei to ETH
   Application → Creates WalletBalanceDto
   Presentation → Returns JSON

7. HTTP RESPONSE
   {
     "address": "0x742d35Cc...",
     "ethBalance": "2.5"
   }
```

---

## Dependency Injection

### Setup (Program.cs)

```csharp
// Register services for dependency injection
builder.Services.AddSingleton(web3Config);      // Configuration
builder.Services.AddScoped<IWeb3Service, Web3Service>();
builder.Services.AddScoped<IWalletService, WalletService>();
builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddScoped<ISmartContractService, SmartContractService>();
builder.Services.AddDbContext<Web3DbContext>();
```

**Scopes:**
- `Singleton` - One instance for app lifetime (configuration)
- `Scoped` - New instance per request (services)
- `Transient` - New instance every time (rare use)

### Usage in Controller

```csharp
public class WalletController
{
    private readonly IWalletService _walletService;
    
    // ASP.NET automatically injects IWalletService
    public WalletController(IWalletService walletService)
    {
        _walletService = walletService;
    }
    
    public async Task<ActionResult<WalletBalanceDto>> GetBalance(string address)
    {
        // _walletService is automatically available
        var balance = await _walletService.GetWalletBalanceAsync(address);
        return Ok(balance);
    }
}
```

---

## Testing Strategy

### Unit Tests (Web3DashboardAPI.Tests)

Test **one service** in isolation:

```csharp
public class WalletServiceTests
{
    private readonly Mock<IWeb3Service> _mockWeb3;
    private readonly WalletService _walletService;
    
    public WalletServiceTests()
    {
        // Mock dependencies
        _mockWeb3 = new Mock<IWeb3Service>();
        
        // Create service under test
        _walletService = new WalletService(_mockWeb3.Object, logger);
    }
    
    [Fact]
    public async Task ValidateWallet_WithEmptyAddress_ShouldReturnInvalid()
    {
        // Arrange
        var address = "";
        
        // Act
        var result = await _walletService.ValidateWalletAsync(address);
        
        // Assert
        Assert.False(result.IsValid);
    }
}
```

**Benefits:**
- ✅ Fast (no network calls)
- ✅ Isolated (one class at a time)
- ✅ Deterministic (same input = same output)

### Integration Tests (Future)

Test **multiple layers** together:

```csharp
[Fact]
public async Task GetBalance_WithValidAddress_ShouldReturnBalance()
{
    // Uses real database, real RPC calls
    // Slower but tests real behavior
}
```

---

## Adding New Features

### Example: Add "Get Transaction Status" endpoint

#### Step 1: Add to Domain (if needed)

No new domain model needed - use existing `TransactionRecord`.

#### Step 2: Add Service Method

```csharp
// In ITransactionService
Task<TransactionStatusDto> GetTransactionStatusAsync(string hash);

// In TransactionService
public async Task<TransactionStatusDto> GetTransactionStatusAsync(string hash)
{
    var tx = await _web3Service.GetWeb3Instance()
        .Eth.Transactions.GetTransactionReceipt.SendRequestAsync(hash);
    
    return new TransactionStatusDto
    {
        Hash = hash,
        Status = tx?.Status?.Value == 1 ? "Success" : "Failed",
        BlockNumber = tx?.BlockNumber.Value.ToString()
    };
}
```

#### Step 3: Add DTO

```csharp
public class TransactionStatusDto
{
    public string Hash { get; set; } = null!;
    public string Status { get; set; } = null!;
    public string? BlockNumber { get; set; }
}
```

#### Step 4: Add Controller Endpoint

```csharp
[HttpGet("{hash}/status")]
public async Task<ActionResult<TransactionStatusDto>> GetStatus(string hash)
{
    var status = await _transactionService.GetTransactionStatusAsync(hash);
    return Ok(status);
}
```

#### Step 5: Test

```csharp
[Fact]
public async Task GetStatus_WithValidHash_ShouldReturnStatus()
{
    // Arrange
    var hash = "0x...";
    
    // Act
    var result = await _transactionService.GetTransactionStatusAsync(hash);
    
    // Assert
    Assert.NotNull(result.Status);
}
```

---

## Configuration Management

### Environment Variables (.env)

```env
Web3__RpcUrl=https://sepolia.infura.io/v3/KEY
Web3__NetworkId=11155111
```

### Binding in Program.cs

```csharp
var web3Config = builder.Configuration
    .GetSection("Web3")
    .Get<Web3Configuration>();

builder.Services.AddSingleton(web3Config);
```

### Usage in Services

```csharp
public class Web3Service
{
    private readonly Web3Configuration _config;
    
    public Web3Service(Web3Configuration config)
    {
        _config = config;
        // _config.RpcUrl is available
    }
}
```

---

## Logging

Integrated with Serilog:

```csharp
_logger.LogInformation("Getting balance for: {Address}", address);
_logger.LogError(ex, "Failed to get balance");
_logger.LogDebug("Blockchain call successful");
```

Logs to console and can be configured for file output.

---

## Error Handling

### Service Layer

Throws domain exceptions:

```csharp
if (!AddressUtil.Current.IsValidEthereumAddressHexFormat(address))
{
    throw new InvalidOperationException("Invalid address");
}
```

### Controller Layer

Catches and converts to HTTP responses:

```csharp
try
{
    var result = await _service.GetDataAsync();
    return Ok(result);
}
catch (InvalidOperationException ex)
{
    return BadRequest(new { error = ex.Message });
}
catch (Exception ex)
{
    _logger.LogError(ex, "Unexpected error");
    return StatusCode(500, new { error = "Internal server error" });
}
```

---

## Best Practices

### ✅ DO:

- Use interfaces for dependency injection
- Keep services focused (single responsibility)
- Write descriptive error messages
- Log important operations
- Use DTOs for API responses
- Test business logic, not infrastructure
- Use async/await for I/O operations
- Validate inputs in services and controllers

### ❌ DON'T:

- Access database directly from controllers
- Mix HTTP logic with business logic
- Throw HTTP exceptions from services
- Return domain models from APIs
- Hardcode configuration values
- Block async operations with `.Result`
- Ignore exceptions silently
- Make RPC calls from controllers

---

## Performance Considerations

### Caching (Future Enhancement)

```csharp
// Could add Redis or in-memory cache for:
// - Token info (name, symbol, decimals)
// - Validated addresses
// - Block numbers
```

### Async/Await

All network calls are async:
```csharp
await web3.Eth.GetBalance.SendRequestAsync(address);
```

### Database Queries

Currently minimal (only recording transactions).
In future: Index addresses and hashes for fast lookup.

---

## Future Improvements

1. **Event Listeners** - Listen to contract events in real-time
2. **Transaction Signing** - Send transactions (with key management)
3. **Batch Operations** - Query multiple addresses/contracts at once
4. **Caching** - Reduce RPC calls with intelligent caching
5. **GraphQL** - Alternative to REST API
6. **WebSocket** - Real-time updates for balance/transactions
7. **Rate Limiting** - Protect RPC provider from abuse

---

## References

- [ASP.NET Core Architecture](https://docs.microsoft.com/en-us/dotnet/architecture/modern-web-apps-azure/)
- [Clean Architecture](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [Dependency Injection](https://docs.microsoft.com/en-us/dotnet/core/extensions/dependency-injection)
- [Nethereum Architecture](https://docs.nethereum.com/)

---

This architecture makes the project **maintainable, testable, and scalable**. Enjoy building! 🚀
