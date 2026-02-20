using Nethereum.Util;
using Web3DashboardAPI.Application.DTOs;

namespace Web3DashboardAPI.Application.Services;

/// <summary>
/// Wallet management and validation service
/// </summary>
public interface IWalletService
{
    Task<WalletValidationDto> ValidateWalletAsync(string address);
    Task<WalletBalanceDto> GetWalletBalanceAsync(string address);
    Task<string> GetEthBalanceAsync(string address);
}

public class WalletService : IWalletService
{
    private readonly IWeb3Service _web3Service;
    private readonly ILogger<WalletService> _logger;

    public WalletService(IWeb3Service web3Service, ILogger<WalletService> logger)
    {
        _web3Service = web3Service;
        _logger = logger;
    }

    public async Task<WalletValidationDto> ValidateWalletAsync(string address)
    {
        _logger.LogInformation("Validating wallet: {Address}", address);

        var validation = new WalletValidationDto { Address = address };

        if (string.IsNullOrWhiteSpace(address))
        {
            validation.IsValid = false;
            validation.Message = "Address cannot be empty";
            return validation;
        }

        // Check if valid Ethereum address format (0x + 40 hex chars)
        if (!address.StartsWith("0x") || address.Length != 42)
        {
            validation.IsValid = false;
            validation.Message = "Invalid Ethereum address format (must be 0x + 40 hex characters)";
            return validation;
        }

        // Use Nethereum's AddressUtil to validate
        try
        {
            if (!AddressUtil.Current.IsValidEthereumAddressHexFormat(address))
            {
                validation.IsValid = false;
                validation.Message = "Invalid Ethereum address format";
                return validation;
            }

            // Try to get code at address to ensure it's on chain
            var web3 = _web3Service.GetWeb3Instance();
            var code = await web3.Eth.GetCode.SendRequestAsync(address);

            validation.IsValid = true;
            validation.Message = code.Length > 2 ? "Smart contract detected" : "EOA (Externally Owned Account)";

            _logger.LogInformation("Wallet validation successful for: {Address}", address);
            return validation;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating wallet: {Address}", address);
            validation.IsValid = false;
            validation.Message = "Error validating wallet";
            return validation;
        }
    }

    public async Task<WalletBalanceDto> GetWalletBalanceAsync(string address)
    {
        _logger.LogInformation("Getting balance for wallet: {Address}", address);

        var walletBalance = new WalletBalanceDto { Address = address };

        try
        {
            walletBalance.EthBalance = await GetEthBalanceAsync(address);
            return walletBalance;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting wallet balance: {Address}", address);
            throw;
        }
    }

    public async Task<string> GetEthBalanceAsync(string address)
    {
        var web3 = _web3Service.GetWeb3Instance();
        
        try
        {
            var balanceWei = await web3.Eth.GetBalance.SendRequestAsync(address);
            // Convert Wei to Ether (1 ETH = 10^18 Wei)
            var balanceEth = Nethereum.Util.UnitConversion.Convert.FromWei(balanceWei);
            return balanceEth.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting ETH balance for: {Address}", address);
            throw;
        }
    }
}
