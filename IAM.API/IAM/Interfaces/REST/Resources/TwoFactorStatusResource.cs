namespace OsitoPolar.IAM.Service.Interfaces.REST.Resources;

/// <summary>
/// Resource containing the 2FA status of a user
/// </summary>
/// <param name="Username">The username of the user</param>
/// <param name="TwoFactorEnabled">Whether 2FA is currently enabled</param>
/// <param name="TwoFactorConfigured">Whether 2FA has been configured (secret exists)</param>
public record TwoFactorStatusResource(
    string Username,
    bool TwoFactorEnabled,
    bool TwoFactorConfigured
);
