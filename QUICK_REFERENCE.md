# Quick Reference Guide

Fast lookup for common commands and operations.

## 🏃 Quick Commands

### Project Setup
```bash
# Clone/enter project
cd /root/.openclaw/workspace/Web3DashboardAPI

# Restore dependencies
dotnet restore

# Build solution
dotnet build

# Run tests
dotnet test

# Run API
cd src/Web3DashboardAPI.API && dotnet run
```

### Database
```bash
# Create migration (from API folder)
dotnet ef migrations add MigrationName

# Apply migrations
dotnet ef database update

# Drop database (careful!)
dotnet ef database drop

# View migrations
dotnet ef migrations list
```

### Testing
```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test tests/Web3DashboardAPI.Tests.Unit

# Run with coverage
dotnet test /p:CollectCoverage=true

# Verbose output
dotnet test --logger "console;verbosity=detailed"
```

## 🔍 Common API Calls

### Wallet Operations

**Add wallet:**
```bash
curl -X POST http://localhost:5000/api/wallets/add \
  -H "Content-Type: application/json" \
  -d '{
    "address": "0x742d35Cc6634C0532925a3b844Bc9e7595f42e61",
    "name": "My Wallet"
  }'
```

**Get wallet:**
```bash
curl http://localhost:5000/api/wallets/0x742d35Cc6634C0532925a3b844Bc9e7595f42e61
```

**Get balance:**
```bash
curl http://localhost:5000/api/wallets/0x742d35Cc6634C0532925a3b844Bc9e7595f42e61/balance
```

**Sync wallet:**
```bash
curl -X POST http://localhost:5000/api/wallets/0x742d35Cc6634C0532925a3b844Bc9e7595f42e61/sync
```

**Add token:**
```bash
curl -X POST "http://localhost:5000/api/wallets/0x742d35Cc6634C0532925a3b844Bc9e7595f42e61/tokens/add?tokenAddress=0x1c7D4B196Cb0C7B01d743Fbc6116a902379C7238"
```

**Get transactions:**
```bash
curl "http://localhost:5000/api/wallets/0x742d35Cc6634C0532925a3b844Bc9e7595f42e61/transactions?limit=50"
```

### Health Check
```bash
curl http://localhost:5000/api/health
```

## 🧰 Common Ethereum Addresses (Sepolia)

| Token | Address |
|-------|---------|
| WETH | `0x7b79995e5f793a07bc00c21412e50ecae098e7f9` |
| USDC | `0x1c7D4B196Cb0C7B01d743Fbc6116a902379C7238` |
| DAI | `0x7af963cF6D228E564007A5b0f624F51F6b7a07A3` |

## 📝 File Locations

| Purpose | Path |
|---------|------|
| Main README | `/README.md` |
| Learning guide | `/LEARNING.md` |
| Project summary | `/PROJECT_SUMMARY.md` |
| Structure docs | `/docs/PROJECT_STRUCTURE.md` |
| Deployment guide | `/docs/DEPLOYMENT.md` |
| Features list | `/docs/FEATURES.md` |
| Example contract | `/smart-contracts/SimpleToken.sol` |
| Postman collection | `/Web3DashboardAPI.postman_collection.json` |
| Env template | `/.env.example` |

## 🔧 Configuration

### appsettings.json Key Settings

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=Web3Dashboard.db"
  },
  "Ethereum": {
    "RpcUrl": "https://sepolia.infura.io/v3/YOUR_KEY",
    "NetworkName": "Sepolia Testnet",
    "ChainId": 11155111
  }
}
```

### Environment Variables

```bash
ETHEREUM_RPC_URL=https://sepolia.infura.io/v3/...
DATABASE_CONNECTION_STRING=Data Source=Web3Dashboard.db
API_ENVIRONMENT=Development
```

## 🎯 Main Entry Points

| File | Purpose |
|------|---------|
| `Program.cs` | Startup & DI configuration |
| `WalletsController.cs` | REST endpoints |
| `WalletService.cs` | Business logic |
| `EthereumService.cs` | Blockchain interaction |
| `Web3DbContext.cs` | Database mappings |

## 🧪 Test File Locations

| Test | Path |
|------|------|
| Wallet service tests | `/tests/Web3DashboardAPI.Tests.Unit/Services/WalletServiceTests.cs` |
| Integration tests | `/tests/Web3DashboardAPI.Tests.Integration/Controllers/WalletsControllerIntegrationTests.cs` |

## 🚀 Deployment Checklists

### Docker Deploy
```bash
docker build -t web3-dashboard:latest .
docker run -p 5000:5000 \
  -e "Ethereum:RpcUrl=..." \
  web3-dashboard:latest
```

### Linux (systemd) Deploy
```bash
# See docs/DEPLOYMENT.md for full instructions
# Key: Create /etc/systemd/system/web3-dashboard.service
systemctl start web3-dashboard
systemctl status web3-dashboard
journalctl -u web3-dashboard -f
```

## 🆘 Troubleshooting

### API won't start
```bash
# Check .env file has valid RPC URL
cat .env

# Verify .NET 8 installed
dotnet --version

# Check database isn't locked
rm Web3Dashboard.db
dotnet run
```

### Tests failing
```bash
# Clean build
dotnet clean
dotnet build

# Run with verbose output
dotnet test --logger "console;verbosity=detailed"
```

### RPC connection error
```bash
# Test Infura endpoint
curl https://sepolia.infura.io/v3/YOUR_KEY \
  -X POST \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","method":"eth_getBalance","params":["0x0","latest"],"id":1}'
```

## 📊 Useful Websites

| Purpose | URL |
|---------|-----|
| Sepolia Explorer | https://sepolia.etherscan.io/ |
| Get Sepolia ETH | https://sepoliafaucet.com/ |
| Infura | https://www.infura.io/ |
| Remix IDE | https://remix.ethereum.org/ |
| Nethereum Docs | https://docs.nethereum.com/ |
| Ethereum Docs | https://ethereum.org/developers/ |

## 💻 IDE Shortcuts

### Visual Studio Code
```bash
# Build
Ctrl+Shift+B

# Run/Debug
F5

# Open Terminal
Ctrl+`
```

### Visual Studio 2022
```bash
# Build Solution
Ctrl+Shift+B

# Run
F5

# Package Manager Console
Ctrl+Alt+N
```

## 📈 Performance Tips

1. **Add database indexes** on frequently queried columns
2. **Cache token metadata** (name, symbol, decimals)
3. **Batch RPC calls** where possible
4. **Use connection pooling** in production
5. **Monitor response times** with Application Insights

## 🔐 Security Checklist

- [ ] Never commit `.env` or `appsettings.Development.json`
- [ ] Use HTTPS in production
- [ ] Validate all Ethereum addresses
- [ ] Store secrets in environment variables
- [ ] Use strong database passwords
- [ ] Enable rate limiting
- [ ] Review contract addresses carefully

---

**Bookmark this page for quick reference! 📌**
