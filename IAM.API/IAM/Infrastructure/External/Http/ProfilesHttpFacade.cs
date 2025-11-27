using System.Text.Json;

namespace OsitoPolar.IAM.Service.Infrastructure.External.Http;

/// <summary>
/// HTTP Facade implementation for Profiles microservice communication.
/// Makes HTTP calls to Profiles service to fetch profile data during authentication.
/// </summary>
public class ProfilesHttpFacade : IProfilesHttpFacade
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ProfilesHttpFacade> _logger;

    public ProfilesHttpFacade(HttpClient httpClient, ILogger<ProfilesHttpFacade> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<(int ownerId, decimal balance, int planId, int maxUnits)?> GetOwnerProfileForAuthByUserId(int userId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/v1/profiles/owners/auth/{userId}");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var ownerData = JsonSerializer.Deserialize<OwnerAuthResponse>(content,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (ownerData != null)
                {
                    return (ownerData.Id, ownerData.Balance, ownerData.PlanId, ownerData.MaxUnits);
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[IAM->Profiles] Error fetching owner profile for user {UserId}", userId);
            return null;
        }
    }

    public async Task<(int providerId, decimal balance, int planId, int maxClients, string companyName)?> GetProviderProfileForAuthByUserId(int userId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/v1/profiles/providers/auth/{userId}");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var providerData = JsonSerializer.Deserialize<ProviderAuthResponse>(content,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (providerData != null)
                {
                    return (providerData.Id, providerData.Balance, providerData.PlanId,
                            providerData.MaxClients, providerData.CompanyName);
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[IAM->Profiles] Error fetching provider profile for user {UserId}", userId);
            return null;
        }
    }

    public async Task<bool> CheckOwnerEmailExists(string email)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/v1/profiles/owners/check-email/{email}");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<EmailExistsResponse>(content,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return result?.Exists ?? false;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[IAM->Profiles] Error checking owner email {Email}", email);
            return false;
        }
    }

    public async Task<bool> CheckProviderEmailExists(string email)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/v1/profiles/providers/check-email/{email}");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<EmailExistsResponse>(content,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return result?.Exists ?? false;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[IAM->Profiles] Error checking provider email {Email}", email);
            return false;
        }
    }

    public async Task<int> CreateOwnerProfile(int userId, string firstName, string lastName, string email,
        string street, string number, string city, string postalCode, string country,
        int planId, int maxUnits)
    {
        try
        {
            var request = new
            {
                userId,
                firstName,
                lastName,
                email,
                street,
                number,
                city,
                postalCode,
                country,
                planId,
                maxUnits
            };

            var content = new StringContent(
                JsonSerializer.Serialize(request),
                System.Text.Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync("/api/v1/profiles/owners", content);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<CreateProfileResponse>(responseContent,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return result?.Id ?? 0;
            }

            _logger.LogWarning("[IAM->Profiles] Failed to create owner profile for user {UserId}: {StatusCode}",
                userId, response.StatusCode);
            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[IAM->Profiles] Error creating owner profile for user {UserId}", userId);
            return 0;
        }
    }

    public async Task<int> CreateProviderProfile(int userId, string companyName, string firstName, string lastName,
        string email, string street, string number, string city, string postalCode, string country,
        int planId, int maxClients, string taxId)
    {
        try
        {
            var request = new
            {
                userId,
                companyName,
                firstName,
                lastName,
                email,
                street,
                number,
                city,
                postalCode,
                country,
                planId,
                maxClients,
                taxId
            };

            var content = new StringContent(
                JsonSerializer.Serialize(request),
                System.Text.Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync("/api/v1/profiles/providers", content);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<CreateProfileResponse>(responseContent,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return result?.Id ?? 0;
            }

            _logger.LogWarning("[IAM->Profiles] Failed to create provider profile for user {UserId}: {StatusCode}",
                userId, response.StatusCode);
            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[IAM->Profiles] Error creating provider profile for user {UserId}", userId);
            return 0;
        }
    }

    public async Task<(int ownerId, int planId, decimal balance, int maxUnits)?> GetOwnerDataByUserId(int userId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/v1/profiles/owners/by-user/{userId}");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<OwnerDataResponse>(content,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (result != null)
                    return (result.Id, result.PlanId, result.Balance, result.MaxUnits);
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[IAM->Profiles] Error getting owner data for user {UserId}", userId);
            return null;
        }
    }

    public async Task<(int providerId, int planId, decimal balance, int maxClients, string companyName)?> GetProviderDataByUserId(int userId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/v1/profiles/providers/by-user/{userId}");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ProviderDataResponse>(content,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (result != null)
                    return (result.Id, result.PlanId, result.Balance, result.MaxClients, result.CompanyName);
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[IAM->Profiles] Error getting provider data for user {UserId}", userId);
            return null;
        }
    }

    // DTOs for deserialization (matching Profiles service responses)
    private record OwnerAuthResponse(int Id, decimal Balance, int PlanId, int MaxUnits);
    private record ProviderAuthResponse(int Id, decimal Balance, int PlanId, int MaxClients, string CompanyName);
    private record EmailExistsResponse(bool Exists);
    private record CreateProfileResponse(int Id);
    private record OwnerDataResponse(int Id, int PlanId, decimal Balance, int MaxUnits);
    private record ProviderDataResponse(int Id, int PlanId, decimal Balance, int MaxClients, string CompanyName);
}
