using System.Net.Http.Json;
using OsitoPolar.IAM.Service.Shared.Interfaces.ACL;

namespace OsitoPolar.IAM.Service.Application.ACL.Services;

/// <summary>
/// HTTP Facade for communication with Profiles Service
/// Replaces ProfilesContextFacade that accessed the database directly
/// Implements IProfilesContextFacade interface - ALL 20 methods from monolith
/// </summary>
public class ProfilesHttpFacade : IProfilesContextFacade
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ProfilesHttpFacade> _logger;

    public ProfilesHttpFacade(HttpClient httpClient, ILogger<ProfilesHttpFacade> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    #region Core Profile Management (Not used in registration flow)

    public async Task<int> CreateProfile(string firstName, string lastName, string email,
        string street, string number, string city, string postalCode, string country)
    {
        _logger.LogWarning("CreateProfile called - this method is deprecated, use CreateOwnerProfile or CreateProviderProfile");
        // This method is not used in the new registration flow
        throw new NotImplementedException("Use CreateOwnerProfile or CreateProviderProfile instead");
    }

    public async Task<int> FetchProfileIdByEmail(string email)
    {
        // TODO: Create endpoint in Profiles Service: GET /api/v1/profiles/by-email/{email}
        _logger.LogWarning("FetchProfileIdByEmail not implemented - requires Profiles Service endpoint");
        return 0;
    }

    #endregion

    #region User Type Checks

    public async Task<bool> IsUserAnOwner(int userId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/v1/profiles/owners/auth/{userId}");
            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to check if user {UserId} is an owner", userId);
            return false;
        }
    }

    public async Task<bool> IsUserAProvider(int userId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/v1/profiles/providers/auth/{userId}");
            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to check if user {UserId} is a provider", userId);
            return false;
        }
    }

    #endregion

    #region ID Fetching

    public async Task<int> FetchOwnerIdByUserId(int userId)
    {
        try
        {
            var ownerData = await GetOwnerProfileForAuthByUserId(userId);
            return ownerData?.ownerId ?? 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch owner ID for user {UserId}", userId);
            return 0;
        }
    }

    public async Task<int> FetchProviderIdByUserId(int userId)
    {
        try
        {
            var providerData = await GetProviderProfileForAuthByUserId(userId);
            return providerData?.providerId ?? 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch provider ID for user {UserId}", userId);
            return 0;
        }
    }

    public async Task<string> FetchProviderCompanyName(int providerId)
    {
        // TODO: Create endpoint in Profiles Service: GET /api/v1/profiles/providers/{providerId}/company-name
        _logger.LogWarning("FetchProviderCompanyName not implemented - requires Profiles Service endpoint");
        return string.Empty;
    }

    #endregion

    #region Detailed User Data

    public async Task<(int ownerId, int planId, string firstName, string lastName, string email)?> GetOwnerDataByUserId(int userId)
    {
        // TODO: Create endpoint in Profiles Service: GET /api/v1/profiles/owners/data/{userId}
        _logger.LogWarning("GetOwnerDataByUserId not implemented - requires Profiles Service endpoint");
        return null;
    }

    public async Task<(int providerId, int planId, string companyName, string ruc, string email)?> GetProviderDataByUserId(int userId)
    {
        // TODO: Create endpoint in Profiles Service: GET /api/v1/profiles/providers/data/{userId}
        _logger.LogWarning("GetProviderDataByUserId not implemented - requires Profiles Service endpoint");
        return null;
    }

    #endregion

    #region Authentication Profile Data (CRITICAL FOR SIGN-IN)

    /// <summary>
    /// Get owner profile data for authentication - USED IN SIGN-IN FLOW
    /// </summary>
    public async Task<(int ownerId, decimal balance, int planId, int maxUnits)?> GetOwnerProfileForAuthByUserId(int userId)
    {
        try
        {
            _logger.LogInformation("Fetching Owner auth profile for user {UserId}", userId);

            var response = await _httpClient.GetAsync($"/api/v1/profiles/owners/auth/{userId}");

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<OwnerAuthResponse>();

            if (result == null) return null;

            return (result.OwnerId, result.Balance, result.PlanId, result.MaxUnits);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to fetch Owner auth profile for user {UserId}", userId);
            return null;
        }
    }

    /// <summary>
    /// Get provider profile data for authentication - USED IN SIGN-IN FLOW
    /// </summary>
    public async Task<(int providerId, decimal balance, int planId, int maxClients, string companyName)?> GetProviderProfileForAuthByUserId(int userId)
    {
        try
        {
            _logger.LogInformation("Fetching Provider auth profile for user {UserId}", userId);

            var response = await _httpClient.GetAsync($"/api/v1/profiles/providers/auth/{userId}");

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<ProviderAuthResponse>();

            if (result == null) return null;

            return (result.ProviderId, result.Balance, result.PlanId, result.MaxClients, result.CompanyName);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to fetch Provider auth profile for user {UserId}", userId);
            return null;
        }
    }

    #endregion

    #region Profile Creation (CRITICAL FOR REGISTRATION)

    /// <summary>
    /// Create owner profile during registration - USED IN COMPLETE-REGISTRATION FLOW
    /// </summary>
    public async Task<int> CreateOwnerProfile(int userId, string firstName, string lastName, string email,
        string street, string number, string city, string postalCode, string country, int planId, int maxUnits)
    {
        try
        {
            _logger.LogInformation("Creating Owner profile for user {UserId}", userId);

            var request = new CreateOwnerRequest(
                UserId: userId,
                FirstName: firstName,
                LastName: lastName,
                Email: email,
                Street: street,
                Number: number,
                City: city,
                PostalCode: postalCode,
                Country: country,
                PlanId: planId,
                MaxUnits: maxUnits
            );

            var response = await _httpClient.PostAsJsonAsync("/api/v1/profiles/owners", request);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<OwnerResourceResponse>();

            if (result == null)
            {
                _logger.LogError("Failed to deserialize Owner creation response");
                return 0;
            }

            _logger.LogInformation("Owner profile created successfully with ID: {OwnerId}", result.Id);
            return result.Id;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to create Owner profile in Profiles Service");
            return 0;
        }
    }

    /// <summary>
    /// Create provider profile during registration - USED IN COMPLETE-REGISTRATION FLOW
    /// </summary>
    public async Task<int> CreateProviderProfile(int userId, string companyName, string contactFirstName, string contactLastName,
        string email, string street, string number, string city, string postalCode, string country,
        int planId, int maxClients, string taxId)
    {
        try
        {
            _logger.LogInformation("Creating Provider profile for user {UserId}", userId);

            var request = new CreateProviderRequest(
                UserId: userId,
                CompanyName: companyName,
                ContactFirstName: contactFirstName,
                ContactLastName: contactLastName,
                Email: email,
                Street: street,
                Number: number,
                City: city,
                PostalCode: postalCode,
                Country: country,
                PlanId: planId,
                MaxClients: maxClients,
                TaxId: taxId
            );

            var response = await _httpClient.PostAsJsonAsync("/api/v1/profiles/providers", request);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<ProviderResourceResponse>();

            if (result == null)
            {
                _logger.LogError("Failed to deserialize Provider creation response");
                return 0;
            }

            _logger.LogInformation("Provider profile created successfully with ID: {ProviderId}", result.Id);
            return result.Id;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to create Provider profile in Profiles Service");
            return 0;
        }
    }

    #endregion

    #region Provider Specific

    public async Task<int> GetProviderUserIdByProviderId(int providerId)
    {
        // TODO: Create endpoint in Profiles Service: GET /api/v1/profiles/providers/{providerId}/user-id
        _logger.LogWarning("GetProviderUserIdByProviderId not implemented - requires Profiles Service endpoint");
        return 0;
    }

    public async Task<bool> UpdateProviderBalance(int providerId, decimal amount, string description)
    {
        // TODO: Create endpoint in Profiles Service: PUT /api/v1/profiles/providers/{providerId}/balance
        _logger.LogWarning("UpdateProviderBalance not implemented - requires Profiles Service endpoint");
        return false;
    }

    #endregion

    #region Subscription Plan Updates

    public async Task<bool> UpdateOwnerPlan(int ownerId, int planId, int maxUnits)
    {
        // TODO: Create endpoint in Profiles Service: PUT /api/v1/profiles/owners/{ownerId}/plan
        _logger.LogWarning("UpdateOwnerPlan not implemented - requires Profiles Service endpoint");
        return false;
    }

    public async Task<bool> UpdateProviderPlan(int providerId, int planId, int maxClients)
    {
        // TODO: Create endpoint in Profiles Service: PUT /api/v1/profiles/providers/{providerId}/plan
        _logger.LogWarning("UpdateProviderPlan not implemented - requires Profiles Service endpoint");
        return false;
    }

    #endregion

    #region Owner Specific

    public async Task<(string firstName, string lastName)?> GetOwnerNameByOwnerId(int ownerId)
    {
        // TODO: Create endpoint in Profiles Service: GET /api/v1/profiles/owners/{ownerId}/name
        _logger.LogWarning("GetOwnerNameByOwnerId not implemented - requires Profiles Service endpoint");
        return null;
    }

    #endregion

    #region Email Existence Checks

    public async Task<bool> CheckOwnerEmailExists(string email)
    {
        // TODO: Create endpoint in Profiles Service: GET /api/v1/profiles/owners/check-email/{email}
        _logger.LogWarning("CheckOwnerEmailExists not implemented - requires Profiles Service endpoint");
        return false;
    }

    public async Task<bool> CheckProviderEmailExists(string email)
    {
        // TODO: Create endpoint in Profiles Service: GET /api/v1/profiles/providers/check-email/{email}
        _logger.LogWarning("CheckProviderEmailExists not implemented - requires Profiles Service endpoint");
        return false;
    }

    #endregion
}

