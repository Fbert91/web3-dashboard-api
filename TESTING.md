# Testing Guide - Web3 Dashboard API v2.0

Complete testing guide for the Web3 Dashboard API v2.0, including unit tests, integration tests, and manual testing procedures.

## Running Tests

### Run All Tests
```bash
cd /root/.openclaw/workspace/Web3DashboardAPI
dotnet test
```

### Run Specific Test Suite
```bash
# Unit tests
dotnet test tests/Web3DashboardAPI.Tests.Unit

# Integration tests
dotnet test tests/Web3DashboardAPI.Tests.Integration

# With verbose output
dotnet test -v normal

# With coverage
dotnet test /p:CollectCoverage=true /p:CoverageFormat=opencover
```

## Unit Tests

### 1. Address Validation Tests
**File**: `tests/Web3DashboardAPI.Tests.Unit/Utilities/AddressValidatorTests.cs`

**Test Cases**:
```csharp
[Fact]
public void IsValidAddress_ValidAddress_ReturnsTrue()
{
    // Arrange
    var address = "0x742d35Cc6634C0532925a3b844Bc9e7595f42e61";
    
    // Act
    var result = AddressValidator.IsValidAddress(address);
    
    // Assert
    Assert.True(result);
}

[Fact]
public void IsValidAddress_InvalidChecksum_ReturnsFalse()
{
    // Arrange
    var address = "0x742d35cc6634C0532925a3b844Bc9e7595f42e61"; // lowercase c instead of C
    
    // Act
    var result = AddressValidator.IsValidAddress(address);
    
    // Assert
    Assert.False(result);
}

[Theory]
[InlineData(null)]
[InlineData("")]
[InlineData("invalid")]
[InlineData("0x123")]
public void IsValidAddress_InvalidFormats_ReturnsFalse(string address)
{
    // Act
    var result = AddressValidator.IsValidAddress(address);
    
    // Assert
    Assert.False(result);
}
```

### 2. Input Validation Tests
**File**: `tests/Web3DashboardAPI.Tests.Unit/Validation/InputValidatorTests.cs`

**Test Cases**:
```csharp
[Fact]
public void ValidateModel_ValidRequest_NoException()
{
    // Arrange
    var request = new AddWalletRequest { Address = "0x742d35Cc6634C0532925a3b844Bc9e7595f42e61" };
    
    // Act & Assert
    InputValidator.ValidateModel(request); // Should not throw
}

[Fact]
public void ValidateModel_InvalidAddress_ThrowsValidationException()
{
    // Arrange
    var request = new AddWalletRequest { Address = "invalid" };
    
    // Act & Assert
    Assert.Throws<ValidationException>(() => InputValidator.ValidateModel(request));
}
```

### 3. Retry Policy Tests
**File**: `tests/Web3DashboardAPI.Tests.Unit/Utilities/RetryPolicyTests.cs`

**Test Cases**:
```csharp
[Fact]
public async Task ExecuteAsync_SucceedsOnFirstAttempt_ReturnsResult()
{
    // Arrange
    var options = new RetryOptions { MaxRetries = 3 };
    var retryPolicy = new RetryPolicy(options, logger);
    var callCount = 0;
    
    // Act
    var result = await retryPolicy.ExecuteAsync(async () =>
    {
        callCount++;
        return "success";
    });
    
    // Assert
    Assert.Equal(1, callCount);
    Assert.Equal("success", result);
}

[Fact]
public async Task ExecuteAsync_FailsThenSucceeds_Retries()
{
    // Arrange
    var options = new RetryOptions { MaxRetries = 3, InitialDelayMs = 10 };
    var retryPolicy = new RetryPolicy(options, logger);
    var callCount = 0;
    
    // Act
    var result = await retryPolicy.ExecuteAsync(async () =>
    {
        callCount++;
        if (callCount < 3)
            throw new HttpRequestException();
        return "success";
    });
    
    // Assert
    Assert.Equal(3, callCount);
    Assert.Equal("success", result);
}

[Fact]
public async Task ExecuteAsync_ExceedsMaxRetries_ThrowsException()
{
    // Arrange
    var options = new RetryOptions { MaxRetries = 3, InitialDelayMs = 10 };
    var retryPolicy = new RetryPolicy(options, logger);
    
    // Act & Assert
    await Assert.ThrowsAsync<HttpRequestException>(async () =>
    {
        await retryPolicy.ExecuteAsync(async () =>
        {
            throw new HttpRequestException("Network error");
        });
    });
}
```

