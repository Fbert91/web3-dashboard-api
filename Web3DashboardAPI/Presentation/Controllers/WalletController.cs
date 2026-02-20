using Microsoft.AspNetCore.Mvc;
using Web3DashboardAPI.Application.DTOs;
using Web3DashboardAPI.Application.Services;

namespace Web3DashboardAPI.Presentation.Controllers;

/// <summary>
/// Wallet operations API endpoints
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class WalletController : ControllerBase
{
    private readonly IWalletService _walletService;
    private readonly ILogger<WalletController> _logger;

    public WalletController(IWalletService walletService, ILogger<WalletController> logger)
    {
        _walletService = walletService;
        _logger = logger;
    }

    /// <summary>
    /// Validate an Ethereum wallet address
    /// </summary>
    /// <param name="address">Ethereum wallet address (0x...)</param>
    [HttpGet("validate/{address}")]
    public async Task<ActionResult<WalletValidationDto>> ValidateWallet(string address)
    {
        _logger.LogInformation("ValidateWallet: {Address}", address);
        var result = await _walletService.ValidateWalletAsync(address);
        return Ok(result);
    }

    /// <summary>
    /// Get wallet ETH and token balances
    /// </summary>
    /// <param name="address">Ethereum wallet address</param>
    [HttpGet("{address}/balance")]
    public async Task<ActionResult<WalletBalanceDto>> GetBalance(string address)
    {
        _logger.LogInformation("GetBalance: {Address}", address);

        // Validate first
        var validation = await _walletService.ValidateWalletAsync(address);
        if (!validation.IsValid)
            return BadRequest(validation);

        try
        {
            var balance = await _walletService.GetWalletBalanceAsync(address);
            return Ok(balance);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting balance");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get ETH balance for a wallet
    /// </summary>
    /// <param name="address">Ethereum wallet address</param>
    [HttpGet("{address}/eth-balance")]
    public async Task<ActionResult<object>> GetEthBalance(string address)
    {
        _logger.LogInformation("GetEthBalance: {Address}", address);

        try
        {
            var balance = await _walletService.GetEthBalanceAsync(address);
            return Ok(new { address, balance, unit = "ETH" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting ETH balance");
            return BadRequest(new { error = ex.Message });
        }
    }
}
