using Microsoft.AspNetCore.Mvc;
using Web3DashboardAPI.Application.DTOs;
using Web3DashboardAPI.Application.Services;

namespace Web3DashboardAPI.Presentation.Controllers;

/// <summary>
/// Smart contract interaction API endpoints
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ContractController : ControllerBase
{
    private readonly ISmartContractService _contractService;
    private readonly ILogger<ContractController> _logger;

    public ContractController(ISmartContractService contractService, ILogger<ContractController> logger)
    {
        _contractService = contractService;
        _logger = logger;
    }

    /// <summary>
    /// Get ERC20 token balance for a wallet
    /// </summary>
    /// <param name="contractAddress">Token contract address</param>
    /// <param name="walletAddress">Wallet address to check balance for</param>
    [HttpGet("erc20/{contractAddress}/balance/{walletAddress}")]
    public async Task<ActionResult<object>> GetERC20Balance(string contractAddress, string walletAddress)
    {
        _logger.LogInformation("GetERC20Balance: contract={Contract}, wallet={Wallet}",
            contractAddress, walletAddress);

        try
        {
            var balance = await _contractService.GetERC20BalanceAsync(contractAddress, walletAddress);
            return Ok(new { contractAddress, walletAddress, balance });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting ERC20 balance");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get ERC20 token information (name, symbol, decimals)
    /// </summary>
    /// <param name="contractAddress">Token contract address</param>
    [HttpGet("erc20/{contractAddress}/info")]
    public async Task<ActionResult<ERC20TokenInfoDto>> GetERC20Info(string contractAddress)
    {
        _logger.LogInformation("GetERC20Info: {Contract}", contractAddress);

        try
        {
            var info = await _contractService.GetERC20InfoAsync(contractAddress);
            return Ok(info);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting ERC20 info");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Call a smart contract read-only function
    /// </summary>
    /// <param name="contractAddress">Contract address</param>
    /// <param name="functionName">Function name to call</param>
    [HttpPost("call")]
    public async Task<ActionResult<ContractCallResultDto>> CallFunction(
        [FromQuery] string contractAddress,
        [FromQuery] string functionName,
        [FromBody] object[] parameters = null!)
    {
        _logger.LogInformation("CallFunction: contract={Contract}, function={Function}",
            contractAddress, functionName);

        try
        {
            var result = await _contractService.CallContractFunctionAsync(
                contractAddress, functionName, parameters ?? Array.Empty<object>());
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling contract function");
            return BadRequest(new { error = ex.Message });
        }
    }
}
