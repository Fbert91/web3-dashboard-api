using Xunit;
using Moq;
using FluentAssertions;
using Web3DashboardAPI.Application.Services;
using Web3DashboardAPI.Infrastructure.Data;
using Web3DashboardAPI.Infrastructure.Ethereum;
using Web3DashboardAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Web3DashboardAPI.Tests.Unit.Services;

public class WalletServiceTests
{
    private readonly Mock<IEthereumService> _ethereumServiceMock;
    private readonly Web3DbContext _dbContext;
    private readonly WalletService _walletService;

    public WalletServiceTests()
    {
        _ethereumServiceMock = new Mock<IEthereumService>();
        
        // Setup in-memory database
        var options = new DbContextOptionsBuilder<Web3DbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new Web3DbContext(options);
        
        _walletService = new WalletService(_dbContext, _ethereumServiceMock.Object);
    }

    [Fact]
    public async Task AddOrGetWalletAsync_WithValidAddress_ShouldCreateWallet()
    {
        // Arrange
        var address = "0x1234567890123456789012345678901234567890";
        var name = "Test Wallet";
        
        _ethereumServiceMock
            .Setup(x => x.IsValidAddress(address))
            .Returns(true);
        
        _ethereumServiceMock
            .Setup(x => x.GetEthBalanceAsync(address))
            .ReturnsAsync("1000000000000000000"); // 1 ETH in wei

        // Act
        var result = await _walletService.AddOrGetWalletAsync(address, name);

        // Assert
        result.Should().NotBeNull();
        result.Address.Should().Be(address.ToLower());
        result.Name.Should().Be(name);
        result.FormattedEthBalance.Should().Be("1");
    }

    [Fact]
    public async Task GetWalletAsync_WithValidAddress_ShouldReturnWallet()
    {
        // Arrange
        var address = "0x1234567890123456789012345678901234567890";
        var wallet = new Wallet
        {
            Address = address.ToLower(),
            Name = "Test",
            CreatedAt = DateTime.UtcNow,
            LastSyncedAt = DateTime.UtcNow
        };
        
        _dbContext.Wallets.Add(wallet);
        await _dbContext.SaveChangesAsync();

        _ethereumServiceMock
            .Setup(x => x.IsValidAddress(address))
            .Returns(true);
        
        _ethereumServiceMock
            .Setup(x => x.GetEthBalanceAsync(address))
            .ReturnsAsync("2000000000000000000"); // 2 ETH

        // Act
        var result = await _walletService.GetWalletAsync(address);

        // Assert
        result.Should().NotBeNull();
        result.Address.Should().Be(address.ToLower());
        result.FormattedEthBalance.Should().Be("2");
    }

    [Fact]
    public async Task GetWalletAsync_WithInvalidAddress_ShouldThrowException()
    {
        // Arrange
        var invalidAddress = "invalid-address";
        
        _ethereumServiceMock
            .Setup(x => x.IsValidAddress(invalidAddress))
            .Returns(false);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _walletService.GetWalletAsync(invalidAddress)
        );
    }

    [Fact]
    public async Task AddTokenAsync_WithValidToken_ShouldAddTokenBalance()
    {
        // Arrange
        var walletAddress = "0x1234567890123456789012345678901234567890";
        var tokenAddress = "0x0987654321098765432109876543210987654321";
        
        var wallet = new Wallet
        {
            Address = walletAddress.ToLower(),
            CreatedAt = DateTime.UtcNow,
            LastSyncedAt = DateTime.UtcNow
        };
        _dbContext.Wallets.Add(wallet);
        await _dbContext.SaveChangesAsync();

        _ethereumServiceMock
            .Setup(x => x.IsValidAddress(walletAddress))
            .Returns(true);
        
        _ethereumServiceMock
            .Setup(x => x.IsValidAddress(tokenAddress))
            .Returns(true);

        _ethereumServiceMock
            .Setup(x => x.GetErc20TokenDetailsAsync(tokenAddress))
            .ReturnsAsync(("Test Token", "TST", 18));

        _ethereumServiceMock
            .Setup(x => x.GetErc20BalanceAsync(walletAddress, tokenAddress))
            .ReturnsAsync("5000000000000000000"); // 5 tokens

        // Act
        await _walletService.AddTokenAsync(walletAddress, tokenAddress);

        // Assert
        var savedToken = _dbContext.TokenBalances.First();
        savedToken.Name.Should().Be("Test Token");
        savedToken.Symbol.Should().Be("TST");
        savedToken.FormattedBalance.Should().Be("5");
    }
}
