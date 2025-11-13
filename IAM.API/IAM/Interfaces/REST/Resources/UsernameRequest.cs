namespace OsitoPolar.IAM.Service.Interfaces.REST.Resources;

/// <summary>
/// Resource containing just a username for API requests
/// </summary>
/// <param name="Username">The username of the user</param>
public record UsernameRequest(string Username);
