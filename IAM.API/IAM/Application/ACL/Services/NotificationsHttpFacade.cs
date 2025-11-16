using System.Net.Http.Json;
using OsitoPolar.IAM.Service.Shared.Interfaces.ACL;

namespace OsitoPolar.IAM.Service.Application.ACL.Services;

/// <summary>
/// HTTP Facade for communication with Notifications Service
/// Implements INotificationContextFacade interface - matches monolith exactly
/// </summary>
public class NotificationsHttpFacade : INotificationContextFacade
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<NotificationsHttpFacade> _logger;

    public NotificationsHttpFacade(HttpClient httpClient, ILogger<NotificationsHttpFacade> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>
    /// Send email notification - USED IN REGISTRATION FLOW
    /// </summary>
    public async Task<bool> SendEmailNotification(string to, string recipientName, string subject, string body)
    {
        try
        {
            _logger.LogInformation("Sending email to {To} with subject '{Subject}' via Notifications Service", to, subject);

            var request = new SendSimpleEmailRequest(
                To: to,
                ToName: recipientName ?? string.Empty,
                Subject: subject,
                Body: body
            );

            var response = await _httpClient.PostAsJsonAsync("/api/v1/notifications/emails/simple", request);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to send email to {To}, status code: {StatusCode}", to, response.StatusCode);
                return false;
            }

            _logger.LogInformation("Email sent successfully to {To}", to);
            return true;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to send email notification to {To}", to);
            return false;
        }
    }

    /// <summary>
    /// Create in-app notification for a user
    /// </summary>
    public async Task<int> CreateInAppNotification(int userId, string title, string message)
    {
        try
        {
            _logger.LogInformation("Creating in-app notification for user {UserId}", userId);

            var request = new CreateInAppNotificationRequest(
                UserId: userId,
                Title: title,
                Message: message
            );

            var response = await _httpClient.PostAsJsonAsync("/api/v1/notifications/in-app", request);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to create in-app notification for user {UserId}, status code: {StatusCode}", userId, response.StatusCode);
                return 0;
            }

            var result = await response.Content.ReadFromJsonAsync<InAppNotificationResponse>();

            if (result == null)
            {
                _logger.LogError("Failed to deserialize in-app notification response");
                return 0;
            }

            _logger.LogInformation("In-app notification created successfully for user {UserId}, notification ID: {NotificationId}", userId, result.Id);
            return result.Id;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to create in-app notification for user {UserId}", userId);
            return 0;
        }
    }

    /// <summary>
    /// Mark in-app notification as read
    /// </summary>
    public async Task<bool> MarkNotificationAsRead(int notificationId)
    {
        try
        {
            _logger.LogInformation("Marking notification {NotificationId} as read", notificationId);

            var response = await _httpClient.PatchAsync($"/api/v1/notifications/in-app/{notificationId}/read", null);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to mark notification {NotificationId} as read, status code: {StatusCode}", notificationId, response.StatusCode);
                return false;
            }

            _logger.LogInformation("Notification {NotificationId} marked as read successfully", notificationId);
            return true;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to mark notification {NotificationId} as read", notificationId);
            return false;
        }
    }
}

#region DTOs for HTTP Communication with Notifications Service

/// <summary>
/// Request to send simple email
/// </summary>
internal record SendSimpleEmailRequest(
    string To,
    string ToName,
    string Subject,
    string Body
);

/// <summary>
/// Request to create in-app notification
/// </summary>
internal record CreateInAppNotificationRequest(
    int UserId,
    string Title,
    string Message
);

/// <summary>
/// Response from creating in-app notification
/// </summary>
internal record InAppNotificationResponse(
    int Id
);

#endregion
