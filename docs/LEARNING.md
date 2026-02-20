# 🎓 LEARNING.md - Web3 Concepts for .NET Developers

This guide explains blockchain and Ethereum concepts as they apply to this .NET project. Read through these sections sequentially for a comprehensive understanding.

## Table of Contents

1. [Blockchain Basics](#blockchain-basics)
2. [Ethereum 101](#ethereum-101)
3. [Wallets & Addresses](#wallets--addresses)
4. [Transactions](#transactions)
5. [Smart Contracts & ERC20](#smart-contracts--erc20)
6. [Nethereum Integration](#nethereum-integration)
7. [Sepolia Testnet](#sepolia-testnet)
8. [Gas & Costs](#gas--costs)

---

## Blockchain Basics

### What is Blockchain?

A blockchain is a **distributed ledger** - a record of transactions maintained across many computers (nodes). Think of it like a shared database, but:

- ✅ No central authority controls it
- ✅ Every change is cryptographically signed
- ✅ History is immutable (can't change past records)
- ✅ Everyone has a copy

### Key Concepts

| Concept | Analogy | In Ethereum |
|---------|---------|------------|
| **Block** | Page in a ledger | ~15 sec, contains ~150 transactions |
| **Hash** | Fingerprint | 64-char hex identifying the block |
| **Mining** | Solving a puzzle | Validators secure the network |
| **Gas** | Fuel for computation | Measured in tiny units (wei) |

### Consensus Mechanism

Ethereum uses **Proof of Stake (PoS)**:
- Validators lock up ETH as collateral
- They propose and validate blocks
- Malicious behavior costs them money (slashing)
- Rewards incentivize honest participation

---

## Ethereum 101

### What is Ethereum?

Ethereum is a **programmable blockchain**. While Bitcoin only handles payments, Ethereum can run arbitrary code (smart contracts).

```
Bitcoin:     "Digital money"
Ethereum:    "World computer with a cryptocurrency"
```

### ETH vs Ether vs Ethereum

| Term | Meaning |
|------|---------|
| **Ethereum** | The blockchain network |
| **ETH** | The currency (ticker symbol) |
| **Ether** | The actual currency unit |
| **1 ETH** | = 10^18 Wei (smallest unit) |

### The Ethereum Network

```
┌─────────────────────────────────────┐
│  Ethereum Mainnet (Production)      │
│  Real ETH, Real money               │
└─────────────────────────────────────┘
         ↓    ↓    ↓
┌─────────────────────────────────────┐
│  Testnet: Sepolia (Learning)        │
│  Free ETH, No real value            │
└─────────────────────────────────────┘
```

**Our Project Uses:** Sepolia Testnet (safe for learning!)

---

## Wallets & Addresses

### What is a Wallet?

A **wallet** is software that manages your keys and transactions. It doesn't store your funds—the blockchain does. Your wallet just proves you own them.

```
Wallet ≈ Your online banking app
ETH on-chain ≈ Your actual bank account
```

### Ethereum Addresses

An **Ethereum address** is:
- 42 characters long
- Starts with `0x`
- 40 hexadecimal characters
- Example: `0x742d35Cc6634C0532925a3b844Bc9e7595f42bA0`

**Two Types:**

```
1. EOA (Externally Owned Account)
   ├─ Controlled by private key
   ├─ Can initiate transactions
   └─ No code attached
   
2. Contract Account
   ├─ Contains smart contract code
   ├─ Has no private key
   └─ Executed when called
```

### Public vs Private Keys

```
Private Key:  Your secret password (64 hex chars)
              🔒 NEVER share this!
              
Public Key:   Derived from private key
              ✅ Safe to share
              
Address:      Hash of public key
              ✅ Your wallet address
              
Math:  Address = Hash(PublicKey) = Hash(Sign(PrivateKey))
```

**In Our Code:**

```csharp
// Validate address format
var isValid = AddressUtil.Current.IsValidEthereumAddressHexFormat(address);

// Checksum address (correct capitalization)
var checksum = AddressUtil.Current.ToChecksumAddress(address);
```

---

## Transactions

### Transaction Anatomy

Every transaction has:

```
{
  "from":     "0x742d35Cc...",    // Who's sending
  "to":       "0x123456...",      // Who's receiving
  "value":    "1000000000000000000",  // Amount in wei (1 ETH)
  "gas":      "21000",            // Computation cost limit
  "gasPrice": "50000000000",      // Price per gas unit (wei)
  "nonce":    "5",                // Transaction counter
  "data":     "0x",               // Contract call data (empty for ETH transfer)
  "hash":     "0xabc123..."       // Transaction ID (created when mined)
}
```

### Transaction Lifecycle

```
1. CREATE
   User creates & signs transaction
   
2. BROADCAST
   Sent to network, in mempool
   
3. PENDING
   Waiting for validator to include in block
   
4. MINED
   Included in block, 1 confirmation
   
5. CONFIRMED
   Multiple blocks after (3+ for safety)
   
6. FINALIZED
   Immutable, cannot be reversed
```

### Gas Explained

**Gas** is the cost of computation:

```
Concept:    Gas = Fuel for transactions
            Like driving: More distance = More fuel

Example:
  - Simple ETH transfer = 21,000 gas
  - ERC20 token transfer = 65,000 gas
  - Uniswap swap = 150,000+ gas
  
Cost = Gas Used × Gas Price
     = 21,000 × 50 Gwei
     = 1,050,000 Gwei
     = 0.00105 ETH (≈ $2-3 on mainnet)
```

**In Our Code:**

```csharp
// Get transaction gas info
var tx = await web3.Eth.Transactions.GetTransactionByHash
    .SendRequestAsync(transactionHash);

var gasUsed = tx.Gas.Value;
var gasPrice = tx.GasPrice.Value;
var totalCost = gasUsed * gasPrice;
var totalCostEth = UnitConversion.Convert.FromWei(totalCost);
```

---

## Smart Contracts & ERC20

### What is a Smart Contract?

A **smart contract** is code that runs on the blockchain:

- ✅ Immutable (can't change after deploy)
- ✅ Transparent (everyone can read it)
- ✅ Deterministic (same input = same output)
- ✅ Trustless (no intermediary needed)

```
Traditional:  "Trust a bank to handle your money"
Smart Contract: "Code handles your money, math is trustless"
```

### The SimpleToken Contract (Our Example)

```solidity
// SPDX-License-Identifier: MIT
pragma solidity ^0.8.0;

contract SimpleToken {
    // Balances for each address
    mapping(address => uint256) public balances;
    
    constructor(uint256 initialSupply) {
        balances[msg.sender] = initialSupply * 10**18;
    }
    
    // Transfer tokens
    function transfer(address to, uint256 amount) public {
        require(balances[msg.sender] >= amount);
        balances[msg.sender] -= amount;
        balances[to] += amount;
    }
    
    // Check balance
    function balanceOf(address account) public view returns (uint256) {
        return balances[account];
    }
}
```

**Key Features:**
- `mapping` = Like a dictionary/hashmap
- `msg.sender` = Caller's address
- `public` = Anyone can call
- `view` = Read-only, costs no gas

### ERC20 Token Standard

**ERC20** is the standard for fungible tokens (like coins).

```
What's a "standard"?
  = Common interface all tokens follow
  = Like USB: Any USB device works with any USB port
```

**Required Functions:**

```solidity
function transfer(address to, uint256 amount) public returns (bool)
function balanceOf(address account) public view returns (uint256)
function approve(address spender, uint256 amount) public returns (bool)
function transferFrom(address from, address to, uint256 amount) public returns (bool)
function allowance(address owner, address spender) public view returns (uint256)

// Optional but common:
function name() public view returns (string)
function symbol() public view returns (string)
function decimals() public view returns (uint8)
function totalSupply() public view returns (uint256)
```

**In Our .NET Code:**

```csharp
// Nethereum has built-in ERC20 functions
var balanceOfFunction = new BalanceOfFunction { Owner = walletAddress };
var handler = web3.Eth.GetContractQueryHandler<BalanceOfFunction>();
var balance = await handler.QueryAsync<BigInteger>(contractAddress, balanceOfFunction);
```

---

## Nethereum Integration

### What is Nethereum?

**Nethereum** is the .NET library for Ethereum interaction. It wraps:
- JSON-RPC calls to the network
- Contract interaction (ABI encoding/decoding)
- Key management
- Transaction signing

### Key Nethereum Concepts

#### 1. Web3 Instance

```csharp
// Connect to Ethereum via RPC provider
var web3 = new Web3("https://sepolia.infura.io/v3/YOUR_KEY");

// Now you can call Ethereum methods
var blockNumber = await web3.Eth.Blocks.GetBlockNumber.SendRequestAsync();
var balance = await web3.Eth.GetBalance.SendRequestAsync("0x742d35Cc...");
```

#### 2. Contract Functions

```csharp
// Read-only function (call)
var handler = web3.Eth.GetContractQueryHandler<BalanceOfFunction>();
var balance = await handler.QueryAsync<BigInteger>(
    contractAddress: "0xabc123...",
    function: new BalanceOfFunction { Owner = walletAddress }
);

// State-changing function (transaction)
// Requires private key to sign!
```

#### 3. Unit Conversions

```csharp
// Wei ↔ Ether conversion
var balanceWei = BigInteger.Parse("1000000000000000000");
var balanceEth = UnitConversion.Convert.FromWei(balanceWei);  // 1 ETH

// Gwei (common for gas prices)
var gasPriceGwei = UnitConversion.Convert.FromWei(gasPrice, UnitConversion.EthUnit.Gwei);
```

### Our Implementation Pattern

```csharp
// Service layer handles Nethereum
public class WalletService
{
    private readonly IWeb3Service _web3Service;
    
    public async Task<string> GetEthBalanceAsync(string address)
    {
        var web3 = _web3Service.GetWeb3Instance();
        var balanceWei = await web3.Eth.GetBalance.SendRequestAsync(address);
        return UnitConversion.Convert.FromWei(balanceWei).ToString();
    }
}

// Dependency injection (from Program.cs)
builder.Services.AddScoped<IWalletService, WalletService>();
```

---

## Sepolia Testnet

### Why Testnet?

| Mainnet | Testnet (Sepolia) |
|---------|-------------------|
| Real ETH | Free test ETH |
| Real money | No real value |
| Live users | Testing only |
| Irreversible | Can reset |

**Use Sepolia to learn safely!**

### Sepolia Details

```
Network ID:    11155111
RPC URL:       https://sepolia.infura.io/v3/YOUR_KEY
Explorer:      https://sepolia.etherscan.io/
Faucet:        https://www.sepoliafaucet.com/
Block Time:    ~12 seconds
```

### Getting Test ETH

1. Go to [Sepolia Faucet](https://www.sepoliafaucet.com/)
2. Enter your wallet address
3. Get free test ETH (usually 0.5-1 ETH)
4. Use it for transactions
5. No real money involved!

---

## Gas & Costs

### Gas Mechanics

```
Real-world analogy:
  Gas = Electricity meter
  Gas Price = $/kWh rate
  Gas Limit = Maximum spending limit

Ethereum:
  Gas = Computation units
  Gas Price = Wei per unit
  Gas Limit = Max gas sender willing to spend
  
Total Cost = Gas Used × Gas Price
```

### Estimating Gas

```csharp
// Get gas estimate for a transaction
var gasEstimate = await web3.Eth.GetGasPrice.SendRequestAsync();

// For contract calls, estimate gas:
var gasPrice = await web3.Eth.GetGasPrice.SendRequestAsync();

// Rule of thumb:
// - Simple transfer: 21,000 gas
// - Token transfer: 65,000 gas
// - Swap on DEX: 150,000+ gas
```

### Current Gas Prices

On Sepolia (usually cheap for testing):

```
Standard:  1-5 Gwei
Fast:      5-10 Gwei
Instant:   10+ Gwei

Calculation:
  Cost = 21,000 gas × 2 Gwei = 42,000 Gwei = 0.000042 ETH
```

---

## Quick Reference

### Common Address Examples (Sepolia Testnet)

```
Your test address:     0x1234567890123456789012345678901234567890
Token contract:        0xabc123...
Your private key:      🔒 Keep secret!
```

### Common Nethereum Calls

```csharp
// Get balance
await web3.Eth.GetBalance.SendRequestAsync(address);

// Get transaction
await web3.Eth.Transactions.GetTransactionByHash.SendRequestAsync(hash);

// Get block
await web3.Eth.Blocks.GetBlockByNumber.SendRequestAsync(blockNumber);

// Send transaction (requires private key)
await web3.Eth.TransactionManager.SendTransactionAsync(...)

// Call contract function
await handler.QueryAsync<T>(contractAddress, function);
```

### Error Codes

```
"Invalid address"       → Wrong format or checksum
"Insufficient balance"  → Not enough funds
"Gas limit exceeded"    → Transaction too expensive
"Nonce too low"         → Out of order (retry)
"Connection refused"    → RPC endpoint down
```

---

## Further Learning

### Next Topics to Explore

1. **Private Key Management** - How to safely handle keys
2. **Contract Deployment** - Deploy your own contracts
3. **Event Listeners** - React to contract events
4. **Token Standards** - ERC721 (NFTs), ERC1155 (Multi-token)
5. **DeFi Protocols** - Uniswap, AAVE, etc.

### External Resources

- [Ethereum.org Documentation](https://ethereum.org/en/developers/docs/)
- [Solidity by Example](https://solidity-by-example.org/)
- [OpenZeppelin Contracts](https://docs.openzeppelin.com/contracts/)
- [Nethereum Docs](https://docs.nethereum.com/)

---

**Remember:** Blockchain is complex, but understanding it deeply is valuable. Take your time, experiment on testnet, and read the code! 🚀