### 4. Circuit Breaker Tests
**File**: `tests/Web3DashboardAPI.Tests.Unit/Utilities/CircuitBreakerTests.cs`

**Test Cases**:
```csharp
[Fact]
public async Task ExecuteAsync_InitiallyClosedAndSucceeds_StaysClosed()
{
    // Arrange
    var options = new CircuitBreakerOptions();
    var breaker = new CircuitBreaker(options, logger);
    
    // Act
    await breaker.ExecuteAsync(async () => "success");
    
    // Assert
    Assert.Equal(CircuitState.Closed, breaker.State);
}

[Fact]
public async Task ExecuteAsync_FailureThresholdExceeded_Opens()
{
    // Arrange
    var options = new CircuitBreakerOptions { FailureThreshold = 3, TimeoutMs = 100 };
    var breaker = new CircuitBreaker(options, logger);
    
    // Act
    for (int i = 0; i < 3; i++)
    {
        try
        {
            await breaker.ExecuteAsync(async () => throw new Exception("Error"));
        }
        catch { }
    }
    
    // Assert
    Assert.Equal(CircuitState.Open, breaker.State);
}

[Fact]
public async Task ExecuteAsync_CircuitOpen_ThrowsException()
{
    // Arrange
    var options = new CircuitBreakerOptions { FailureThreshold = 1 };
    var breaker = new CircuitBreaker(options, logger);
    
    // Open circuit
    try { await breaker.ExecuteAsync(async () => throw new Exception()); }
    catch { }
    
    // Act & Assert
    await Assert.ThrowsAsync<InvalidOperationException>(async () =>
    {
        await breaker.ExecuteAsync(async () => "success");
    });
}
```

### 5. Rate Limiter Tests
**File**: `tests/Web3DashboardAPI.Tests.Unit/RateLimiting/RateLimiterTests.cs`

**Test Cases**:
```csharp
[Fact]
public void IsAllowed_WithinLimit_ReturnsTrue()
{
    // Arrange
    var options = new RateLimitOptions { RequestsPerWindow = 5, WindowDurationSeconds = 1 };
    var limiter = new RateLimiter(options, logger);
    
    // Act & Assert
    for (int i = 0; i < 5; i++)
    {
        Assert.True(limiter.IsAllowed("user1"));
    }
}

[Fact]
public void IsAllowed_ExceedsLimit_ReturnsFalse()
{
    // Arrange
    var options = new RateLimitOptions { RequestsPerWindow = 2, WindowDurationSeconds = 1 };
    var limiter = new RateLimiter(options, logger);
    
    // Act
    limiter.IsAllowed("user1");
    limiter.IsAllowed("user1");
    var result = limiter.IsAllowed("user1");
    
    // Assert
    Assert.False(result);
}

[Fact]
public void IsAllowed_DifferentUsers_Independent()
{
    // Arrange
    var options = new RateLimitOptions { RequestsPerWindow = 2, WindowDurationSeconds = 1 };
    var limiter = new RateLimiter(options, logger);
    
    // Act
    limiter.IsAllowed("user1");
    limiter.IsAllowed("user1");
    var result = limiter.IsAllowed("user2");
    
    // Assert
    Assert.True(result); // user2 should have own quota
}
```

### 6. Cache Tests
**File**: `tests/Web3DashboardAPI.Tests.Unit/Utilities/CacheTests.cs`

**Test Cases**:
```csharp
[Fact]
public void Get_SetValue_ReturnsValue()
{
    // Arrange
    var cache = new MemoryCache();
    cache.Set("key1", "value1", 60);
    
    // Act
    var result = cache.Get<string>("key1");
    
    // Assert
    Assert.Equal("value1", result);
}

[Fact]
public void Get_ExpiredValue_ReturnsNull()
{
    // Arrange
    var cache = new MemoryCache();
    cache.Set("key1", "value1", 1); // 1 second
    
    // Act
    Thread.Sleep(1100);
    var result = cache.Get<string>("key1");
    
    // Assert
    Assert.Null(result);
}

[Fact]
public void Get_NonExistentKey_ReturnsNull()
{
    // Arrange
    var cache = new MemoryCache();
    
    // Act
    var result = cache.Get<string>("nonexistent");
    
    // Assert
    Assert.Null(result);
}
```

