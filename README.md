# Web3 Dashboard API

A learning-focused ASP.NET Core backend for interacting with the Ethereum blockchain using the Nethereum library. This project demonstrates Web3 integration, wallet management, and smart contract interaction in a clean, scalable architecture.

## 🚀 Quick Start

### Prerequisites
- .NET 8 SDK ([Download](https://dotnet.microsoft.com/download/dotnet/8.0))
- An Infura or Alchemy API key (free tier available)
- Optional: Postman or similar REST client

### Setup

1. **Clone the repository**
   ```bash
   cd /root/.openclaw/workspace/Web3DashboardAPI
   ```

2. **Configure your RPC endpoint**
   
   Edit `src/Web3DashboardAPI.API/appsettings.json`:
   ```json
   {
     "Ethereum": {
       "RpcUrl": "https://sepolia.infura.io/v3/YOUR_INFURA_KEY"
     }
   }
   ```

   Get your free Infura key: https://www.infura.io/

3. **Build the solution**
   ```bash
   dotnet build
   ```

4. **Run the API**
   ```bash
   cd src/Web3DashboardAPI.API
   dotnet run
   ```

5. **Access the API**
   - Swagger UI: http://localhost:5000
   - Health check: http://localhost:5000/api/health

## 📁 Project Structure

```
Web3DashboardAPI/
├── src/
│   ├── Web3DashboardAPI.Domain/          # Domain entities & DTOs
│   │   ├── Entities/
│   │   │   ├── Wallet.cs                 # Wallet entity
│   │   │   ├── TokenBalance.cs           # ERC20 token balance
│   │   │   └── Transaction.cs            # Transaction record
│   │   └── Dtos/
│   │       └── Web3Dtos.cs               # Request/response models
│   ├── Web3DashboardAPI.Application/     # Business logic
│   │   └── Services/
│   │       └── WalletService.cs          # Wallet operations
│   ├── Web3DashboardAPI.Infrastructure/  # Data & external services
│   │   ├── Data/
│   │   │   └── Web3DbContext.cs          # Entity Framework context
│   │   └── Ethereum/
│   │       └── EthereumService.cs        # Nethereum wrapper
│   └── Web3DashboardAPI.API/             # REST endpoints
│       ├── Controllers/
│       │   └── WalletsController.cs      # Wallet API endpoints
│       ├── Program.cs                    # Dependency injection & middleware
│       └── appsettings.json              # Configuration
├── tests/
│   ├── Web3DashboardAPI.Tests.Unit/      # Unit tests
│   └── Web3DashboardAPI.Tests.Integration/ # Integration tests
├── docs/
├── smart-contracts/                      # Example contracts
└── Web3DashboardAPI.sln
```

### Architecture Pattern

This project uses **Clean Architecture**:

```
┌─────────────────────────────────────┐
│      API (Controllers, DTOs)        │  ← User-facing REST endpoints
├─────────────────────────────────────┤
│  Application (Services, Use Cases)  │  ← Business logic
├─────────────────────────────────────┤
│      Domain (Entities, Models)      │  ← Core business rules
├─────────────────────────────────────┤
│ Infrastructure (DB, Ethereum, APIs) │  ← External services & data
└─────────────────────────────────────┘
```

## 📡 API Endpoints

### Wallet Management

#### Add or Get Wallet
```http
POST /api/wallets/add
Content-Type: application/json

{
  "address": "0x742d35Cc6634C0532925a3b844Bc9e7595f42e61",
  "name": "My Sepolia Wallet"
}
```

**Response:**
```json
{
  "address": "0x742d35cc6634c0532925a3b844bc9e7595f42e61",
  "name": "My Sepolia Wallet",
  "ethBalance": "1000000000000000000",
  "formattedEthBalance": "1.0",
  "tokenBalances": [],
  "lastSyncedAt": "2024-02-19T14:41:00Z"
}
```

#### Get Wallet Details
```http
GET /api/wallets/{address}
```

#### Get ETH Balance
```http
GET /api/wallets/{address}/balance
```

#### Sync Wallet (Refresh Balances)
```http
POST /api/wallets/{address}/sync
```

#### Add Token to Track
```http
POST /api/wallets/{address}/tokens/add?tokenAddress=0x...
```

#### Get Transaction History
```http
GET /api/wallets/{address}/transactions?limit=50
```

## 🔌 Nethereum Configuration

The `EthereumService` class wraps Nethereum for wallet and token operations:

```csharp
// Example: Check wallet balance
var balance = await ethereumService.GetEthBalanceAsync("0x742d35Cc6634C0532925a3b844Bc9e7595f42e61");

// Example: Get ERC20 token balance
var tokenBalance = await ethereumService.GetErc20BalanceAsync(
    walletAddress: "0x742d35Cc...",
    contractAddress: "0x1f9840a85d5aF5bf1D1762F925BDADdC4201F984" // UNI token
);

// Example: Get token details
var (name, symbol, decimals) = await ethereumService.GetErc20TokenDetailsAsync(
    contractAddress: "0x1f9840a85d5aF5bf1D1762F925BDADdC4201F984"
);
// Result: ("Uniswap", "UNI", 18)
```

## 🧪 Testing

### Run Unit Tests
```bash
dotnet test tests/Web3DashboardAPI.Tests.Unit
```

### Run Integration Tests
```bash
dotnet test tests/Web3DashboardAPI.Tests.Integration
```

### Run All Tests with Coverage
```bash
dotnet test /p:CollectCoverage=true
```

## 💾 Database

SQLite is used for local development. The database is created automatically on startup.

**Database file:** `Web3Dashboard.db`

### Database Schema

#### Wallets Table
```sql
CREATE TABLE Wallets (
    Id INTEGER PRIMARY KEY,
    Address TEXT UNIQUE NOT NULL,
    Name TEXT,
    LastSyncedAt DATETIME,
    CreatedAt DATETIME
);
```

#### TokenBalances Table
```sql
CREATE TABLE TokenBalances (
    Id INTEGER PRIMARY KEY,
    WalletId INTEGER NOT NULL,
    ContractAddress TEXT NOT NULL,
    Name TEXT NOT NULL,
    Symbol TEXT NOT NULL,
    Decimals INTEGER,
    Balance TEXT,
    FormattedBalance TEXT,
    LastUpdatedAt DATETIME,
    FOREIGN KEY (WalletId) REFERENCES Wallets(Id)
);
```

#### Transactions Table
```sql
CREATE TABLE Transactions (
    Id INTEGER PRIMARY KEY,
    WalletId INTEGER NOT NULL,
    TransactionHash TEXT UNIQUE NOT NULL,
    BlockNumber TEXT,
    From TEXT,
    To TEXT,
    Value TEXT,
    Gas TEXT,
    GasPrice TEXT,
    Input TEXT,
    Status INTEGER,
    BlockTimestamp DATETIME,
    SyncedAt DATETIME,
    TransactionType TEXT,
    FOREIGN KEY (WalletId) REFERENCES Wallets(Id)
);
```

## 🌐 Testnet Setup (Sepolia)

This project is configured for **Sepolia Testnet** by default.

### Get Sepolia Test ETH
1. Go to https://www.infura.io/
2. Create a free account
3. Get your API key
4. Use a faucet to get test ETH:
   - https://sepoliafaucet.com/
   - https://www.infura.io/faucet/sepolia

### Test Addresses
Some popular Sepolia token contracts for testing:

| Token | Address | Decimals |
|-------|---------|----------|
| Wrapped ETH | 0x7b79995e5f793a07bc00c21412e50ecae098e7f9 | 18 |
| USDC | 0x1c7D4B196Cb0C7B01d743Fbc6116a902379C7238 | 6 |
| DAI | 0x7af963cF6D228E564007A5b0f624F51F6b7a07A3 | 18 |

## 📚 Learning Resources

### Ethereum & Web3 Basics
- **What is Ethereum?** https://ethereum.org/en/developers/docs/
- **Smart Contracts 101:** https://ethereum.org/en/developers/docs/smart-contracts/
- **ERC20 Token Standard:** https://ethereum.org/en/developers/docs/standards/tokens/erc-20/

### Nethereum Documentation
- **Official Docs:** https://docs.nethereum.com/
- **GitHub:** https://github.com/Nethereum/Nethereum
- **Quick Start:** https://docs.nethereum.com/en/latest/getting-started/

### RPC & Blockchain Data
- **JSON-RPC Spec:** https://ethereum.org/en/developers/docs/apis/json-rpc/
- **Etherscan Testnet:** https://sepolia.etherscan.io/

### .NET & ASP.NET Core
- **Entity Framework Core:** https://learn.microsoft.com/en-us/ef/core/
- **ASP.NET Core Docs:** https://learn.microsoft.com/en-us/aspnet/core/

## 🔐 Best Practices

### API Security
- ✅ Validate all Ethereum addresses before use
- ✅ Use HTTPS in production
- ✅ Rate limit API endpoints
- ✅ Never expose private keys or mnemonic phrases
- ❌ Don't use unencrypted RPC endpoints for sensitive operations

### Smart Contract Interaction
- ✅ Always verify contract addresses
- ✅ Test on testnet before mainnet
- ✅ Handle decimal places correctly (e.g., 18 for most ERC20s)
- ✅ Check function ABIs match actual contract implementations

### Database
- ✅ Back up `Web3Dashboard.db` regularly
- ✅ Use connection pooling in production
- ✅ Consider PostgreSQL for production
- ❌ Don't commit sensitive configuration to Git

## 🛠️ Development

### IDE Setup
- **Visual Studio Code:** Install C# extension
- **Visual Studio 2022:** Choose ASP.NET Core workload
- **JetBrains Rider:** Full .NET 8 support

### Common Tasks

**Create a new migration:**
```bash
cd src/Web3DashboardAPI.API
dotnet ef migrations add YourMigrationName --project ../Web3DashboardAPI.Infrastructure
```

**Apply migrations:**
```bash
dotnet ef database update --project ../Web3DashboardAPI.Infrastructure
```

**Generate Swagger docs:**
```bash
dotnet build
# Docs are auto-generated and served at http://localhost:5000
```

## 📊 Example: Full Wallet Workflow

1. **Add a wallet to track:**
   ```bash
   curl -X POST http://localhost:5000/api/wallets/add \
     -H "Content-Type: application/json" \
     -d '{
       "address": "0x742d35Cc6634C0532925a3b844Bc9e7595f42e61",
       "name": "My Sepolia Wallet"
     }'
   ```

2. **Add a token to track (e.g., USDC):**
   ```bash
   curl -X POST "http://localhost:5000/api/wallets/0x742d35Cc6634C0532925a3b844Bc9e7595f42e61/tokens/add?tokenAddress=0x1c7D4B196Cb0C7B01d743Fbc6116a902379C7238"
   ```

3. **Sync wallet balances:**
   ```bash
   curl -X POST http://localhost:5000/api/wallets/0x742d35Cc6634C0532925a3b844Bc9e7595f42e61/sync
   ```

4. **Get wallet details:**
   ```bash
   curl http://localhost:5000/api/wallets/0x742d35Cc6634C0532925a3b844Bc9e7595f42e61
   ```

## 🚨 Troubleshooting

### "Invalid RPC URL"
- Check your Infura/Alchemy key is correct in `appsettings.json`
- Verify the endpoint is active and not rate-limited

### "Address already exists" error
- The wallet address is already in the database
- Use the GET endpoint to retrieve it instead

### Database locked (SQLite)
- Close any open connections to `Web3Dashboard.db`
- Restart the API

### Nethereum connection timeout
- Check internet connection
- Verify RPC endpoint is reachable
- Try a different RPC provider (Infura, Alchemy, etc.)

## 📦 Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| Nethereum.Web3 | 4.21.0 | Ethereum interaction |
| Microsoft.EntityFrameworkCore.Sqlite | 8.0.1 | Database ORM |
| Swashbuckle.AspNetCore | 6.4.6 | Swagger/OpenAPI |
| xunit | 2.6.6 | Unit testing |
| Moq | 4.20.70 | Mocking framework |

## 📄 License

MIT License - See LICENSE file for details

## 🤝 Contributing

This is a learning project! Feel free to:
- Add more features (transaction sync, real-time updates, etc.)
- Improve error handling
- Add more test coverage
- Contribute documentation

---

**Happy learning! 🚀**

Questions? Check `LEARNING.md` for Web3 concepts explained!