#region DTOs for HTTP Communication with Profiles Service

// Request DTOs (sent TO Profiles Service)
internal record CreateOwnerRequest(
    int UserId,
    string FirstName,
    string LastName,
    string Email,
    string Street,
    string Number,
    string City,
    string PostalCode,
    string Country,
    int PlanId,
    int MaxUnits
);

internal record CreateProviderRequest(
    int UserId,
    string CompanyName,
    string ContactFirstName,
    string ContactLastName,
    string Email,
    string Street,
    string Number,
    string City,
    string PostalCode,
    string Country,
    int PlanId,
    int MaxClients,
    string? TaxId
);

// Response DTOs (received FROM Profiles Service)
internal record OwnerAuthResponse(
    int OwnerId,
    decimal Balance,
    int PlanId,
    int MaxUnits
);

internal record ProviderAuthResponse(
    int ProviderId,
    decimal Balance,
    int PlanId,
    int MaxClients,
    string CompanyName
);

internal record OwnerResourceResponse(
    int Id,
    int UserId,
    string FirstName,
    string LastName,
    string Email,
    string Street,
    string Number,
    string City,
    string PostalCode,
    string Country,
    int PlanId,
    int MaxUnits
);

internal record ProviderResourceResponse(
    int Id,
    int UserId,
    string CompanyName,
    string ContactFirstName,
    string ContactLastName,
    string Email,
    string Street,
    string Number,
    string City,
    string PostalCode,
    string Country,
    int PlanId,
    int MaxClients,
    string? TaxId
);

#endregion
