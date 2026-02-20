namespace Web3DashboardAPI.Infrastructure.Configuration;

/// <summary>
/// Web3 configuration for Ethereum network connectivity
/// </summary>
public class Web3Configuration
{
    public string RpcUrl { get; set; } = null!;
    public string NetworkId { get; set; } = null!;
    public string ChainName { get; set; } = null!;
    public string? EtherscanApiUrl { get; set; }
}
