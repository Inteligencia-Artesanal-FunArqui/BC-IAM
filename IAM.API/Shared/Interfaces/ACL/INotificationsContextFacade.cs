namespace OsitoPolar.IAM.Service.Shared.Interfaces.ACL;

/// <summary>
/// Facade for the Notifications context
/// COPIED EXACTLY FROM MONOLITH - DO NOT MODIFY
/// </summary>
public interface INotificationContextFacade
{
    /// <summary>
    /// Send email notification
    /// </summary>
    /// <param name="to">Recipient email</param>
    /// <param name="recipientName">Recipient name (optional)</param>
    /// <param name="subject">Email subject</param>
    /// <param name="body">Email body (HTML or plain text)</param>
    /// <returns>True if email sent successfully, false otherwise</returns>
    Task<bool> SendEmailNotification(string to, string recipientName, string subject, string body);

    /// <summary>
    /// Create in-app notification for a user
    /// </summary>
    /// <param name="userId">User ID from IAM</param>
    /// <param name="title">Notification title</param>
    /// <param name="message">Notification message</param>
    /// <returns>Notification ID if created, 0 otherwise</returns>
    Task<int> CreateInAppNotification(int userId, string title, string message);

    /// <summary>
    /// Mark in-app notification as read
    /// </summary>
    /// <param name="notificationId">Notification ID</param>
    /// <returns>True if marked as read, false otherwise</returns>
    Task<bool> MarkNotificationAsRead(int notificationId);
}
