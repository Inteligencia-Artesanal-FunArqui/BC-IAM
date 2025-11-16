namespace OsitoPolar.IAM.Service.Shared.Interfaces.ACL;

/// <summary>
/// Facade interface for communication with Subscriptions Service
/// </summary>
public interface ISubscriptionsContextFacade
{
    /// <summary>
    /// Gets the subscription plan for a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>Subscription data or null if not found</returns>
    Task<SubscriptionDto?> GetUserSubscriptionAsync(int userId);

    /// <summary>
    /// Assigns a default plan to a new user
    /// </summary>
    /// <param name="userId">User ID</param>
    Task AssignDefaultPlanAsync(int userId);
}

/// <summary>
/// DTO for Subscription data from Subscriptions Service
/// </summary>
public record SubscriptionDto(int Id, string PlanName, int MaxClients);
