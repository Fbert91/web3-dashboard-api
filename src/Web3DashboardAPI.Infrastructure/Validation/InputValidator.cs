using System.ComponentModel.DataAnnotations;
using Web3DashboardAPI.Infrastructure.Utilities;

namespace Web3DashboardAPI.Infrastructure.Validation;

/// <summary>
/// Validates Ethereum address format and checksum.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class EthereumAddressAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
        {
            return new ValidationResult("Address is required");
        }

        var address = value.ToString()!;

        if (!AddressValidator.IsValidAddress(address))
        {
            return new ValidationResult("Invalid Ethereum address or invalid checksum");
        }

        return ValidationResult.Success;
    }
}

/// <summary>
/// Validates that a string is not empty or whitespace.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class RequiredAddressAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
        {
            return new ValidationResult($"{validationContext.DisplayName} is required");
        }

        return ValidationResult.Success;
    }
}

/// <summary>
/// Validates string length within bounds.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class ValidNameAttribute : ValidationAttribute
{
    private const int MaxLength = 100;
    private const int MinLength = 1;

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value == null)
            return ValidationResult.Success; // Name is optional

        var str = value.ToString()!;

        if (str.Length < MinLength || str.Length > MaxLength)
        {
            return new ValidationResult($"Name must be between {MinLength} and {MaxLength} characters");
        }

        // Check for invalid characters
        if (str.Any(c => char.IsControl(c)))
        {
            return new ValidationResult("Name contains invalid characters");
        }

        return ValidationResult.Success;
    }
}

/// <summary>
/// Input validation helper.
/// </summary>
public static class InputValidator
{
    /// <summary>
    /// Validates an API request model.
    /// </summary>
    public static void ValidateModel<T>(T model) where T : class
    {
        if (model == null)
            throw new ArgumentException("Model cannot be null");

        var context = new ValidationContext(model);
        var results = new List<ValidationResult>();

        if (!Validator.TryValidateObject(model, context, results, validateAllProperties: true))
        {
            var errors = string.Join("; ", results.Select(r => r.ErrorMessage));
            throw new ValidationException($"Validation failed: {errors}");
        }
    }

    /// <summary>
    /// Sanitizes an Ethereum address (normalizes and optionally checksums).
    /// </summary>
    public static string SanitizeAddress(string address, bool useChecksum = false)
    {
        var normalized = AddressValidator.NormalizeAddress(address);

        if (useChecksum)
        {
            try
            {
                return AddressValidator.ToChecksumAddress(normalized);
            }
            catch
            {
                return normalized;
            }
        }

        return normalized;
    }

    /// <summary>
    /// Sanitizes a name string (removes leading/trailing whitespace, limits length).
    /// </summary>
    public static string SanitizeName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return string.Empty;

        return name.Trim().Substring(0, Math.Min(100, name.Trim().Length));
    }
}
