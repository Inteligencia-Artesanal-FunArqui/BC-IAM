namespace OsitoPolar.IAM.Service.Infrastructure.External.Http;

/// <summary>
/// HTTP Facade for communicating with the Notifications microservice.
/// Enables cross-service communication for sending emails.
/// </summary>
public interface INotificationsHttpFacade
{
    /// <summary>
    /// Send a templated email via the Notifications microservice.
    /// </summary>
    /// <param name="to">Recipient email address</param>
    /// <param name="toName">Recipient name</param>
    /// <param name="templateName">Template name (Welcome, TwoFactorSetup, CredentialDelivery, etc.)</param>
    /// <param name="templateData">Template data dictionary</param>
    /// <returns>True if email was sent successfully</returns>
    Task<bool> SendTemplatedEmailAsync(
        string to,
        string toName,
        string templateName,
        Dictionary<string, object> templateData);

    /// <summary>
    /// Send a simple email via the Notifications microservice.
    /// </summary>
    Task<bool> SendSimpleEmailAsync(
        string to,
        string toName,
        string subject,
        string body);
}
