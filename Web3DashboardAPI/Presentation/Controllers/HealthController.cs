using Microsoft.AspNetCore.Mvc;
using Web3DashboardAPI.Application.Services;

namespace Web3DashboardAPI.Presentation.Controllers;

/// <summary>
/// Health check and system status endpoints
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly IWeb3Service _web3Service;
    private readonly ILogger<HealthController> _logger;

    public HealthController(IWeb3Service web3Service, ILogger<HealthController> logger)
    {
        _web3Service = web3Service;
        _logger = logger;
    }

    /// <summary>
    /// Basic health check
    /// </summary>
    [HttpGet]
    public IActionResult Health()
    {
        return Ok(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            service = "Web3 Dashboard API"
        });
    }

    /// <summary>
    /// Check Ethereum network connectivity
    /// </summary>
    [HttpGet("network")]
    public async Task<IActionResult> NetworkStatus()
    {
        _logger.LogInformation("Checking network status");

        try
        {
            var isConnected = await _web3Service.IsNetworkConnectedAsync();
            var networkId = await _web3Service.GetNetworkIdAsync();

            if (isConnected)
            {
                return Ok(new
                {
                    status = "connected",
                    networkId,
                    timestamp = DateTime.UtcNow
                });
            }
            else
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable,
                    new { status = "disconnected", error = "Cannot connect to Ethereum RPC" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Network status check failed");
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { status = "error", error = ex.Message });
        }
    }
}
