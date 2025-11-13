namespace OsitoPolar.IAM.Service.Interfaces.REST.Resources;

/// <summary>
/// Resource containing 2FA setup information
/// </summary>
/// <param name="QrCodeDataUrl">QR code as data URL for scanning with Google Authenticator</param>
/// <param name="ManualEntryKey">Secret formatted for manual entry</param>
/// <param name="Message">Instructions for the user</param>
public record TwoFactorSetupResource(
    string QrCodeDataUrl,
    string ManualEntryKey,
    string Message
);
