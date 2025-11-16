using System.Text;
using System.Text.Json;

namespace OsitoPolar.IAM.Service.Infrastructure.External.Http;

/// <summary>
/// HTTP Facade implementation for communicating with the Notifications microservice.
/// </summary>
public class NotificationsHttpFacade : INotificationsHttpFacade
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<NotificationsHttpFacade> _logger;

    public NotificationsHttpFacade(HttpClient httpClient, ILogger<NotificationsHttpFacade> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<bool> SendTemplatedEmailAsync(
        string to,
        string toName,
        string templateName,
        Dictionary<string, object> templateData)
    {
        try
        {
            var requestBody = new
            {
                to = to,
                toName = toName,
                templateName = templateName,
                templateData = templateData
            };

            var jsonContent = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/api/v1/notifications/emails/templated", content);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "Successfully sent templated email to {Email} with template {Template}",
                    to, templateName);
                return true;
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogWarning(
                "Failed to send templated email to {Email}. Status: {StatusCode}, Error: {Error}",
                to, response.StatusCode, errorContent);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Exception occurred while sending templated email to {Email} with template {Template}",
                to, templateName);
            return false;
        }
    }

    public async Task<bool> SendSimpleEmailAsync(
        string to,
        string toName,
        string subject,
        string body)
    {
        try
        {
            var requestBody = new
            {
                to = to,
                toName = toName,
                subject = subject,
                body = body
            };

            var jsonContent = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/api/v1/notifications/emails/simple", content);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "Successfully sent simple email to {Email} with subject {Subject}",
                    to, subject);
                return true;
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogWarning(
                "Failed to send simple email to {Email}. Status: {StatusCode}, Error: {Error}",
                to, response.StatusCode, errorContent);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Exception occurred while sending simple email to {Email}",
                to);
            return false;
        }
    }
}