## Integration Tests

### 1. Wallet Controller Tests
**File**: `tests/Web3DashboardAPI.Tests.Integration/Controllers/WalletsControllerIntegrationTests.cs`

**Test Cases**:
```csharp
[Fact]
public async Task AddWallet_ValidAddress_Returns200()
{
    // Arrange
    var request = new AddWalletRequest 
    { 
        Address = "0x742d35Cc6634C0532925a3b844Bc9e7595f42e61"
    };
    
    // Act
    var response = await _client.PostAsJsonAsync("/api/wallets/add", request);
    
    // Assert
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
}

[Fact]
public async Task AddWallet_InvalidAddress_Returns400()
{
    // Arrange
    var request = new AddWalletRequest { Address = "invalid" };
    
    // Act
    var response = await _client.PostAsJsonAsync("/api/wallets/add", request);
    
    // Assert
    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
}

[Fact]
public async Task GetWallet_WithoutApiKey_Returns401()
{
    // Arrange
    var address = "0x742d35Cc6634C0532925a3b844Bc9e7595f42e61";
    
    // Create client without API key
    var client = new HttpClient { BaseAddress = _factory.Server.BaseAddress };
    
    // Act
    var response = await client.GetAsync($"/api/wallets/{address}");
    
    // Assert
    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
}

[Fact]
public async Task GetWallet_WithInvalidApiKey_Returns401()
{
    // Arrange
    var address = "0x742d35Cc6634C0532925a3b844Bc9e7595f42e61";
    var client = new HttpClient { BaseAddress = _factory.Server.BaseAddress };
    client.DefaultRequestHeaders.Add("X-API-Key", "invalid-key");
    
    // Act
    var response = await client.GetAsync($"/api/wallets/{address}");
    
    // Assert
    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
}
```

### 2. Rate Limiting Integration Tests
**File**: `tests/Web3DashboardAPI.Tests.Integration/RateLimiting/RateLimitingTests.cs`

**Test Cases**:
```csharp
[Fact]
public async Task RateLimit_ExceedLimit_Returns429()
{
    // Arrange
    var requests = 101; // Default limit is 100 per minute
    
    // Act
    var responses = new List<HttpResponseMessage>();
    for (int i = 0; i < requests; i++)
    {
        var response = await _client.GetAsync("/api/health");
        responses.Add(response);
    }
    
    // Assert
    Assert.Equal(100, responses.Count(r => r.StatusCode == HttpStatusCode.OK));
    Assert.Equal(1, responses.Count(r => r.StatusCode == HttpStatusCode.TooManyRequests));
}

[Fact]
public async Task RateLimit_WithRetryAfter_HeaderPresent()
{
    // Arrange - exceed limit
    for (int i = 0; i < 101; i++)
    {
        await _client.GetAsync("/api/health");
    }
    
    // Act
    var response = await _client.GetAsync("/api/health");
    
    // Assert
    Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
    Assert.NotNull(response.Headers.RetryAfter);
}
```

## Manual Testing

### Using cURL

#### 1. Test API Key Authentication
```bash
# Without API key (should fail)
curl http://localhost:5000/api/wallets/0x742d35Cc6634C0532925a3b844Bc9e7595f42e61
# Response: 401 Unauthorized

# With API key (should work)
curl -H "X-API-Key: test-key-12345" \
  http://localhost:5000/api/wallets/0x742d35Cc6634C0532925a3b844Bc9e7595f42e61
# Response: 200 OK or 404 Not Found (wallet doesn't exist)
```

#### 2. Test Address Validation
```bash
# Invalid address (should fail)
curl -X POST http://localhost:5000/api/wallets/add \
  -H "Content-Type: application/json" \
  -H "X-API-Key: test-key-12345" \
  -d '{"address": "0x123", "name": "Test"}'
# Response: 400 Bad Request

# Invalid checksum (should fail)
curl -X POST http://localhost:5000/api/wallets/add \
  -H "Content-Type: application/json" \
  -H "X-API-Key: test-key-12345" \
  -d '{"address": "0x742d35cc6634C0532925a3b844Bc9e7595f42e61"}'
# Response: 400 Bad Request (checksum error)

# Valid address (should work)
curl -X POST http://localhost:5000/api/wallets/add \
  -H "Content-Type: application/json" \
  -H "X-API-Key: test-key-12345" \
  -d '{"address": "0x742d35Cc6634C0532925a3b844Bc9e7595f42e61", "name": "My Wallet"}'
# Response: 200 OK
```

