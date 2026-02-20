# API Testing Guide - Postman Collection

This guide provides curl commands and Postman examples for testing all API endpoints.

## Quick Start

### Option A: Using Curl

```bash
# Health check
curl http://localhost:5000/api/health

# Validate wallet
curl http://localhost:5000/api/wallet/validate/0x742d35Cc6634C0532925a3b844Bc9e7595f42bA0

# Get ETH balance
curl http://localhost:5000/api/wallet/0x742d35Cc6634C0532925a3b844Bc9e7595f42bA0/eth-balance
```

### Option B: Swagger UI (Easiest)

1. Run the application: `dotnet run`
2. Open browser: `http://localhost:5000/swagger`
3. Try endpoints directly from the UI

---

## Endpoint Examples

### 1. Health Check

**Request:**
```bash
curl -X GET "http://localhost:5000/api/health"
```

**Response:**
```json
{
  "status": "healthy",
  "timestamp": "2024-02-19T10:30:00Z",
  "service": "Web3 Dashboard API"
}
```

### 2. Network Status

**Request:**
```bash
curl -X GET "http://localhost:5000/api/health/network"
```

**Response (Connected):**
```json
{
  "status": "connected",
  "networkId": "11155111",
  "timestamp": "2024-02-19T10:30:00Z"
}
```

### 3. Validate Wallet Address

**Request:**
```bash
curl -X GET "http://localhost:5000/api/wallet/validate/0x742d35Cc6634C0532925a3b844Bc9e7595f42bA0"
```

**Response (Valid Address):**
```json
{
  "address": "0x742d35Cc6634C0532925a3b844Bc9e7595f42bA0",
  "isValid": true,
  "message": "EOA (Externally Owned Account)"
}
```

**Response (Contract Address):**
```json
{
  "address": "0x1f9840a85d5af5bf1d1762f925bdaddc4201f984",
  "isValid": true,
  "message": "Smart contract detected"
}
```

**Response (Invalid Address):**
```json
{
  "address": "not-an-address",
  "isValid": false,
  "message": "Invalid Ethereum address format (must be 0x + 40 hex characters)"
}
```

### 4. Get ETH Balance

**Request:**
```bash
curl -X GET "http://localhost:5000/api/wallet/0x742d35Cc6634C0532925a3b844Bc9e7595f42bA0/eth-balance"
```

**Response:**
```json
{
  "address": "0x742d35Cc6634C0532925a3b844Bc9e7595f42bA0",
  "balance": "2.5",
  "unit": "ETH"
}
```

### 5. Get Wallet Balance (with Tokens)

**Request:**
```bash
curl -X GET "http://localhost:5000/api/wallet/0x742d35Cc6634C0532925a3b844Bc9e7595f42bA0/balance"
```

**Response:**
```json
{
  "address": "0x742d35Cc6634C0532925a3b844Bc9e7595f42bA0",
  "ethBalance": "2.5",
  "tokens": [
    {
      "contractAddress": "0x7af173f350d916b7e4553b2b5e2b9150bffb7d42",
      "symbol": "USDC",
      "balance": "1000000000000000000",
      "decimals": 6,
      "formattedBalance": "1000000"
    }
  ]
}
```

### 6. Get Transaction History

**Request:**
```bash
curl -X GET "http://localhost:5000/api/transaction/wallet/0x742d35Cc6634C0532925a3b844Bc9e7595f42bA0?limit=5"
```

**Response:**
```json
{
  "address": "0x742d35Cc6634C0532925a3b844Bc9e7595f42bA0",
  "transactions": [
    {
      "hash": "0xabc123...",
      "from": "0x742d35Cc...",
      "to": "0xdef456...",
      "value": "1000000000000000000",
      "gasPrice": "50000000000",
      "gas": "21000",
      "status": "success",
      "timestamp": "2024-02-19T10:00:00Z"
    }
  ]
}
```

### 7. Get Transaction Details

**Request:**
```bash
curl -X GET "http://localhost:5000/api/transaction/0xabc123def456..."
```

**Response:**
```json
{
  "hash": "0xabc123def456...",
  "from": "0x742d35Cc6634C0532925a3b844Bc9e7595f42bA0",
  "to": "0xdef4567890abcdef...",
  "value": "1000000000000000000",
  "gasPrice": "50000000000",
  "gas": "21000",
  "status": "success",
  "timestamp": "2024-02-19T10:00:00Z"
}
```

### 8. Get ERC20 Token Info

**Request:**
```bash
curl -X GET "http://localhost:5000/api/contract/erc20/0x7af173f350d916b7e4553b2b5e2b9150bffb7d42/info"
```

**Response:**
```json
{
  "contractAddress": "0x7af173f350d916b7e4553b2b5e2b9150bffb7d42",
  "name": "USD Coin",
  "symbol": "USDC",
  "decimals": 6
}
```

### 9. Get ERC20 Balance

