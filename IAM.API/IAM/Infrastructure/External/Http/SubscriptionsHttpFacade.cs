using System.Text.Json;

namespace OsitoPolar.IAM.Service.Infrastructure.External.Http;

/// <summary>
/// HTTP Facade implementation for Subscriptions microservice communication.
/// Makes HTTP calls to Subscriptions service to validate and fetch plan data during registration.
/// </summary>
public class SubscriptionsHttpFacade : ISubscriptionsHttpFacade
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<SubscriptionsHttpFacade> _logger;

    public SubscriptionsHttpFacade(HttpClient httpClient, ILogger<SubscriptionsHttpFacade> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<(int planId, string planName, decimal price, string currency, int? maxEquipment, int? maxClients)?> GetFullSubscriptionData(int planId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/v1/subscriptions/plans/{planId}");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var planData = JsonSerializer.Deserialize<SubscriptionPlanResponse>(content,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (planData != null)
                {
                    return (planData.Id, planData.PlanName, planData.Price, planData.Currency,
                            planData.MaxEquipment, planData.MaxClients);
                }
            }

            _logger.LogWarning("[IAM->Subscriptions] Failed to get subscription plan {PlanId}: {StatusCode}",
                planId, response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[IAM->Subscriptions] Error getting subscription plan {PlanId}", planId);
            return null;
        }
    }

    public async Task<(int planId, string planName, decimal price, string currency, int? maxEquipment, int? maxClients)?> GetSubscriptionDataById(int planId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/v1/subscriptions/plans/{planId}");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<SubscriptionPlanResponse>(content,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (result != null)
                    return (result.Id, result.PlanName, result.Price, result.Currency ?? "USD", result.MaxEquipment, result.MaxClients);
            }

            _logger.LogWarning("[IAM->Subscriptions] Failed to get subscription plan {PlanId}: {StatusCode}",
                planId, response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[IAM->Subscriptions] Error getting subscription plan {PlanId}", planId);
            return null;
        }
    }

    public async Task<(int? maxEquipment, int? maxClients)?> GetSubscriptionLimits(int planId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/v1/subscriptions/plans/{planId}/limits");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<SubscriptionLimitsResponse>(content,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (result != null)
                    return (result.MaxEquipment, result.MaxClients);
            }

            _logger.LogWarning("[IAM->Subscriptions] Failed to get subscription limits for plan {PlanId}: {StatusCode}",
                planId, response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[IAM->Subscriptions] Error getting subscription limits for plan {PlanId}", planId);
            return null;
        }
    }

    // DTOs for deserialization (matching Subscriptions service response)
    private record SubscriptionPlanResponse(
        int Id,
        string PlanName,
        decimal Price,
        string Currency,
        int? MaxEquipment,
        int? MaxClients);
    private record SubscriptionLimitsResponse(int? MaxEquipment, int? MaxClients);
}
