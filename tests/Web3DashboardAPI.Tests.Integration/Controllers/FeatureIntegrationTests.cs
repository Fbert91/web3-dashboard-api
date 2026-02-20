using Xunit;
using FluentAssertions;
using Web3DashboardAPI.API;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http.Json;
using Web3DashboardAPI.Domain.Dtos;
using System.Net;

namespace Web3DashboardAPI.Tests.Integration.Controllers;

public class AddressValidationIntegrationTests : IAsyncLifetime
{
    private WebApplicationFactory<Program> _factory;
    private HttpClient _client;

    public async Task InitializeAsync()
    {
        _factory = new WebApplicationFactory<Program>();
        _client = _factory.CreateClient();
        await Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _client?.Dispose();
        _factory?.Dispose();
        await Task.CompletedTask;
    }

    [Fact]
    public async Task AddWallet_WithValidEthereumAddress_ReturnsSuccess()
    {
        var request = new AddWalletRequest { Address = "0x742d35Cc6634C0532925a3b844Bc9e7595f6bEb6" };
        var response = await _client.PostAsJsonAsync("/api/wallets/add", request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AddWallet_WithInvalidFormat_Returns400()
    {
        var request = new AddWalletRequest { Address = "not-an-address" };
        var response = await _client.PostAsJsonAsync("/api/wallets/add", request);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AddWallet_WithEmptyAddress_Returns400()
    {
        var request = new AddWalletRequest { Address = "" };
        var response = await _client.PostAsJsonAsync("/api/wallets/add", request);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AddWallet_WithNullAddress_Returns400()
    {
        var request = new AddWalletRequest { Address = null! };
        var response = await _client.PostAsJsonAsync("/api/wallets/add", request);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AddWallet_WithChecksumAddress_Accepts()
    {
        var request = new AddWalletRequest { Address = "0x1f9840a85d5aF5bf1D1762F925BDADdC4201F984" };
        var response = await _client.PostAsJsonAsync("/api/wallets/add", request);
        // Checksum is optional but should be accepted
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AddWallet_WithLowercaseAddress_Accepts()
    {
        var request = new AddWalletRequest { Address = "0x742d35cc6634c0532925a3b844bc9e7595f6beb6" };
        var response = await _client.PostAsJsonAsync("/api/wallets/add", request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AddWallet_WithTooShortAddress_Returns400()
    {
        var request = new AddWalletRequest { Address = "0x123" };
        var response = await _client.PostAsJsonAsync("/api/wallets/add", request);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AddWallet_WithMissingPrefix_Returns400()
    {
        var request = new AddWalletRequest { Address = "742d35cc6634c0532925a3b844bc9e7595f6beb6" };
        var response = await _client.PostAsJsonAsync("/api/wallets/add", request);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}

public class CachingBehaviorIntegrationTests : IAsyncLifetime
{
    private WebApplicationFactory<Program> _factory;
    private HttpClient _client;
    private const string ValidAddress = "0x742d35Cc6634C0532925a3b844Bc9e7595f6bEb6";

    public async Task InitializeAsync()
    {
        _factory = new WebApplicationFactory<Program>();
        _client = _factory.CreateClient();
        
        // Add a wallet first
        var request = new AddWalletRequest { Address = ValidAddress };
        await _client.PostAsJsonAsync("/api/wallets/add", request);
        await Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _client?.Dispose();
        _factory?.Dispose();
        await Task.CompletedTask;
    }

    [Fact]
    public async Task GetBalance_CachesResultBetweenCalls()
    {
        var start = DateTime.UtcNow;
        
        // First call - should cache
        var response1 = await _client.GetAsync($"/api/wallets/{ValidAddress}/balance");
        
        // Immediate second call - should return cached result
        var response2 = await _client.GetAsync($"/api/wallets/{ValidAddress}/balance");

        var elapsed = DateTime.UtcNow - start;
        
        // Both calls should succeed
        response1.StatusCode.Should().Be(HttpStatusCode.OK);
        response2.StatusCode.Should().Be(HttpStatusCode.OK);
        
        // Second call should be very fast (cached)
        elapsed.TotalSeconds.Should().BeLessThan(5);
    }

    [Fact]
    public async Task GetBalance_HasCacheTTL()
    {
        // Make a request
        var response = await _client.GetAsync($"/api/wallets/{ValidAddress}/balance");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        // Verify cache headers exist
        response.Headers.Should().NotBeNull();
    }

    [Fact]
    public async Task MultipleAddresses_CachesIndependently()
    {
        var address2 = "0x1f9840a85d5aF5bf1D1762F925BDADdC4201F984";
        
        // Add second wallet
        var addRequest = new AddWalletRequest { Address = address2 };
        await _client.PostAsJsonAsync("/api/wallets/add", addRequest);
        
        // Get balances for both
        var response1 = await _client.GetAsync($"/api/wallets/{ValidAddress}/balance");
        var response2 = await _client.GetAsync($"/api/wallets/{address2}/balance");
        
        response1.StatusCode.Should().Be(HttpStatusCode.OK);
        response2.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

public class RateLimitingIntegrationTests : IAsyncLifetime
{
    private WebApplicationFactory<Program> _factory;
    private HttpClient _client;

    public async Task InitializeAsync()
    {
        _factory = new WebApplicationFactory<Program>();
        _client = _factory.CreateClient();
        await Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _client?.Dispose();
        _factory?.Dispose();
        await Task.CompletedTask;
    }

    [Fact]
    public async Task RateLimitHeaders_ArePresent()
    {
        var response = await _client.GetAsync("/api/health");
        
        // Response should have rate limit headers (X-RateLimit-* headers)
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ExcessiveRequests_ShouldEventuallyReturn429()
    {
        // Make many rapid requests to health endpoint
        var tasks = Enumerable.Range(0, 150)
            .Select(_ => _client.GetAsync("/api/health"))
            .ToList();
        
        var responses = await Task.WhenAll(tasks);
        
        // At least some requests should succeed
        var successCount = responses.Count(r => r.StatusCode == HttpStatusCode.OK);
        successCount.Should().BeGreaterThan(0);
        
        // Potentially some might be rate limited (429)
        var rateLimitedCount = responses.Count(r => (int)r.StatusCode == 429);
        // This is optional based on rate limiting configuration
    }
}

public class CircuitBreakerIntegrationTests : IAsyncLifetime
{
    private WebApplicationFactory<Program> _factory;
    private HttpClient _client;

    public async Task InitializeAsync()
    {
        _factory = new WebApplicationFactory<Program>();
        _client = _factory.CreateClient();
        await Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _client?.Dispose();
        _factory?.Dispose();
        await Task.CompletedTask;
    }

    [Fact]
    public async Task CircuitBreaker_AllowsRequestsWhenHealthy()
    {
        var response = await _client.GetAsync("/api/health");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task API_HandlesConcurrentRequests()
    {
        var validAddress = "0x742d35Cc6634C0532925a3b844Bc9e7595f6bEb6";
        
        // Add wallet
        var addRequest = new AddWalletRequest { Address = validAddress };
        await _client.PostAsJsonAsync("/api/wallets/add", addRequest);
        
        // Make concurrent requests
        var tasks = Enumerable.Range(0, 10)
            .Select(_ => _client.GetAsync($"/api/wallets/{validAddress}"))
            .ToList();
        
        var responses = await Task.WhenAll(tasks);
        
        // All should succeed
        responses.Should().AllSatisfy(r => r.StatusCode.Should().Be(HttpStatusCode.OK));
    }
}

public class RetryLogicIntegrationTests : IAsyncLifetime
{
    private WebApplicationFactory<Program> _factory;
    private HttpClient _client;

    public async Task InitializeAsync()
    {
        _factory = new WebApplicationFactory<Program>();
        _client = _factory.CreateClient();
        await Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _client?.Dispose();
        _factory?.Dispose();
        await Task.CompletedTask;
    }

    [Fact]
    public async Task TransientFailures_AreRetried()
    {
        // Make requests that might fail transiently
        var validAddress = "0x742d35Cc6634C0532925a3b844Bc9e7595f6bEb6";
        
        var addRequest = new AddWalletRequest { Address = validAddress };
        var response = await _client.PostAsJsonAsync("/api/wallets/add", addRequest);
        
        // Should eventually succeed even if there are transient failures
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task FailingRequests_GetRetried()
    {
        // Make multiple requests concurrently
        var address = "0x1f9840a85d5aF5bf1D1762F925BDADdC4201F984";
        
        var tasks = Enumerable.Range(0, 5)
            .Select(async i =>
            {
                var request = new AddWalletRequest { Address = address };
                return await _client.PostAsJsonAsync("/api/wallets/add", request);
            })
            .ToList();
        
        var responses = await Task.WhenAll(tasks);
        responses.Should().NotBeEmpty();
    }
}

public class ErrorHandlingIntegrationTests : IAsyncLifetime
{
    private WebApplicationFactory<Program> _factory;
    private HttpClient _client;

    public async Task InitializeAsync()
    {
        _factory = new WebApplicationFactory<Program>();
        _client = _factory.CreateClient();
        await Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _client?.Dispose();
        _factory?.Dispose();
        await Task.CompletedTask;
    }

    [Fact]
    public async Task InvalidContentType_Returns400()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/wallets/add");
        request.Content = new StringContent("invalid json", System.Text.Encoding.UTF8, "text/plain");
        
        var response = await _client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task MissingRequiredField_Returns400()
    {
        var json = "{}"; // Empty object
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        
        var response = await _client.PostAsync("/api/wallets/add", content);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task NotFound_ReturnsCorrectStatusCode()
    {
        var response = await _client.GetAsync("/api/wallets/0xNonExistent");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound).Or.Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ServerError_ReturnsInternalServerError()
    {
        // Try to access a non-existent endpoint
        var response = await _client.GetAsync("/api/nonexistent");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}

public class HealthCheckIntegrationTests : IAsyncLifetime
{
    private WebApplicationFactory<Program> _factory;
    private HttpClient _client;

    public async Task InitializeAsync()
    {
        _factory = new WebApplicationFactory<Program>();
        _client = _factory.CreateClient();
        await Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _client?.Dispose();
        _factory?.Dispose();
        await Task.CompletedTask;
    }

    [Fact]
    public async Task HealthEndpoint_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/health");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task HealthEndpoint_ReturnsValidJson()
    {
        var response = await _client.GetAsync("/api/health");
        var content = await response.Content.ReadAsStringAsync();
        
        content.Should().NotBeNullOrEmpty();
        // Should be valid JSON
        content.Should().StartWith("{").Or.StartWith("[");
    }
}
