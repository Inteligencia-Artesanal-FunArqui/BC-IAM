namespace OsitoPolar.IAM.Service.Infrastructure.External.Http;

/// <summary>
/// HTTP Facade for communicating with the Subscriptions microservice.
/// Used during registration to validate subscription plans.
/// </summary>
public interface ISubscriptionsHttpFacade
{
    /// <summary>
    /// Get full subscription plan data including price, limits, etc.
    /// Used during registration to validate and get plan details.
    /// Returns (planId, planName, price, currency, maxEquipment, maxClients)
    /// </summary>
    Task<(int planId, string planName, decimal price, string currency, int? maxEquipment, int? maxClients)?> GetFullSubscriptionData(int planId);

    /// <summary>
    /// Get subscription plan data by plan ID.
    /// Returns plan ID, name, price, currency, max equipment, and max clients.
    /// Endpoint: GET /api/v1/subscriptions/plans/{planId}
    /// </summary>
    Task<(int planId, string planName, decimal price, string currency, int? maxEquipment, int? maxClients)?> GetSubscriptionDataById(int planId);

    /// <summary>
    /// Get subscription limits for a plan.
    /// Returns max equipment and max clients.
    /// Endpoint: GET /api/v1/subscriptions/plans/{planId}/limits
    /// </summary>
    Task<(int? maxEquipment, int? maxClients)?> GetSubscriptionLimits(int planId);
}
