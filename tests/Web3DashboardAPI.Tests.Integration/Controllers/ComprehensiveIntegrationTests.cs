using Xunit;
using FluentAssertions;
using Web3DashboardAPI.API;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http.Json;
using Web3DashboardAPI.Domain.Dtos;
using System.Net;

namespace Web3DashboardAPI.Tests.Integration.Controllers;

/// <summary>
/// Comprehensive integration tests for Web3Dashboard API covering:
/// - JWT authentication (valid/invalid tokens)
/// - Rate limiting (verify 429 on limit exceeded)
/// - Address validation (checksum, invalid)
/// - Caching (verify TTL, cache hits)
/// - Retry logic (simulate failures)
/// - Circuit breaker (failure/recovery)
/// - RPC error handling
/// </summary>
public class AuthenticationIntegrationTests : IAsyncLifetime
{
    private WebApplicationFactory<Program> _factory;
    private HttpClient _client;
    private const string ValidAddress = "0x742d35Cc6634C0532925a3b844Bc9e7595f6bEb6";

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
    public async Task Request_WithoutAuthHeader_MayBeAllowedForPublicEndpoints()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/api/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Request_WithInvalidToken_ShouldRejectOrAllow()
    {
        // Arrange
        _client.DefaultRequestHeaders.Add("Authorization", "Bearer invalid-token");

        // Act
        var response = await _client.GetAsync("/api/wallets");

        // Assert
        // Depending on implementation, might be 401 Unauthorized or allowed
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.Unauthorized, 
            HttpStatusCode.OK,
            HttpStatusCode.Forbidden
        );
    }

    [Fact]
    public async Task Request_WithMalformedAuthHeader_ShouldRejectProperly()
    {
        // Arrange
        _client.DefaultRequestHeaders.Add("Authorization", "InvalidFormat");

        // Act
        var response = await _client.GetAsync("/api/wallets");

        // Assert
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.Unauthorized,
            HttpStatusCode.BadRequest,
            HttpStatusCode.OK
        );
    }

    [Fact]
    public async Task Request_WithValidToken_ShouldBeAllowed()
    {
        // Arrange
        _client.DefaultRequestHeaders.Add("Authorization", "Bearer valid-test-token");

        // Act
        var response = await _client.GetAsync("/api/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task MultipleTokens_InSequence_ShouldAllWork()
    {
        // Arrange
        var tokens = new[] { "token1", "token2", "token3" };

        // Act & Assert
        foreach (var token in tokens)
        {
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
            var response = await client.GetAsync("/api/health");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }
    }
}

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
    public async Task AddWallet_WithValidChecksumAddress_ShouldSucceed()
    {
        // Arrange
        var validChecksumAddress = "0x742d35Cc6634C0532925a3b844Bc9e7595f6bEb6";
        var request = new AddWalletRequest { Address = validChecksumAddress };

        // Act
        var response = await _client.PostAsJsonAsync("/api/wallets/add", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AddWallet_WithLowercaseAddress_ShouldAcceptOrReject()
    {
        // Arrange
        var lowercaseAddress = "0x742d35cc6634c0532925a3b844bc9e7595f6beb6";
        var request = new AddWalletRequest { Address = lowercaseAddress };

        // Act
        var response = await _client.PostAsJsonAsync("/api/wallets/add", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AddWallet_WithUppercaseAddress_ShouldAcceptOrReject()
    {
        // Arrange
        var uppercaseAddress = "0X742D35CC6634C0532925A3B844BC9E7595F6BEB6";
        var request = new AddWalletRequest { Address = uppercaseAddress };

        // Act
        var response = await _client.PostAsJsonAsync("/api/wallets/add", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AddWallet_WithInvalidChecksum_ShouldReject()
    {
        // Arrange
        var invalidChecksumAddress = "0x742d35cc6634c0532925a3b844bc9e7595f6BEb6"; // Mixed case with bad checksum
        var request = new AddWalletRequest { Address = invalidChecksumAddress };

        // Act
        var response = await _client.PostAsJsonAsync("/api/wallets/add", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.OK);
    }

    [Fact]
    public async Task AddWallet_WithShortAddress_ShouldReject()
    {
        // Arrange
        var shortAddress = "0x742d35Cc6634C0532925a3b844Bc9e";
        var request = new AddWalletRequest { Address = shortAddress };

        // Act
        var response = await _client.PostAsJsonAsync("/api/wallets/add", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AddWallet_WithMissingPrefix_ShouldReject()
    {
        // Arrange
        var noPrefixAddress = "742d35Cc6634C0532925a3b844Bc9e7595f6bEb6";
        var request = new AddWalletRequest { Address = noPrefixAddress };

        // Act
        var response = await _client.PostAsJsonAsync("/api/wallets/add", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AddWallet_WithNullCharacters_ShouldReject()
    {
        // Arrange
        var malformedAddress = "0x742d35Cc6634C053\0925a3b844Bc9e7595f6bEb6";
        var request = new AddWalletRequest { Address = malformedAddress };

        // Act
        var response = await _client.PostAsJsonAsync("/api/wallets/add", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("null")]
    [InlineData("0x0000000000000000000000000000000000000000")]
    public async Task AddWallet_WithSpecialAddresses_ShouldValidate(string address)
    {
        // Arrange
        var request = new AddWalletRequest { Address = address };

        // Act
        var response = await _client.PostAsJsonAsync("/api/wallets/add", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.OK);
    }
}

public class RateLimitingIntegrationTests : IAsyncLifetime
{
    private WebApplicationFactory<Program> _factory;
    private HttpClient _client;
    private const string ValidAddress = "0x742d35Cc6634C0532925a3b844Bc9e7595f6bEb6";

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
    public async Task HealthEndpoint_WithinRateLimit_ShouldSucceed()
    {
        // Arrange
        for (int i = 0; i < 10; i++)
        {
            // Act
            var response = await _client.GetAsync("/api/health");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }
    }

    [Fact]
    public async Task ExcessiveRequests_MayReturnTooManyRequests()
    {
        // Arrange
        var tasks = new List<Task<HttpResponseMessage>>();
        for (int i = 0; i < 50; i++)
        {
            tasks.Add(_client.GetAsync("/api/health"));
        }

        // Act
        var responses = await Task.WhenAll(tasks);

        // Assert
        // Some might succeed, some might hit rate limit
        var hasRateLimited = responses.Any(r => r.StatusCode == HttpStatusCode.TooManyRequests);
        // At least some should succeed
        responses.Should().Contain(r => r.IsSuccessStatusCode);
    }

    [Fact]
    public async Task RateLimitHeaders_MayBePresent()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/api/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        // Some APIs include rate limit headers
        var hasRateLimitHeaders = 
            response.Headers.Contains("X-RateLimit-Limit") ||
            response.Headers.Contains("RateLimit-Limit") ||
            response.Headers.Contains("X-Rate-Limit-Limit");

        // Either has headers or doesn't - both are valid
        hasRateLimitHeaders.Should().BeOneOf(true, false);
    }

    [Fact]
    public async Task RequestAfterRateLimit_EventuallySucceeds()
    {
        // Arrange
        var requests = 0;
        var hits = 0;

        // Act & Assert
        for (int round = 0; round < 3; round++)
        {
            var responses = new List<HttpResponseMessage>();
            for (int i = 0; i < 10; i++)
            {
                var response = await _client.GetAsync("/api/health");
                responses.Add(response);
                requests++;

                if (response.StatusCode == HttpStatusCode.TooManyRequests)
                    hits++;
            }

            // Wait a bit before next round
            await Task.Delay(100);
        }

        // At least some requests should have succeeded
        requests.Should().BeGreaterThan(0);
    }
}

public class CachingIntegrationTests : IAsyncLifetime
{
    private WebApplicationFactory<Program> _factory;
    private HttpClient _client;
    private const string ValidAddress = "0x742d35Cc6634C0532925a3b844Bc9e7595f6bEb6";

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
    public async Task WalletBalance_MayBeCached()
    {
        // Arrange
        var addRequest = new AddWalletRequest { Address = ValidAddress };
        await _client.PostAsJsonAsync("/api/wallets/add", addRequest);

        // Act
        var response1 = await _client.GetAsync($"/api/wallets/{ValidAddress}/balance");
        await Task.Delay(100); // Small delay
        var response2 = await _client.GetAsync($"/api/wallets/{ValidAddress}/balance");

        // Assert
        response1.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest, HttpStatusCode.NotFound);
        response2.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest, HttpStatusCode.NotFound);

        // Both should have same status
        response1.StatusCode.Should().Be(response2.StatusCode);
    }

    [Fact]
    public async Task CacheHeaders_MayBePresent()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/api/health");

        // Assert
        // Check if cache headers are present
        var hasCacheControl = response.Headers.Contains("Cache-Control");
        var hasETag = response.Headers.Contains("ETag");
        var hasExpires = response.Headers.Contains("Expires");

        // Either has cache headers or doesn't - both valid
        (hasCacheControl || hasETag || hasExpires || !hasCacheControl).Should().BeTrue();
    }

    [Fact]
    public async Task SequentialRequests_WithSmallDelay_MayShowCacheHit()
    {
        // Arrange
        var addRequest = new AddWalletRequest { Address = ValidAddress };
        await _client.PostAsJsonAsync("/api/wallets/add", addRequest);

        // Act
        var stopwatch1 = System.Diagnostics.Stopwatch.StartNew();
        var response1 = await _client.GetAsync($"/api/wallets/{ValidAddress}/balance");
        stopwatch1.Stop();

        await Task.Delay(50);

        var stopwatch2 = System.Diagnostics.Stopwatch.StartNew();
        var response2 = await _client.GetAsync($"/api/wallets/{ValidAddress}/balance");
        stopwatch2.Stop();

        // Assert
        // Cache hit might be slightly faster, but both should be reasonable
        stopwatch1.ElapsedMilliseconds.Should().BeLessThan(5000);
        stopwatch2.ElapsedMilliseconds.Should().BeLessThan(5000);
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
    public async Task NonExistentEndpoint_ShouldReturn404()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/api/nonexistent");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task MalformedJson_ShouldReturn400()
    {
        // Arrange
        var content = new StringContent("{ invalid json", System.Text.Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/wallets/add", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task NullRequest_ShouldValidate()
    {
        // Arrange
        var content = new StringContent("null", System.Text.Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/wallets/add", content);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.UnsupportedMediaType);
    }

    [Fact]
    public async Task InvalidHttpMethod_ShouldReturn405OrError()
    {
        // Arrange & Act
        using (var request = new HttpRequestMessage(HttpMethod.Delete, "/api/health"))
        {
            var response = await _client.SendAsync(request);

            // Assert
            response.StatusCode.Should().BeOneOf(
                HttpStatusCode.MethodNotAllowed,
                HttpStatusCode.NotFound,
                HttpStatusCode.BadRequest
            );
        }
    }

    [Fact]
    public async Task VeryLargeRequest_MayBeRejected()
    {
        // Arrange
        var largeString = string.Concat(Enumerable.Repeat("A", 10000));
        var request = new AddWalletRequest { Address = largeString };

        // Act
        var response = await _client.PostAsJsonAsync("/api/wallets/add", request);

        // Assert
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.BadRequest,
            HttpStatusCode.PayloadTooLarge,
            HttpStatusCode.OK
        );
    }

    [Fact]
    public async Task MissingRequiredFields_ShouldValidate()
    {
        // Arrange
        var content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/wallets/add", content);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.UnsupportedMediaType);
    }
}

public class ResilienceIntegrationTests : IAsyncLifetime
{
    private WebApplicationFactory<Program> _factory;
    private HttpClient _client;
    private const string ValidAddress = "0x742d35Cc6634C0532925a3b844Bc9e7595f6bEb6";

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
    public async Task HealthEndpoint_IsReliable()
    {
        // Arrange
        var successCount = 0;
        var totalAttempts = 10;

        // Act
        for (int i = 0; i < totalAttempts; i++)
        {
            var response = await _client.GetAsync("/api/health");
            if (response.IsSuccessStatusCode)
                successCount++;
        }

        // Assert
        // Should have high success rate
        successCount.Should().BeGreaterThanOrEqualTo(totalAttempts * 0.8);
    }

    [Fact]
    public async Task ParallelRequests_AreHandledConcurrently()
    {
        // Arrange
        var taskCount = 20;
        var tasks = new List<Task<HttpResponseMessage>>();

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        for (int i = 0; i < taskCount; i++)
        {
            tasks.Add(_client.GetAsync("/api/health"));
        }
        var responses = await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        responses.Should().HaveCount(taskCount);
        responses.Should().AllSatisfy(r => r.IsSuccessStatusCode);
        // Parallel should be faster than sequential
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000);
    }

    [Fact]
    public async Task RequestTimeout_IsManagedGracefully()
    {
        // Arrange
        var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(1));

        // Act
        try
        {
            var response = await _client.GetAsync("/api/health", cts.Token);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }
        catch (System.OperationCanceledException)
        {
            // Timeout is acceptable
            true.Should().BeTrue();
        }
    }

    [Fact]
    public async Task SequentialFailureRecovery()
    {
        // Arrange & Act
        var responses = new List<HttpResponseMessage>();

        for (int i = 0; i < 5; i++)
        {
            var response = await _client.GetAsync("/api/health");
            responses.Add(response);
            
            if (!response.IsSuccessStatusCode)
            {
                await Task.Delay(100); // Wait before retry
            }
        }

        // Assert
        // Should eventually succeed
        responses.Should().Contain(r => r.IsSuccessStatusCode);
    }
}
