namespace OsitoPolar.IAM.Service.Domain.Model.Commands;

/// <summary>
/// Command to verify a two-factor authentication code
/// </summary>
/// <param name="Username">The username of the user</param>
/// <param name="Code">The 6-digit TOTP code from Google Authenticator</param>
public record VerifyTwoFactorCommand(string Username, string Code);
