namespace OsitoPolar.IAM.Service.Interfaces.REST.Resources;

/// <summary>
/// Resource for verifying a two-factor authentication code
/// </summary>
/// <param name="Username">The username of the user</param>
/// <param name="Code">The 6-digit TOTP code from Google Authenticator</param>
public record VerifyTwoFactorResource(string Username, string Code);
