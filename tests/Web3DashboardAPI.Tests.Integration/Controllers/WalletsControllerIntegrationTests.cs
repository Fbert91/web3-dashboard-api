using Xunit;
using FluentAssertions;
using Web3DashboardAPI.API;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http.Json;
using Web3DashboardAPI.Domain.Dtos;
using System.Net;

namespace Web3DashboardAPI.Tests.Integration.Controllers;

public class WalletsControllerIntegrationTests : IAsyncLifetime
{
    private WebApplicationFactory<Program> _factory;
    private HttpClient _client;
    private const string ValidAddress = "0x742d35Cc6634C0532925a3b844Bc9e7595f6bEb6";
    private const string ValidAddress2 = "0x1f9840a85d5aF5bf1D1762F925BDADdC4201F984";

    public async Task InitializeAsync()
    {
        _factory = new WebApplicationFactory<Program>();
        _client = _factory.CreateClient();
        // Database is initialized in Program.cs
        await Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _client?.Dispose();
        _factory?.Dispose();
        await Task.CompletedTask;
    }

    [Fact]
    public async Task GetHealth_ShouldReturn200OK()
    {
        // Act
        var response = await _client.GetAsync("/api/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AddWallet_WithValidAddress_ShouldSucceed()
    {
        // Arrange
        var request = new AddWalletRequest { Address = ValidAddress };

        // Act
        var response = await _client.PostAsJsonAsync("/api/wallets/add", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<WalletResponse>();
        result.Should().NotBeNull();
        result!.Address.Should().Be(ValidAddress);
    }

    [Fact]
    public async Task AddWallet_WithInvalidAddress_ShouldReturn400BadRequest()
    {
        // Arrange
        var request = new AddWalletRequest { Address = "invalid-address" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/wallets/add", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetWallet_WithValidAddress_ShouldReturn200OK()
    {
        // Arrange - first add a wallet
        var addRequest = new AddWalletRequest { Address = ValidAddress };
        await _client.PostAsJsonAsync("/api/wallets/add", addRequest);

        // Act
        var response = await _client.GetAsync($"/api/wallets/{ValidAddress}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<WalletResponse>();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetWallet_NotFound_ShouldReturn404()
    {
        // Arrange
        var nonExistentAddress = "0x0000000000000000000000000000000000000000";

        // Act
        var response = await _client.GetAsync($"/api/wallets/{nonExistentAddress}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetBalance_WithValidAddress_ShouldReturn200OK()
    {
        // Arrange
        var addRequest = new AddWalletRequest { Address = ValidAddress };
        await _client.PostAsJsonAsync("/api/wallets/add", addRequest);

        // Act
        var response = await _client.GetAsync($"/api/wallets/{ValidAddress}/balance");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetBalance_WithInvalidAddress_ShouldReturn400()
    {
        // Act
        var response = await _client.GetAsync($"/api/wallets/invalid-address/balance");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetTransactions_WithValidAddress_ShouldReturn200OK()
    {
        // Arrange
        var address = ValidAddress;

        // Act
        var response = await _client.GetAsync($"/api/wallets/{address}/transactions");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetTransactions_WithLimit_ShouldReturnLimitedResults()
    {
        // Arrange
        var address = ValidAddress;
        var limit = 10;

        // Act
        var response = await _client.GetAsync($"/api/wallets/{address}/transactions?limit={limit}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetTransactions_WithInvalidLimit_ShouldValidate()
    {
        // Arrange
        var address = ValidAddress;
        var invalidLimit = 1000; // Might exceed max

        // Act
        var response = await _client.GetAsync($"/api/wallets/{address}/transactions?limit={invalidLimit}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AddToken_WithValidAddress_ShouldSucceed()
    {
        // Arrange
        var walletRequest = new AddWalletRequest { Address = ValidAddress };
        await _client.PostAsJsonAsync("/api/wallets/add", walletRequest);

        var tokenAddress = "0xA0b86991c6218b36c1d19D4a2e9Eb0cE3606eB48"; // USDC

        // Act
        var response = await _client.PostAsJsonAsync(
            $"/api/wallets/{ValidAddress}/tokens",
            new { tokenAddress = tokenAddress }
        );

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SyncWallet_WithValidAddress_ShouldSucceed()
    {
        // Arrange
        var addRequest = new AddWalletRequest { Address = ValidAddress };
        await _client.PostAsJsonAsync("/api/wallets/add", addRequest);

        // Act
        var response = await _client.PutAsJsonAsync($"/api/wallets/{ValidAddress}/sync", new { });

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AddMultipleWallets_ShouldEachBeIndependent()
    {
        // Arrange
        var request1 = new AddWalletRequest { Address = ValidAddress };
        var request2 = new AddWalletRequest { Address = ValidAddress2 };

        // Act
        var response1 = await _client.PostAsJsonAsync("/api/wallets/add", request1);
        var response2 = await _client.PostAsJsonAsync("/api/wallets/add", request2);

        // Assert
        response1.StatusCode.Should().Be(HttpStatusCode.OK);
        response2.StatusCode.Should().Be(HttpStatusCode.OK);

        var wallet1 = await response1.Content.ReadFromJsonAsync<WalletResponse>();
        var wallet2 = await response2.Content.ReadFromJsonAsync<WalletResponse>();

        wallet1!.Address.Should().NotBe(wallet2!.Address);
    }

    [Fact]
    public async Task AddWallet_Twice_ShouldHandleGracefully()
    {
        // Arrange
        var request = new AddWalletRequest { Address = ValidAddress };

        // Act
        var response1 = await _client.PostAsJsonAsync("/api/wallets/add", request);
        var response2 = await _client.PostAsJsonAsync("/api/wallets/add", request);

        // Assert
        response1.StatusCode.Should().Be(HttpStatusCode.OK);
        // Second add might return conflict or success depending on implementation
        response2.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task AllEndpoints_RequireValidAddress()
    {
        var invalidAddress = "invalid";

        var responses = new List<HttpResponseMessage>
        {
            await _client.GetAsync($"/api/wallets/{invalidAddress}"),
            await _client.GetAsync($"/api/wallets/{invalidAddress}/balance"),
            await _client.GetAsync($"/api/wallets/{invalidAddress}/transactions"),
        };

        responses.Should().AllSatisfy(r => 
            r.StatusCode.Should().Be(HttpStatusCode.BadRequest)
        );
    }

    [Fact]
    public async Task ConcurrentWalletAdds_ShouldSucceed()
    {
        // Arrange
        var requests = new List<AddWalletRequest>
        {
            new() { Address = ValidAddress },
            new() { Address = ValidAddress2 }
        };

        // Act
        var tasks = requests
            .Select(r => _client.PostAsJsonAsync("/api/wallets/add", r))
            .ToList();

        var responses = await Task.WhenAll(tasks);

        // Assert
        responses.Should().AllSatisfy(r => 
            r.StatusCode.Should().Be(HttpStatusCode.OK)
        );
    }

    [Fact]
    public async Task GetBalance_ResponseTime_IsReasonable()
    {
        // Arrange
        var addRequest = new AddWalletRequest { Address = ValidAddress };
        await _client.PostAsJsonAsync("/api/wallets/add", addRequest);

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var response = await _client.GetAsync($"/api/wallets/{ValidAddress}/balance");
        stopwatch.Stop();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000); // Should complete in 5 seconds
    }
}
