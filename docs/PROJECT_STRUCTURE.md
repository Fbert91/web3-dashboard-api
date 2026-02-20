# Project Structure Reference

Complete visual guide to the Web3 Dashboard API project structure.

```
Web3DashboardAPI/
│
├── src/                                    # Source code (clean architecture layers)
│   │
│   ├── Web3DashboardAPI.Domain/           # DOMAIN LAYER (Business rules)
│   │   ├── Web3DashboardAPI.Domain.csproj
│   │   ├── Entities/                      # Domain models
│   │   │   ├── Wallet.cs                  # Wallet aggregate root
│   │   │   ├── TokenBalance.cs            # ERC20 token holdings
│   │   │   └── Transaction.cs             # Transaction records
│   │   └── Dtos/                          # Data transfer objects
│   │       └── Web3Dtos.cs                # Request/response models
│   │
│   ├── Web3DashboardAPI.Application/      # APPLICATION LAYER (Use cases)
│   │   ├── Web3DashboardAPI.Application.csproj
│   │   └── Services/
│   │       └── WalletService.cs           # Wallet business logic
│   │           ├── AddOrGetWalletAsync()
│   │           ├── GetWalletAsync()
│   │           ├── SyncWalletBalancesAsync()
│   │           ├── AddTokenAsync()
│   │           └── GetTransactionHistoryAsync()
│   │
│   ├── Web3DashboardAPI.Infrastructure/   # INFRASTRUCTURE LAYER (External services)
│   │   ├── Web3DashboardAPI.Infrastructure.csproj
│   │   ├── Data/
│   │   │   └── Web3DbContext.cs           # Entity Framework Core context
│   │   │       ├── Wallets DbSet
│   │   │       ├── TokenBalances DbSet
│   │   │       └── Transactions DbSet
│   │   └── Ethereum/
│   │       └── EthereumService.cs         # Nethereum wrapper service
│   │           ├── IsValidAddress()
│   │           ├── GetEthBalanceAsync()
│   │           ├── GetErc20BalanceAsync()
│   │           └── GetErc20TokenDetailsAsync()
│   │
│   └── Web3DashboardAPI.API/              # PRESENTATION LAYER (REST endpoints)
│       ├── Web3DashboardAPI.API.csproj
│       ├── Controllers/
│       │   └── WalletsController.cs       # REST API endpoints
│       │       ├── POST /api/wallets/add
│       │       ├── GET /api/wallets/{address}
│       │       ├── GET /api/wallets/{address}/balance
│       │       ├── POST /api/wallets/{address}/sync
│       │       ├── POST /api/wallets/{address}/tokens/add
│       │       └── GET /api/wallets/{address}/transactions
│       ├── Program.cs                     # DI configuration & middleware
│       ├── appsettings.json               # API configuration
│       └── appsettings.Development.json   # Dev settings (gitignored)
│
├── tests/                                  # Testing layer
│   │
│   ├── Web3DashboardAPI.Tests.Unit/       # Unit tests
│   │   ├── Web3DashboardAPI.Tests.Unit.csproj
│   │   └── Services/
│   │       └── WalletServiceTests.cs      # Service tests with mocks
│   │           ├── AddOrGetWalletAsync_WithValidAddress_ShouldCreateWallet
│   │           ├── GetWalletAsync_WithValidAddress_ShouldReturnWallet
│   │           └── AddTokenAsync_WithValidToken_ShouldAddTokenBalance
│   │
│   └── Web3DashboardAPI.Tests.Integration/# Integration tests
│       ├── Web3DashboardAPI.Tests.Integration.csproj
│       └── Controllers/
│           └── WalletsControllerIntegrationTests.cs
│               ├── GetHealth_ShouldReturn200OK
│               ├── AddWallet_WithInvalidAddress_ShouldReturn400
│               ├── GetWallet_NotFound_ShouldReturn404
│               └── GetTransactions_ShouldReturn200OK
│
├── docs/                                  # Documentation (empty - add your docs here)
│   └── [Add architecture diagrams, guides here]
│
├── smart-contracts/                       # Example Solidity contracts
│   └── SimpleToken.sol                    # Learning ERC20 token
│
├── Web3DashboardAPI.sln                   # Solution file (all projects)
├── README.md                              # Main documentation & quick start
├── LEARNING.md                            # Web3 concepts guide
├── .env.example                           # Environment variables template
├── .gitignore                             # Git ignore rules
└── Web3DashboardAPI.postman_collection.json  # Postman API tests
```

