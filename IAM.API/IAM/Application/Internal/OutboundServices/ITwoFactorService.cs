namespace OsitoPolar.IAM.Service.Application.Internal.OutboundServices;

/// <summary>
/// Service for managing Two-Factor Authentication (2FA) operations
/// </summary>
public interface ITwoFactorService
{
    /// <summary>
    /// Generate a new 2FA secret and QR code for a user
    /// </summary>
    /// <param name="username">The username (email) of the user</param>
    /// <returns>A result containing the secret, QR code data URL, and manual entry key</returns>
    TwoFactorSetupResult GenerateTwoFactorSecret(string username);

    /// <summary>
    /// Validate a TOTP code against a user's secret
    /// </summary>
    /// <param name="secret">The Base32-encoded secret key</param>
    /// <param name="code">The 6-digit TOTP code to validate</param>
    /// <returns>True if the code is valid, false otherwise</returns>
    bool ValidateTwoFactorCode(string secret, string code);
}

/// <summary>
/// Result of generating a 2FA secret
/// </summary>
public record TwoFactorSetupResult
{
    /// <summary>
    /// The Base32-encoded secret key to be stored in the database
    /// </summary>
    public required string Secret { get; init; }

    /// <summary>
    /// The QR code as a data URL (data:image/png;base64,...)
    /// </summary>
    public required string QrCodeDataUrl { get; init; }

    /// <summary>
    /// The secret formatted for manual entry (e.g., "JBSW Y3DP EHPK 3PXP")
    /// </summary>
    public required string ManualEntryKey { get; init; }
}
