using System.Net.Http.Json;
using OsitoPolar.IAM.Service.Shared.Interfaces.ACL;

namespace OsitoPolar.IAM.Service.Application.ACL.Services;

/// <summary>
/// HTTP Facade for communication with Subscriptions Service
/// Implements ISubscriptionContextFacade interface - ALL 7 methods from monolith
/// </summary>
public class SubscriptionsHttpFacade : ISubscriptionContextFacade
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<SubscriptionsHttpFacade> _logger;
    private const decimal ServiceCommissionRate = 0.15m; // 15%

    public SubscriptionsHttpFacade(HttpClient httpClient, ILogger<SubscriptionsHttpFacade> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    #region User Subscription Management

    public async Task<int> GetUserSubscriptionPlanId(int userId)
    {
        // TODO: This requires an endpoint in Subscriptions Service to get plan by userId
        // For now, return 0 (not found)
        _logger.LogWarning("GetUserSubscriptionPlanId not implemented - requires Subscriptions Service endpoint GET /api/v1/subscriptions/user/{userId}/plan-id");
        return 0;
    }

    public async Task<bool> CanUserAddMoreClients(int userId)
    {
        // TODO: This requires business logic in Subscriptions Service to check client count vs plan limits
        _logger.LogWarning("CanUserAddMoreClients not implemented - requires Subscriptions Service endpoint");
        return true; // Default to allowing
    }

    #endregion

    #region Commission Calculation (No HTTP needed)

    public decimal CalculateServiceCommission(decimal amount)
    {
        return amount * ServiceCommissionRate;
    }

    #endregion

    #region Subscription Plan Data (CRITICAL FOR REGISTRATION)

    /// <summary>
    /// Get subscription plan name - USED IN REGISTRATION FLOW
    /// </summary>
    public async Task<string> FetchSubscriptionPlanName(int planId)
    {
        try
        {
            _logger.LogInformation("Fetching subscription plan name for plan {PlanId}", planId);

            var subscriptionData = await GetSubscriptionDataById(planId);
            return subscriptionData?.planName ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch subscription plan name for plan {PlanId}", planId);
            return string.Empty;
        }
    }

    /// <summary>
    /// Get subscription data by plan ID - USED IN REGISTRATION FLOW
    /// </summary>
    public async Task<(int planId, string planName, decimal price, int maxClients)?> GetSubscriptionDataById(int planId)
    {
        try
        {
            _logger.LogInformation("Fetching subscription data for plan {PlanId}", planId);

            var response = await _httpClient.GetAsync($"/api/v1/subscriptions/{planId}/data");

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Subscription plan {PlanId} not found", planId);
                return null;
            }

            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<SubscriptionDataResponse>();

            if (result == null)
            {
                _logger.LogError("Failed to deserialize subscription data for plan {PlanId}", planId);
                return null;
            }

            return (result.PlanId, result.PlanName, result.Price, result.MaxClients);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to fetch subscription data for plan {PlanId}", planId);
            return null;
        }
    }

    /// <summary>
    /// Get subscription limits for registration validation - USED IN REGISTRATION FLOW
    /// </summary>
    public async Task<(int maxEquipment, int maxClients)?> GetSubscriptionLimits(int planId)
    {
        try
        {
            _logger.LogInformation("Fetching subscription limits for plan {PlanId}", planId);

            var response = await _httpClient.GetAsync($"/api/v1/subscriptions/{planId}/limits");

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Subscription plan {PlanId} not found", planId);
                return null;
            }

            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<SubscriptionLimitsResponse>();

            if (result == null)
            {
                _logger.LogError("Failed to deserialize subscription limits for plan {PlanId}", planId);
                return null;
            }

            return (result.MaxEquipment, result.MaxClients);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to fetch subscription limits for plan {PlanId}", planId);
            return null;
        }
    }

    /// <summary>
    /// Get full subscription data including all fields
    /// </summary>
    public async Task<(int planId, string planName, decimal price, string currency, int? maxEquipment, int? maxClients)?> GetFullSubscriptionData(int planId)
    {
        try
        {
            _logger.LogInformation("Fetching full subscription data for plan {PlanId}", planId);

            var response = await _httpClient.GetAsync($"/api/v1/subscriptions/{planId}");

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Subscription plan {PlanId} not found", planId);
                return null;
            }

            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<FullSubscriptionResponse>();

            if (result == null)
            {
                _logger.LogError("Failed to deserialize full subscription data for plan {PlanId}", planId);
                return null;
            }

            return (result.Id, result.PlanName, result.Price, "USD", result.MaxEquipment, result.MaxClients);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to fetch full subscription data for plan {PlanId}", planId);
            return null;
        }
    }

    #endregion
}

#region DTOs for HTTP Communication with Subscriptions Service

/// <summary>
/// Response from GET /api/v1/subscriptions/{planId}/data
/// </summary>
internal record SubscriptionDataResponse(
    int PlanId,
    string PlanName,
    decimal Price,
    int MaxClients
);

/// <summary>
/// Response from GET /api/v1/subscriptions/{planId}/limits
/// </summary>
internal record SubscriptionLimitsResponse(
    int MaxEquipment,
    int MaxClients
);

/// <summary>
/// Response from GET /api/v1/subscriptions/{planId} (full data)
/// </summary>
internal record FullSubscriptionResponse(
    int Id,
    string PlanName,
    decimal Price,
    string BillingCycle,
    int? MaxEquipment,
    int? MaxClients,
    List<string> Features
);

#endregion
