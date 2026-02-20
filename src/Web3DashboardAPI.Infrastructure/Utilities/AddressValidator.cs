using Nethereum.Util;
using System.Text.RegularExpressions;

namespace Web3DashboardAPI.Infrastructure.Utilities;

/// <summary>
/// Utility for validating and converting Ethereum addresses with checksum support.
/// </summary>
public static class AddressValidator
{
    /// <summary>
    /// Validates an Ethereum address with checksum validation.
    /// </summary>
    /// <param name="address">Address to validate</param>
    /// <returns>True if valid (checksummed or all lowercase/uppercase)</returns>
    public static bool IsValidAddress(string address)
    {
        if (string.IsNullOrWhiteSpace(address))
            return false;

        // Check basic format
        if (!address.IsValidEthereumAddressHexFormat())
            return false;

        // If it has mixed case, it should have valid checksum
        if (HasMixedCase(address))
        {
            return IsValidChecksum(address);
        }

        return true;
    }

    /// <summary>
    /// Converts an address to its checksummed form.
    /// </summary>
    /// <param name="address">Address to checksum</param>
    /// <returns>Checksummed address</returns>
    /// <exception cref="ArgumentException">If address is invalid</exception>
    public static string ToChecksumAddress(string address)
    {
        if (!address.IsValidEthereumAddressHexFormat())
            throw new ArgumentException("Invalid Ethereum address format", nameof(address));

        return Nethereum.Util.AddressUtil.Current.ConvertToChecksumAddress(address);
    }

    /// <summary>
    /// Normalizes an address (lowercase, removes spaces).
    /// </summary>
    /// <param name="address">Address to normalize</param>
    /// <returns>Normalized address</returns>
    public static string NormalizeAddress(string address)
    {
        if (string.IsNullOrWhiteSpace(address))
            return address;

        return address.Trim().ToLowerInvariant();
    }

    /// <summary>
    /// Checks if address has mixed case (checksum format).
    /// </summary>
    private static bool HasMixedCase(string address)
    {
        // Remove 0x prefix for checking
        var addressPart = address.Length > 2 && address.StartsWith("0x") 
            ? address[2..] 
            : address;

        var hasUpperCase = addressPart.Any(char.IsUpper);
        var hasLowerCase = addressPart.Any(char.IsLower);

        return hasUpperCase && hasLowerCase;
    }

    /// <summary>
    /// Validates checksum of an address.
    /// </summary>
    private static bool IsValidChecksum(string address)
    {
        try
        {
            var checksummed = Nethereum.Util.AddressUtil.Current.ConvertToChecksumAddress(address);
            return checksummed == address;
        }
        catch
        {
            return false;
        }
    }
}
