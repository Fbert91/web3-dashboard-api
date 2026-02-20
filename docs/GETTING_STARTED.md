# Getting Started - First Run Guide

This guide will help you get the Web3 Dashboard API running on your machine in less than 10 minutes.

## Prerequisites Check

Before starting, verify you have these installed:

```bash
# Check .NET 8 installation
dotnet --version
# Should show: 8.0.x or higher

# Check Git installation
git --version

# Check Node.js (optional, for Hardhat)
node --version
```

If any are missing, install them:
- **.NET 8 SDK**: https://dotnet.microsoft.com/download
- **Git**: https://git-scm.com/
- **Node.js**: https://nodejs.org/ (optional)

## Step 1: Clone/Setup Project

```bash
# If cloning from Git
git clone <repository-url>
cd Web3DashboardAPI

# Or if working locally
cd /root/.openclaw/workspace/Web3DashboardAPI
```

## Step 2: Configure Environment

```bash
# Copy example configuration
cp .env.example .env

# Edit .env and add your Infura/Alchemy key
# (Use any text editor)
```

### Getting an RPC URL (Free):

1. **Infura** (Recommended for beginners):
   - Go to https://infura.io/
   - Sign up (free)
   - Create new project
   - Select "Sepolia" network
   - Copy Project ID
   - URL: `https://sepolia.infura.io/v3/YOUR_PROJECT_ID`

2. **Alchemy**:
   - Go to https://www.alchemy.com/
   - Sign up (free tier available)
   - Create app → select "Sepolia"
   - Copy API URL

3. **Ankr**:
   - Go to https://www.ankr.com/
   - Free public RPC available
   - URL: `https://rpc.ankr.co/eth_sepolia`

## Step 3: Restore Dependencies

```bash
# Download NuGet packages
dotnet restore

# This may take 1-2 minutes on first run
```

## Step 4: Build Project

```bash
# Compile the project
dotnet build

# Should show: "Build succeeded"
```

## Step 5: Create Database

```bash
# Database is created automatically on first run
# But you can manually initialize with:
# (Not necessary, but good to verify setup)
```

## Step 6: Run the API

```bash
# From project root directory
dotnet run --project Web3DashboardAPI/

# Should show:
# info: Microsoft.Hosting.Lifetime[14]
#       Now listening on: http://localhost:5000
#       Now listening on: https://localhost:5001
```

## Step 7: Test the API

Open a new terminal (keep the server running):

```bash
# Test basic health check
curl http://localhost:5000/api/health

# Should return:
# {"status":"healthy",...}
```

## Step 8: Access Swagger UI

Open your browser and go to:
```
http://localhost:5000/swagger
```

You should see interactive API documentation! 🎉

---

## First API Calls

Try these in order:

### 1. Check Network Connection

```bash
curl http://localhost:5000/api/health/network
```

**Expected Response:**
```json
{
  "status": "connected",
  "networkId": "11155111",
  "timestamp": "2024-02-19T..."
}
```

If you see an error, check your `.env` RPC URL!

### 2. Validate a Wallet Address

```bash
curl "http://localhost:5000/api/wallet/validate/0x742d35Cc6634C0532925a3b844Bc9e7595f42bA0"
```

### 3. Check ETH Balance

Use a Sepolia address that has test ETH:

```bash
curl "http://localhost:5000/api/wallet/0x742d35Cc6634C0532925a3b844Bc9e7595f42bA0/eth-balance"
```

---

## Troubleshooting

### Error: "Cannot connect to RPC"

**Problem:** Network connection failed

**Solutions:**
1. Check `.env` RPC URL is correct (with YOUR_KEY replaced)
2. Verify internet connection
3. Test RPC URL manually:
   ```bash
   curl https://sepolia.infura.io/v3/YOUR_KEY -X POST -H "Content-Type: application/json" -d '{"jsonrpc":"2.0","method":"eth_blockNumber","params":[],"id":1}'
   ```
4. Check if RPC provider is having issues

### Error: "Restore failed" or "Package not found"

**Problem:** NuGet packages couldn't be downloaded

**Solutions:**
```bash
# Clear NuGet cache
dotnet nuget locals all --clear

# Try restore again
dotnet restore

# Or restore with diagnostics
dotnet restore --verbosity diagnostic
```

### Error: "Port 5000 is already in use"

**Problem:** Another application is using port 5000

**Solutions:**
```bash
# Find and kill process on port 5000 (Unix/Mac)
lsof -ti:5000 | xargs kill -9

# Or on Windows:
netstat -ano | findstr :5000
taskkill /PID <PID> /F

# Or change port in program, or use different port:
dotnet run --project Web3DashboardAPI/ -- --urls "http://localhost:5002"
```

### Error: "Microsoft.EntityFrameworkCore not found"

**Problem:** .NET targeting wrong framework version

**Solutions:**
```bash
# Verify .NET version
dotnet --version
# Should be 8.0.x or higher

# If not, install .NET 8
# Then try restore again
dotnet restore
```

---

## Next Steps

### What to Read Next:

1. **[LEARNING.md](./docs/LEARNING.md)** - Web3 concepts explained
2. **[POSTMAN.md](./docs/POSTMAN.md)** - Test all API endpoints
3. **[DEPLOYMENT.md](./docs/DEPLOYMENT.md)** - Deploy smart contract
4. **[README.md](./README.md)** - Full project overview

### What to Code Next:

1. ✅ Get this project running (you are here!)
2. ⬜ Read through LEARNING.md (essential)
3. ⬜ Test endpoints with Postman/Swagger
4. ⬜ Deploy SimpleToken to Sepolia
5. ⬜ Add transaction signing (requires private key management)
6. ⬜ Build a frontend UI

---

## Daily Workflow

Once set up, your daily workflow:

```bash
# In terminal 1: Run the API
cd Web3DashboardAPI
dotnet run

# In terminal 2: Test endpoints
curl http://localhost:5000/api/health

# In browser: 
# Open http://localhost:5000/swagger
```

---

## Learning Tips

- 📖 **Read code**: Start with `Program.cs`, then explore services
- 🧪 **Test often**: Use Swagger to test endpoints as you change code
- 📝 **Take notes**: Document what you learn in a personal file
- 🔍 **Read errors**: Stack traces tell you exactly what's wrong
- 💬 **Ask questions**: Error messages are hints, not failures

---

## Common Questions

**Q: Do I need a real ETH wallet?**
A: No! Sepolia is free testnet. Use any address with test ETH from faucet.

**Q: Can I lose money?**
A: No! Sepolia has no real value. Mainnet is the dangerous one.

**Q: How do I know if RPC connection works?**
A: Visit `/api/health/network` - it confirms connection.

**Q: What if port 5000 doesn't work?**
A: Change it in `Program.cs` or use `--urls` parameter.

**Q: How do I see all endpoints?**
A: Open http://localhost:5000/swagger in browser!

---

## Success Indicators

✅ When you see these, your setup is correct:

- [ ] `dotnet build` shows "Build succeeded"
- [ ] `dotnet run` shows "Now listening on: http://localhost:5000"
- [ ] `/api/health` returns `{"status":"healthy",...}`
- [ ] `/api/health/network` shows `"status":"connected"`
- [ ] Swagger UI loads at http://localhost:5000/swagger
- [ ] Can access other endpoints without 500 errors

---

**You're all set!** 🚀

Next: Read [LEARNING.md](./docs/LEARNING.md) to understand the Web3 concepts behind this API.

Questions? Check the error message, or review these docs more carefully. Learning takes time! 📚