**Request:**
```bash
curl -X GET "http://localhost:5000/api/contract/erc20/0x7af173f350d916b7e4553b2b5e2b9150bffb7d42/balance/0x742d35Cc6634C0532925a3b844Bc9e7595f42bA0"
```

**Response:**
```json
{
  "contractAddress": "0x7af173f350d916b7e4553b2b5e2b9150bffb7d42",
  "walletAddress": "0x742d35Cc6634C0532925a3b844Bc9e7595f42bA0",
  "balance": "1000000000000000000"
}
```

### 10. Call Contract Function

**Request:**
```bash
curl -X POST "http://localhost:5000/api/contract/call?contractAddress=0x7af173f350d916b7e4553b2b5e2b9150bffb7d42&functionName=name" \
  -H "Content-Type: application/json" \
  -d '[]'
```

**Response:**
```json
{
  "success": true,
  "result": "USD Coin",
  "error": null
}
```

---

## Common Test Addresses (Sepolia Testnet)

| Name | Address | Type |
|------|---------|------|
| USDC | `0x7af173f350d916b7e4553b2b5e2b9150bffb7d42` | Token |
| WETH | `0x7b79995e5f793A07Bc00c21412e50Ecae098E7f9` | Token |
| Uniswap Router | `0xC532a74256cb655e1b582F08Faeb0A7B72c756c0` | Contract |

---

## Postman Collection Import

### JSON Collection (Save as `Web3Dashboard.postman_collection.json`)

```json
{
  "info": {
    "name": "Web3 Dashboard API",
    "version": "1.0.0"
  },
  "auth": {},
  "servers": [
    {
      "url": "http://localhost:5000"
    }
  ],
  "item": [
    {
      "name": "Health",
      "item": [
        {
          "name": "Health Check",
          "request": {
            "method": "GET",
            "url": {
              "raw": "{{base_url}}/api/health",
              "host": ["{{base_url}}"],
              "path": ["api", "health"]
            }
          }
        },
        {
          "name": "Network Status",
          "request": {
            "method": "GET",
            "url": {
              "raw": "{{base_url}}/api/health/network",
              "host": ["{{base_url}}"],
              "path": ["api", "health", "network"]
            }
          }
        }
      ]
    },
    {
      "name": "Wallet",
      "item": [
        {
          "name": "Validate Wallet",
          "request": {
            "method": "GET",
            "url": {
              "raw": "{{base_url}}/api/wallet/validate/{{wallet_address}}",
              "host": ["{{base_url}}"],
              "path": ["api", "wallet", "validate", "{{wallet_address}}"]
            }
          }
        },
        {
          "name": "Get ETH Balance",
          "request": {
            "method": "GET",
            "url": {
              "raw": "{{base_url}}/api/wallet/{{wallet_address}}/eth-balance",
              "host": ["{{base_url}}"],
              "path": ["api", "wallet", "{{wallet_address}}", "eth-balance"]
            }
          }
        },
        {
          "name": "Get Wallet Balance",
          "request": {
            "method": "GET",
            "url": {
              "raw": "{{base_url}}/api/wallet/{{wallet_address}}/balance",
              "host": ["{{base_url}}"],
              "path": ["api", "wallet", "{{wallet_address}}", "balance"]
            }
          }
        }
      ]
    }
  ],
  "variable": [
    {
      "key": "base_url",
      "value": "http://localhost:5000"
    },
    {
      "key": "wallet_address",
      "value": "0x742d35Cc6634C0532925a3b844Bc9e7595f42bA0"
    }
  ]
}
```

### Import Steps:

1. Open Postman
2. Click "Import" (top left)
3. Select "Raw text"
4. Paste the JSON above
5. Click "Import"
6. Use `base_url` and `wallet_address` variables

---

## Testing Workflow

### Step 1: Verify Connection

```bash
curl http://localhost:5000/api/health/network
```

### Step 2: Test with Known Address

```bash
curl http://localhost:5000/api/wallet/0x0000000000000000000000000000000000000000/eth-balance
```

### Step 3: Test with Your Address

Replace with a real Sepolia address that has test ETH.

### Step 4: Test Token Interaction

Use a known token address (USDC on Sepolia).

---

## Error Responses

### Invalid Address Format

```json
{
  "error": "Invalid Ethereum address format (must be 0x + 40 hex characters)"
}
```

### Network Disconnected

```json
{
  "status": "disconnected",
  "error": "Cannot connect to Ethereum RPC"
}
```

### Transaction Not Found

```json
{
  "error": "Transaction not found"
}
```

---

## Tips for Testing

1. **Always validate first:** Call validate endpoint before other queries
2. **Use Sepolia addresses:** Test addresses should be on Sepolia testnet
3. **Check gas prices:** Use `/api/health/network` to verify connectivity
4. **Log responses:** Save responses for debugging
5. **Rate limit yourself:** Don't spam requests to RPC

---

Happy testing! 🚀
