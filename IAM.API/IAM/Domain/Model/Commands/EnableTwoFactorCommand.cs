namespace OsitoPolar.IAM.Service.Domain.Model.Commands;

/// <summary>
/// Command to enable two-factor authentication for a user
/// </summary>
/// <param name="Username">The username of the user</param>
/// <param name="Code">The 6-digit TOTP code to verify the setup</param>
public record EnableTwoFactorCommand(string Username, string Code);
