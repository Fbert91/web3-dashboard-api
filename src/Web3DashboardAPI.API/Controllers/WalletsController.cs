using Microsoft.AspNetCore.Mvc;
using Web3DashboardAPI.Application.Services;
using Web3DashboardAPI.Domain.Dtos;
using Web3DashboardAPI.Infrastructure.Validation;

namespace Web3DashboardAPI.API.Controllers;

/// <summary>
/// Controller for wallet-related operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class WalletsController : ControllerBase
{
    private readonly IWalletService _walletService;
    private readonly ILogger<WalletsController> _logger;

    public WalletsController(IWalletService walletService, ILogger<WalletsController> logger)
    {
        _walletService = walletService;
        _logger = logger;
    }

    /// <summary>
    /// Add or retrieve a wallet.
    /// </summary>
    /// <param name="request">Wallet address and optional name</param>
    /// <returns>Wallet details with current balances</returns>
    /// <response code="200">Wallet retrieved or created successfully</response>
    /// <response code="400">Invalid wallet address or input validation failed</response>
    [HttpPost("add")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<WalletResponse>> AddWallet([FromBody] AddWalletRequest request)
    {
        try
        {
            // Validate model
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors);
                var errorMessage = string.Join("; ", errors.Select(e => e.ErrorMessage));
                return BadRequest(new { error = errorMessage });
            }

            // Additional model validation
            InputValidator.ValidateModel(request);

            var wallet = await _walletService.AddOrGetWalletAsync(request.Address, request.Name);
            return Ok(wallet);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning($"Invalid wallet address: {ex.Message}");
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning($"Error adding wallet: {ex.Message}");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error adding wallet: {ex}");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get wallet details including ETH and token balances.
    /// </summary>
    /// <param name="address">Ethereum wallet address</param>
    /// <returns>Wallet details with current balances</returns>
    /// <response code="200">Wallet found</response>
    /// <response code="404">Wallet not found</response>
    /// <response code="400">Invalid wallet address</response>
    [HttpGet("{address}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<WalletResponse>> GetWallet(string address)
    {
        try
        {
            // Validate address format
            if (string.IsNullOrWhiteSpace(address))
                return BadRequest(new { error = "Address is required" });

            var wallet = await _walletService.GetWalletAsync(address);
            return Ok(wallet);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning($"Invalid wallet address: {ex.Message}");
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning($"Wallet not found: {ex.Message}");
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error retrieving wallet: {ex}");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get balance for a specific wallet.
    /// </summary>
    /// <param name="address">Ethereum wallet address</param>
    /// <returns>ETH balance and token balances</returns>
    /// <response code="200">Balance retrieved successfully</response>
    /// <response code="400">Invalid wallet address</response>
    /// <response code="404">Wallet not found</response>
    [HttpGet("{address}/balance")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GetBalance(string address)
    {
        try
        {
            var wallet = await _walletService.GetWalletAsync(address);
            return Ok(new
            {
                address = wallet.Address,
                ethBalance = wallet.FormattedEthBalance,
                ethBalanceWei = wallet.EthBalance,
                tokens = wallet.TokenBalances
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Sync wallet balances with the blockchain.
    /// </summary>
    /// <param name="address">Ethereum wallet address</param>
    /// <returns>Updated wallet details</returns>
    /// <response code="200">Wallet synced successfully</response>
    /// <response code="400">Invalid wallet address</response>
    /// <response code="404">Wallet not found</response>
    [HttpPost("{address}/sync")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WalletResponse>> SyncWallet(string address)
    {
        try
        {
            await _walletService.SyncWalletBalancesAsync(address);
            var wallet = await _walletService.GetWalletAsync(address);
            return Ok(wallet);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Add a token to track for a wallet.
    /// </summary>
    /// <param name="address">Wallet address</param>
    /// <param name="tokenAddress">ERC20 token contract address</param>
    /// <returns>Updated wallet with new token</returns>
    /// <response code="200">Token added successfully</response>
    /// <response code="400">Invalid addresses</response>
    /// <response code="404">Wallet not found</response>
    [HttpPost("{address}/tokens/add")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WalletResponse>> AddToken(string address, [FromQuery] string tokenAddress)
    {
        try
        {
            await _walletService.AddTokenAsync(address, tokenAddress);
            var wallet = await _walletService.GetWalletAsync(address);
            return Ok(wallet);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get transaction history for a wallet.
    /// </summary>
    /// <param name="address">Wallet address</param>
    /// <param name="limit">Maximum number of transactions (default 50)</param>
    /// <returns>List of transactions</returns>
    /// <response code="200">Transactions retrieved</response>
    /// <response code="400">Invalid wallet address</response>
    [HttpGet("{address}/transactions")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<TransactionResponse>>> GetTransactions(string address, [FromQuery] int limit = 50)
    {
        try
        {
            if (limit > 1000) limit = 1000; // Cap the limit
            if (limit < 1) limit = 1;

            var transactions = await _walletService.GetTransactionHistoryAsync(address, limit);
            return Ok(transactions);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
