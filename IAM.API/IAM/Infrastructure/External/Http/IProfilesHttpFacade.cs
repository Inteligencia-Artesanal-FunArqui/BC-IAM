namespace OsitoPolar.IAM.Service.Infrastructure.External.Http;

/// <summary>
/// HTTP Facade for communicating with the Profiles microservice.
/// Used during authentication and registration to fetch/create profile data.
/// </summary>
public interface IProfilesHttpFacade
{
    /// <summary>
    /// Get Owner profile data for authentication response.
    /// Returns owner ID, balance, plan ID, and max units.
    /// </summary>
    Task<(int ownerId, decimal balance, int planId, int maxUnits)?> GetOwnerProfileForAuthByUserId(int userId);

    /// <summary>
    /// Get Provider profile data for authentication response.
    /// Returns provider ID, balance, plan ID, max clients, and company name.
    /// </summary>
    Task<(int providerId, decimal balance, int planId, int maxClients, string companyName)?> GetProviderProfileForAuthByUserId(int userId);

    /// <summary>
    /// Check if an owner email already exists in the system.
    /// Used during registration validation.
    /// </summary>
    Task<bool> CheckOwnerEmailExists(string email);

    /// <summary>
    /// Check if a provider email already exists in the system.
    /// Used during registration validation.
    /// </summary>
    Task<bool> CheckProviderEmailExists(string email);

    /// <summary>
    /// Create a new Owner profile during registration.
    /// Returns the created owner ID.
    /// </summary>
    Task<int> CreateOwnerProfile(int userId, string firstName, string lastName, string email,
        string street, string number, string city, string postalCode, string country,
        int planId, int maxUnits);

    /// <summary>
    /// Create a new Provider profile during registration.
    /// Returns the created provider ID.
    /// </summary>
    Task<int> CreateProviderProfile(int userId, string companyName, string firstName, string lastName,
        string email, string street, string number, string city, string postalCode, string country,
        int planId, int maxClients, string taxId);

    /// <summary>
    /// Get Owner profile data by user ID for UsersController.
    /// Returns owner ID, plan ID, balance, and max units.
    /// </summary>
    Task<(int ownerId, int planId, decimal balance, int maxUnits)?> GetOwnerDataByUserId(int userId);

    /// <summary>
    /// Get Provider profile data by user ID for UsersController.
    /// Returns provider ID, plan ID, balance, max clients, and company name.
    /// </summary>
    Task<(int providerId, int planId, decimal balance, int maxClients, string companyName)?> GetProviderDataByUserId(int userId);
}
