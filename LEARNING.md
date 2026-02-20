# LEARNING.md - Web3 & Nethereum Concepts

This guide explains the Web3/blockchain concepts used in this project and how they connect to the .NET code.

## 📚 Table of Contents

1. [Ethereum Basics](#ethereum-basics)
2. [Wallets & Addresses](#wallets--addresses)
3. [Gas & Transactions](#gas--transactions)
4. [Smart Contracts & ERC20](#smart-contracts--erc20)
5. [Nethereum Integration](#nethereum-integration)
6. [Data Formatting](#data-formatting)

---

## 🔗 Ethereum Basics

### What is Ethereum?

Ethereum is a **decentralized blockchain** where:
- Users can send **ether (ETH)** to each other
- **Smart contracts** run automatically when conditions are met
- Everything is **immutable** (permanent, can't be changed)
- The network is **distributed** (no single company controls it)

### How It Works

```
┌─────────────────────────────────────────────────────────────┐
│                    Ethereum Network                          │
│  (13,000+ nodes running the same code worldwide)             │
│                                                               │
│  ┌───────────┐  ┌───────────┐  ┌───────────┐               │
│  │  Node 1   │  │  Node 2   │  │  Node 3   │               │
│  │ (Your PC) │  │ (Infura)  │  │ (Alchemy) │               │
│  └─────┬─────┘  └─────┬─────┘  └─────┬─────┘               │
│        │              │              │                       │
│        └──────────────┼──────────────┘                       │
│                       │                                      │
│              [Blockchain] (Immutable)                        │
│              Block 1 → Block 2 → Block 3 → ...              │
│                (All nodes have same copy)                    │
└─────────────────────────────────────────────────────────────┘
```

### Key Concepts

| Term | What It Is | Example |
|------|-----------|---------|
| **Block** | A batch of transactions + timestamp + hash | Height 5,000,000 |
| **Block Hash** | Unique identifier for a block | `0x1a2b3c...` |
| **Transaction (TX)** | A record of value transfer | Sending 1 ETH to friend |
| **Gas** | Fee to execute transactions | 21,000 gas for ETH transfer |
| **Wei** | Smallest unit of ETH (like cents) | 1 ETH = 10^18 wei |

---

## 👛 Wallets & Addresses

### What is a Wallet?

A **wallet** is:
- A pair of keys: public key (shareable) + private key (secret)
- Used to send transactions and prove ownership
- Not stored "on chain" - it's just cryptography

### Ethereum Addresses

An Ethereum address is a **42-character hexadecimal string**:
- Starts with `0x`
- Derived from your public key using cryptography
- Example: `0x742d35Cc6634C0532925a3b844Bc9e7595f42e61`

### Checksum Addresses

Modern Ethereum uses **EIP-55 checksums**:

```
Regular:     0x742d35cc6634c0532925a3b844bc9e7595f42e61
Checksum:    0x742d35Cc6634C0532925a3b844Bc9e7595f42e61  ← Mixed case
                         ^^^ ^^  ^^   ^
```

The mixed case is a checksum - copying wrong addresses won't work!

### Address Validation in Code

This project validates addresses using Nethereum:

```csharp
// In EthereumService.cs
public bool IsValidAddress(string address)
{
    return address.IsValidEthereumAddressHexFormat();
}
```

---

## ⛽ Gas & Transactions

### What is Gas?

Gas is **fuel for transactions**. You pay gas to execute anything on Ethereum.

```
┌─────────────────────────────────────────────┐
│  Transaction Cost = Gas Used × Gas Price    │
├─────────────────────────────────────────────┤
│  Example:                                    │
│  - Gas Used: 21,000 (ETH transfer)           │
│  - Gas Price: 10 gwei                        │
│  - Cost: 21,000 × 10 gwei = 0.00021 ETH      │
└─────────────────────────────────────────────┘
```

### Transaction Types

| Type | What It Does | Gas Cost |
|------|-------------|----------|
| **Transfer ETH** | Send ether to another address | ~21,000 gas |
| **Contract Call** | Execute smart contract function | Varies (1000-millions) |
| **Contract Deploy** | Create new smart contract | ~200,000+ gas |

### Transaction Status

After a transaction is mined:
- **Status = 1**: ✅ Success
- **Status = 0**: ❌ Failed (but gas was still paid)

In the database (`Transaction.cs`):
```csharp
public int? Status { get; set; }  // 1 = success, 0 = failed
```

---

## 🔧 Smart Contracts & ERC20

### What is a Smart Contract?

A smart contract is **code that runs on Ethereum**:
- Written in Solidity (looks like JavaScript)
- Stored on blockchain at an address
- Immutable (can't be changed after deploy)

### ERC20 Standard

**ERC20** is the standard for fungible tokens (like money):

```solidity
// Simplified ERC20 interface
contract ERC20 {
    function balanceOf(address owner) returns (uint256);
    function transfer(address to, uint256 amount) returns (bool);
    function approve(address spender, uint256 amount) returns (bool);
    function transferFrom(address from, address to, uint256 amount) returns (bool);
}
```

### Example ERC20 Tokens on Sepolia

| Token | Address | Decimals | Use Case |
|-------|---------|----------|----------|
| USDC | `0x1c7D4B196Cb0C7B01d743Fbc6116a902379C7238` | 6 | Stablecoin |
| DAI | `0x7af963cF6D228E564007A5b0f624F51F6b7a07A3` | 18 | Stablecoin |
| WETH | `0x7b79995e5f793a07bc00c21412e50ecae098e7f9` | 18 | Wrapped ETH |

### Decimals Explained

Tokens have decimal places, just like currencies:

```
Raw balance:     1000000000000000000
Token decimals:  18
Human readable:  1.0 (in USDC with 6 decimals: 0.000001)

Formula: Readable = Raw ÷ (10 ^ decimals)
```

In code (`WalletService.cs`):
```csharp
private string FormatBalance(string balance, int decimals)
{
    var divisor = BigInteger.Pow(10, decimals);
    var whole = bigBalance / divisor;
    var remainder = bigBalance % divisor;
    // Return formatted string like "1.5"
}
```

---

## 🌐 Nethereum Integration

### What is Nethereum?

Nethereum is a **.NET library** that:
- Connects to Ethereum nodes via RPC
- Handles Web3 operations easily
- Abstracts away complex cryptography

### RPC (Remote Procedure Call)

Your app talks to Ethereum through **RPC calls**:

```
Your API  →  Nethereum  →  RPC Endpoint (Infura/Alchemy)  →  Ethereum Node
(C#)        (Wrapper)      (JSON-RPC)                        (Blockchain)
```

### Nethereum Setup in This Project

**In `Program.cs`:**
```csharp
// Get RPC URL from config
var rpcUrl = builder.Configuration.GetSection("Ethereum:RpcUrl").Value;

// Register Ethereum service
builder.Services.AddScoped<IEthereumService>(provider => 
    new EthereumService(rpcUrl)
);
```

**In `appsettings.json`:**
```json
{
  "Ethereum": {
    "RpcUrl": "https://sepolia.infura.io/v3/YOUR_KEY",
    "NetworkName": "Sepolia Testnet"
  }
}
```

### Common Nethereum Operations

#### 1. Connect to the Network

```csharp
var web3 = new Web3("https://sepolia.infura.io/v3/...");
```

#### 2. Get ETH Balance

```csharp
var balance = await web3.Eth.GetBalance.SendRequestAsync(address);
// Returns BigInteger in wei
```

#### 3. Get Contract Function

```csharp
var contract = web3.Eth.GetContract(ABI, contractAddress);
var balanceOfFunc = contract.GetFunction("balanceOf");
var balance = await balanceOfFunc.CallAsync<BigInteger>(walletAddress);
```

#### 4. Call Read Function (No gas required)

```csharp
var nameFunc = contract.GetFunction("name");
var name = await nameFunc.CallAsync<string>();  // Just reads, doesn't change
```

---

## 📊 Data Formatting

### Wei to ETH Conversion

Ethereum internally uses **wei** (smallest unit):

```
1 ETH = 1,000,000,000,000,000,000 wei (10^18)
1 gwei = 1,000,000,000 wei (10^9)
1 wei = 1 wei
```

### Converting Wei to Readable

```csharp
// Raw from blockchain
BigInteger weiAmount = 1000000000000000000;  // 1 ETH

// Decimals (ETH has 18)
int decimals = 18;

// Convert
var readable = weiAmount / (10 ^ decimals);  // = 1.0
```

### Token Decimal Examples

```
USDC (6 decimals):
  1 USDC = 1,000,000 raw units
  Raw: 1000000 → Readable: 1.0

DAI (18 decimals):
  1 DAI = 1,000,000,000,000,000,000 raw units
  Raw: 1000000000000000000 → Readable: 1.0
```

---

## 🔄 Workflow: How Your API Works

### Step 1: Add a Wallet

```
User sends: POST /api/wallets/add
  ↓
WalletService.AddOrGetWalletAsync()
  ↓
[Check if valid Ethereum address] ← EthereumService.IsValidAddress()
  ↓
[Query database for existing wallet]
  ↓
[If new, create and save to DB]
  ↓
[Fetch current ETH balance from blockchain] ← EthereumService.GetEthBalanceAsync()
  ↓
Return wallet details
```

### Step 2: Add a Token

```
User sends: POST /api/wallets/{addr}/tokens/add?tokenAddress=0x...
  ↓
WalletService.AddTokenAsync()
  ↓
[Validate addresses]
  ↓
[Get token details] ← EthereumService.GetErc20TokenDetailsAsync()
  ├─ name
  ├─ symbol
  └─ decimals
  ↓
[Get current balance] ← EthereumService.GetErc20BalanceAsync()
  ↓
[Format balance] ← Decimal conversion with decimals
  ↓
[Save to TokenBalances table]
  ↓
Return updated wallet
```

### Step 3: Sync Balances

```
User sends: POST /api/wallets/{addr}/sync
  ↓
WalletService.SyncWalletBalancesAsync()
  ↓
For each tracked token:
  ├─ Get new balance from blockchain
  ├─ Format it
  └─ Update database
  ↓
Update wallet.LastSyncedAt
  ↓
Return wallet
```

---

## 🧪 Testing Web3 Code

### Mock Ethereum Calls

In `WalletServiceTests.cs`:

```csharp
// Mock the Ethereum service
var ethereumServiceMock = new Mock<IEthereumService>();

ethereumServiceMock
    .Setup(x => x.GetEthBalanceAsync("0x123..."))
    .ReturnsAsync("1000000000000000000");  // 1 ETH

// Now WalletService can be tested without real RPC calls
```

### Why Mock?

✅ **No testnet ETH needed**
✅ **Tests run fast**
✅ **Predictable results**
❌ **Doesn't test real blockchain**

---

## 🚀 Next Steps to Learn

1. **Deploy a simple contract:**
   - Try Remix IDE: https://remix.ethereum.org/

2. **Understand transactions deeply:**
   - Read: https://ethereum.org/en/developers/docs/transactions/

3. **Explore more Nethereum examples:**
   - Docs: https://docs.nethereum.com/

4. **Add transaction history sync:**
   - Use Etherscan API or Alchemy
   - Fetch past transactions for a wallet

5. **Build contract interactions:**
   - Learn to call write functions (requires signatures)
   - Implement approval patterns for tokens

6. **Mainnet deployment:**
   - Test on Sepolia first ✅
   - Then deploy to Ethereum mainnet

---

## 📖 Reference Resources

### Ethereum Concepts
- **Official Docs:** https://ethereum.org/en/developers/docs/
- **Whitepaper:** https://ethereum.org/en/whitepaper/ (technical)
- **Etherscan:** https://etherscan.io/ (block explorer)

### Nethereum
- **GitHub:** https://github.com/Nethereum/Nethereum
- **Docs:** https://docs.nethereum.com/
- **Examples:** https://github.com/Nethereum/Nethereum/tree/master/src/Nethereum.Workbook

### ERC20 Standard
- **Specification:** https://eips.ethereum.org/EIPS/eip-20
- **Openzeppelin Implementation:** https://github.com/OpenZeppelin/openzeppelin-contracts

### Tools
- **Sepolia Faucet:** https://sepoliafaucet.com/
- **Sepolia Explorer:** https://sepolia.etherscan.io/
- **Infura:** https://www.infura.io/ (free RPC provider)

---

**Happy learning! Questions? Start with the README.md and work your way through this guide. 🚀**
