using OtpNet;
using QRCoder;
using OsitoPolar.IAM.Service.Application.Internal.OutboundServices;

namespace OsitoPolar.IAM.Service.Infrastructure.Security;

/// <summary>
/// Implementation of ITwoFactorService using OtpNet and QRCoder
/// </summary>
public class TwoFactorService : ITwoFactorService
{
    private const string Issuer = "OsitoPolar";

    /// <summary>
    /// Generate a new 2FA secret and QR code for a user
    /// </summary>
    /// <param name="username">The username (email) of the user</param>
    /// <returns>A result containing the secret, QR code data URL, and manual entry key</returns>
    public TwoFactorSetupResult GenerateTwoFactorSecret(string username)
    {
        // Generate a random secret key (160 bits / 20 bytes)
        var secretKey = KeyGeneration.GenerateRandomKey(20);
        var base32Secret = Base32Encoding.ToString(secretKey);

        // Generate the otpauth URL for Google Authenticator
        // Format: otpauth://totp/OsitoPolar:username?secret=SECRET&issuer=OsitoPolar
        var label = username;
        var otpUrl = $"otpauth://totp/{Uri.EscapeDataString(Issuer)}:{Uri.EscapeDataString(label)}?secret={base32Secret}&issuer={Uri.EscapeDataString(Issuer)}";

        // Generate QR code
        using var qrGenerator = new QRCodeGenerator();
        var qrCodeData = qrGenerator.CreateQrCode(otpUrl, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(qrCodeData);
        var qrCodeBytes = qrCode.GetGraphic(20);
        var qrCodeBase64 = Convert.ToBase64String(qrCodeBytes);
        var qrCodeDataUrl = $"data:image/png;base64,{qrCodeBase64}";

        // Format secret for manual entry (groups of 4 characters)
        var manualEntryKey = FormatSecretForDisplay(base32Secret);

        return new TwoFactorSetupResult
        {
            Secret = base32Secret,
            QrCodeDataUrl = qrCodeDataUrl,
            ManualEntryKey = manualEntryKey
        };
    }

    /// <summary>
    /// Validate a TOTP code against a user's secret
    /// </summary>
    /// <param name="secret">The Base32-encoded secret key</param>
    /// <param name="code">The 6-digit TOTP code to validate</param>
    /// <returns>True if the code is valid, false otherwise</returns>
    public bool ValidateTwoFactorCode(string secret, string code)
    {
        try
        {
            // Decode the Base32 secret
            var secretBytes = Base32Encoding.ToBytes(secret);

            // Create a TOTP instance
            var totp = new Totp(secretBytes);

            // Validate the code with a time window of ±1 period (30 seconds each)
            // This allows for clock skew between server and client
            var isValid = totp.VerifyTotp(code, out _, new VerificationWindow(previous: 1, future: 1));

            return isValid;
        }
        catch (Exception)
        {
            // Invalid secret format or other error
            return false;
        }
    }

    /// <summary>
    /// Format a Base32 secret for manual entry by grouping into sets of 4 characters
    /// </summary>
    /// <param name="secret">The Base32 secret</param>
    /// <returns>Formatted secret (e.g., "JBSW Y3DP EHPK 3PXP")</returns>
    private static string FormatSecretForDisplay(string secret)
    {
        var formatted = new List<string>();
        for (var i = 0; i < secret.Length; i += 4)
        {
            var remaining = secret.Length - i;
            var length = Math.Min(4, remaining);
            formatted.Add(secret.Substring(i, length));
        }
        return string.Join(" ", formatted);
    }
}
