using Nethereum.Web3;
using Web3DashboardAPI.Infrastructure.Configuration;

namespace Web3DashboardAPI.Application.Services;

/// <summary>
/// Core Web3 service - manages Ethereum RPC connections
/// </summary>
public interface IWeb3Service
{
    IWeb3 GetWeb3Instance();
    Task<string> GetNetworkIdAsync();
    Task<bool> IsNetworkConnectedAsync();
}

public class Web3Service : IWeb3Service
{
    private readonly Web3Configuration _config;
    private readonly ILogger<Web3Service> _logger;
    private IWeb3? _web3Instance;

    public Web3Service(Web3Configuration config, ILogger<Web3Service> logger)
    {
        _config = config;
        _logger = logger;
    }

    public IWeb3 GetWeb3Instance()
    {
        _web3Instance ??= new Web3(_config.RpcUrl);
        return _web3Instance;
    }

    public async Task<string> GetNetworkIdAsync()
    {
        try
        {
            var web3 = GetWeb3Instance();
            var version = await web3.Net.Version.SendRequestAsync();
            _logger.LogInformation("Connected to network: {NetworkId}", version);
            return version;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get network ID");
            throw;
        }
    }

    public async Task<bool> IsNetworkConnectedAsync()
    {
        try
        {
            var web3 = GetWeb3Instance();
            var blockNumber = await web3.Eth.Blocks.GetBlockNumber.SendRequestAsync();
            return blockNumber.Value > 0;
        }
        catch
        {
            return false;
        }
    }
}
