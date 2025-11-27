using System.Text.Json;

namespace OsitoPolar.IAM.Service.Infrastructure.External.Http;

/// <summary>
/// HTTP Facade implementation for ServiceRequests microservice communication.
/// Makes HTTP calls to ServiceRequests service to fetch statistics.
/// </summary>
public class ServiceRequestsHttpFacade : IServiceRequestsHttpFacade
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ServiceRequestsHttpFacade> _logger;

    public ServiceRequestsHttpFacade(HttpClient httpClient, ILogger<ServiceRequestsHttpFacade> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<int> CountActiveServiceRequestsByEquipmentId(int equipmentId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/v1/service-requests/count/equipment/{equipmentId}/active");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<CountResponse>(content,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return result?.Count ?? 0;
            }

            _logger.LogWarning("[IAM->ServiceRequests] Failed to get active service requests count for equipment {EquipmentId}: {StatusCode}",
                equipmentId, response.StatusCode);
            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[IAM->ServiceRequests] Error counting active service requests for equipment {EquipmentId}", equipmentId);
            return 0;
        }
    }

    public async Task<int> CountAllActiveServiceRequests()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/v1/service-requests/count/active");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<CountResponse>(content,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return result?.Count ?? 0;
            }

            _logger.LogWarning("[IAM->ServiceRequests] Failed to get all active service requests count: {StatusCode}",
                response.StatusCode);
            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[IAM->ServiceRequests] Error counting all active service requests");
            return 0;
        }
    }

    // DTOs for deserialization
    private record CountResponse(int Count);
}