## Layer Responsibilities

### Domain Layer (Web3DashboardAPI.Domain)
- **Contains:** Entities, DTOs, interfaces
- **No dependencies:** Independent, pure business rules
- **Examples:** Wallet, TokenBalance, Transaction classes

### Application Layer (Web3DashboardAPI.Application)
- **Contains:** Services, business logic, use cases
- **Depends on:** Domain layer
- **Handles:** Orchestrating domain objects, interacting with infrastructure
- **Example:** WalletService coordinates wallet operations

### Infrastructure Layer (Web3DashboardAPI.Infrastructure)
- **Contains:** Database context, Ethereum client, external services
- **Depends on:** Domain, Application layers
- **Handles:** EF Core, Nethereum, RPC calls
- **Examples:** Web3DbContext, EthereumService

### API Layer (Web3DashboardAPI.API)
- **Contains:** Controllers, endpoints, middleware, configuration
- **Depends on:** All layers
- **Handles:** HTTP routing, dependency injection, error handling
- **Examples:** WalletsController, Program.cs

## Data Flow Example: "Get Wallet Balance"

```
HTTP Request
    ↓
WalletsController.GetBalance(address)
    ↓
WalletService.GetWalletAsync(address)
    ├─ Query Web3DbContext.Wallets
    ├─ Call EthereumService.GetEthBalanceAsync()
    │   └─ Make RPC call via Nethereum
    └─ Format and return WalletResponse
    ↓
HTTP Response (JSON)
```

## Dependencies Between Projects

```
API
├─ Application ──┐
├─ Infrastructure|
└─ Domain ────────┤
                  │
Application ──────┤
├─ Domain ────────┘
└─ Infrastructure

Infrastructure ───┐
├─ Domain ────────┘
└─ Application ────┘

Domain
└─ (No dependencies - pure business logic)
```

## Database Schema Overview

### Wallets Table
```sql
PK  Id (int)
UK  Address (string) -- Unique, normalized lowercase
    Name (string)    -- Optional friendly name
    LastSyncedAt (datetime)
    CreatedAt (datetime)
```

### TokenBalances Table
```sql
PK  Id (int)
FK  WalletId (int) → Wallets.Id
UK  (WalletId, ContractAddress) -- No duplicate tokens per wallet
    ContractAddress (string)
    Name (string)
    Symbol (string)
    Decimals (int)
    Balance (string) -- Raw wei/units
    FormattedBalance (string) -- Human readable
    LastUpdatedAt (datetime)
```

### Transactions Table
```sql
PK  Id (int)
FK  WalletId (int) → Wallets.Id
UK  TransactionHash (string)
    BlockNumber (string)
    From (string)
    To (string)
    Value (string)      -- In wei
    Gas (string)
    GasPrice (string)
    Status (int)        -- 1=success, 0=failed
    BlockTimestamp (datetime)
    SyncedAt (datetime)
    TransactionType (string) -- "sent", "received", etc.
```

## Configuration Files

### appsettings.json (Checked in)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=Web3Dashboard.db"
  },
  "Ethereum": {
    "RpcUrl": "https://sepolia.infura.io/v3/YOUR_INFURA_KEY"
  }
}
```

### appsettings.Development.json (Gitignored)
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug"
    }
  }
}
```

### .env.example (Reference)
Lists all environment variables needed for configuration.

## Testing Structure

### Unit Tests (WalletServiceTests.cs)
- Mock IEthereumService
- Test WalletService methods in isolation
- Use in-memory database
- Fast, no external dependencies

### Integration Tests (WalletsControllerIntegrationTests.cs)
- Test full HTTP pipeline
- Use WebApplicationFactory
- Real database (in-memory)
- Validates end-to-end behavior

## Key Files to Understand

| File | Purpose | Learn |
|------|---------|-------|
| Program.cs | Dependency injection & middleware setup | How .NET Core configures apps |
| WalletService.cs | Core business logic | Clean architecture pattern |
| EthereumService.cs | Nethereum wrapper | Web3 integration |
| WalletsController.cs | REST endpoints | ASP.NET Core controllers |
| Web3DbContext.cs | EF Core mappings | Entity Framework Core |
| Web3Dtos.cs | Data contracts | DTO pattern |

---

**Use this reference when navigating the codebase!**