#### 3. Test Rate Limiting
```bash
# Run 101 requests quickly
for i in {1..101}; do
  curl -H "X-API-Key: test-key-12345" http://localhost:5000/api/health
done

# The 101st request should get 429 Too Many Requests
```

#### 4. Test Retry Logic (with mock failure)
```bash
# This requires manual testing or integration tests
# Simulate network failure by temporarily stopping RPC provider
# API should retry and eventually succeed when RPC comes back
```

### Using Postman

1. **Import Collection**: `Web3DashboardAPI.postman_collection.json`
2. **Set API Key**:
   - Go to Collection > Variables
   - Set `api_key` to `test-key-12345`
3. **Run Requests**:
   - Add Wallet
   - Get Wallet
   - Get Balance
   - Sync Wallet
   - Add Token
   - Get Transactions

### Using Swagger UI

1. Navigate to `http://localhost:5000`
2. Click "Authorize" button
3. Enter API key: `test-key-12345`
4. Try endpoints from the UI

## Test Coverage Report

Expected test coverage:

| Component | Coverage | Status |
|-----------|----------|--------|
| AddressValidator | 95% | ✅ |
| InputValidator | 90% | ✅ |
| RetryPolicy | 95% | ✅ |
| CircuitBreaker | 95% | ✅ |
| RateLimiter | 90% | ✅ |
| Cache | 95% | ✅ |
| EthereumService | 85% | ✅ |
| WalletService | 80% | ✅ |
| Controllers | 75% | ⚠️ |
| **Total** | **87%** | ✅ |

## Performance Tests

### Load Testing

```bash
# Using Apache Bench
ab -n 1000 -c 10 -H "X-API-Key: test-key-12345" \
  http://localhost:5000/api/health

# Expected: ~1000 requests in ~10-20 seconds
# With caching, subsequent requests should be much faster
```

### Stress Testing

```bash
# Using wrk
wrk -t4 -c100 -d30s -H "X-API-Key: test-key-12345" \
  http://localhost:5000/api/health

# Monitor for circuit breaker opens and error rates
```

## Security Tests

### SQL Injection Test
```bash
curl -X POST http://localhost:5000/api/wallets/add \
  -H "Content-Type: application/json" \
  -H "X-API-Key: test-key-12345" \
  -d '{"address": "0x742d35Cc6634C0532925a3b844Bc9e7595f42e61'\'''; DROP TABLE wallets; --", "name": "Attack"}'
# Response: 400 Bad Request (address validation fails)
```

### XSS Test
```bash
curl -X POST http://localhost:5000/api/wallets/add \
  -H "Content-Type: application/json" \
  -H "X-API-Key: test-key-12345" \
  -d '{"address": "0x742d35Cc6634C0532925a3b844Bc9e7595f42e61", "name": "<script>alert(1)</script>"}'
# Response: 400 Bad Request (name validation fails)
```

## CI/CD Integration

### GitHub Actions Example

```yaml
name: Tests
on: [push, pull_request]
jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v2
      - uses: actions/setup-dotnet@v1
        with:
          dotnet-version: '8.0.x'
      - run: dotnet restore
      - run: dotnet build
      - run: dotnet test --logger "console;verbosity=detailed"
      - run: dotnet test /p:CollectCoverage=true
```

## Test Data

### Valid Test Addresses (Sepolia)

```
0x742d35Cc6634C0532925a3b844Bc9e7595f42e61 - Valid checksummed address
0x1234567890123456789012345678901234567890 - Valid lowercase address
0xc0ffee254729296a45a3885639AC7E10F9d54979 - Another valid address
```

### Invalid Test Addresses

```
0x123                               - Too short
0x742d35cc6634C0532925a3b844Bc9e7595f42e61  - Invalid checksum
invalid                             - Not hex
0x742d35Cc6634C0532925a3b844Bc9e7595f42e6g  - Invalid character
```

---

**Last Updated**: 2024-02-19
**Version**: 2.0
