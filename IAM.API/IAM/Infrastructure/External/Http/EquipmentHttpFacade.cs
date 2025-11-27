using System.Text.Json;

namespace OsitoPolar.IAM.Service.Infrastructure.External.Http;

/// <summary>
/// HTTP Facade implementation for Equipment microservice communication.
/// Makes HTTP calls to Equipment service to fetch equipment statistics.
/// </summary>
public class EquipmentHttpFacade : IEquipmentHttpFacade
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<EquipmentHttpFacade> _logger;

    public EquipmentHttpFacade(HttpClient httpClient, ILogger<EquipmentHttpFacade> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<int> CountEquipmentByOwnerId(int ownerId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/v1/equipments/count/owner/{ownerId}");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<CountResponse>(content,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return result?.Count ?? 0;
            }

            _logger.LogWarning("[IAM->Equipment] Failed to get equipment count for owner {OwnerId}: {StatusCode}",
                ownerId, response.StatusCode);
            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[IAM->Equipment] Error counting equipment for owner {OwnerId}", ownerId);
            return 0;
        }
    }

    public async Task<IEnumerable<int>> FetchEquipmentIdsByOwnerId(int ownerId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/v1/equipments/ids/owner/{ownerId}");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<List<int>>(content,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return result ?? new List<int>();
            }

            _logger.LogWarning("[IAM->Equipment] Failed to get equipment IDs for owner {OwnerId}: {StatusCode}",
                ownerId, response.StatusCode);
            return new List<int>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[IAM->Equipment] Error fetching equipment IDs for owner {OwnerId}", ownerId);
            return new List<int>();
        }
    }

    // DTOs for deserialization
    private record CountResponse(int Count);
}
