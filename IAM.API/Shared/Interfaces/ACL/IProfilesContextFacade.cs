namespace OsitoPolar.IAM.Service.Shared.Interfaces.ACL;

/// <summary>
/// Facade for the profiles context
/// </summary>
public interface IProfilesContextFacade
{
    /// <summary>
    /// Create a profile
    /// </summary>
    /// <param name="firstName">
    /// First name of the profile
    /// </param>
    /// <param name="lastName">
    /// Last name of the profile
    /// </param>
    /// <param name="email">
    /// Email of the profile
    /// </param>
    /// <param name="street">
    /// Street of the profile
    /// </param>
    /// <param name="number">
    /// Number of the profile
    /// </param>
    /// <param name="city">
    /// City of the profile
    /// </param>
    /// <param name="postalCode">
    /// Postal code of the profile
    /// </param>
    /// <param name="country">
    /// Country of the profile
    /// </param>
    /// <returns>
    /// The id of the created profile if successful, 0 otherwise
    /// </returns>
    Task<int> CreateProfile(string firstName,
        string lastName,
        string email,
        string street,
        string number,
        string city,
        string postalCode,
        string country);

    /// <summary>
    /// Fetch the profile id by email
    /// </summary>
    /// <param name="email">
    /// Email of the profile to fetch
    /// </param>
    /// <returns>
    /// The id of the profile if found, 0 otherwise
    /// </returns>
    Task<int> FetchProfileIdByEmail(string email);

    /// <summary>
    /// Check if a user is an owner
    /// </summary>
    /// <param name="userId">User ID from IAM</param>
    /// <returns>True if user is an owner, false otherwise</returns>
    Task<bool> IsUserAnOwner(int userId);

    /// <summary>
    /// Check if a user is a provider
    /// </summary>
    /// <param name="userId">User ID from IAM</param>
    /// <returns>True if user is a provider, false otherwise</returns>
    Task<bool> IsUserAProvider(int userId);

    /// <summary>
    /// Fetch owner ID by user ID
    /// </summary>
    /// <param name="userId">User ID from IAM</param>
    /// <returns>Owner ID if found, 0 otherwise</returns>
    Task<int> FetchOwnerIdByUserId(int userId);

    /// <summary>
    /// Fetch provider ID by user ID
    /// </summary>
    /// <param name="userId">User ID from IAM</param>
    /// <returns>Provider ID if found, 0 otherwise</returns>
    Task<int> FetchProviderIdByUserId(int userId);

    /// <summary>
    /// Fetch provider company name
    /// </summary>
    /// <param name="providerId">Provider ID</param>
    /// <returns>Company name if found, empty string otherwise</returns>
    Task<string> FetchProviderCompanyName(int providerId);

    /// <summary>
    /// Get owner data by user ID (returns tuple with owner info)
    /// </summary>
    /// <param name="userId">User ID from IAM</param>
    /// <returns>Tuple with (ownerId, planId, firstName, lastName, email) or null if not found</returns>
    Task<(int ownerId, int planId, string firstName, string lastName, string email)?> GetOwnerDataByUserId(int userId);

    /// <summary>
    /// Get provider data by user ID (returns tuple with provider info)
    /// </summary>
    /// <param name="userId">User ID from IAM</param>
    /// <returns>Tuple with (providerId, planId, companyName, ruc, email) or null if not found</returns>
    Task<(int providerId, int planId, string companyName, string ruc, string email)?> GetProviderDataByUserId(int userId);

    /// <summary>
    /// Get owner profile data for authentication by user ID
    /// </summary>
    /// <param name="userId">User ID from IAM</param>
    /// <returns>Tuple with (ownerId, balance, planId, maxUnits) or null if not found</returns>
    Task<(int ownerId, decimal balance, int planId, int maxUnits)?> GetOwnerProfileForAuthByUserId(int userId);

    /// <summary>
    /// Get provider profile data for authentication by user ID
    /// </summary>
    /// <param name="userId">User ID from IAM</param>
    /// <returns>Tuple with (providerId, balance, planId, maxClients, companyName) or null if not found</returns>
    Task<(int providerId, decimal balance, int planId, int maxClients, string companyName)?> GetProviderProfileForAuthByUserId(int userId);

    /// <summary>
    /// Create owner profile (used during registration)
    /// </summary>
    Task<int> CreateOwnerProfile(int userId, string firstName, string lastName, string email,
        string street, string number, string city, string postalCode, string country, int planId, int maxUnits);

    /// <summary>
    /// Create provider profile (used during registration)
    /// </summary>
    Task<int> CreateProviderProfile(int userId, string companyName, string contactFirstName, string contactLastName,
        string email, string street, string number, string city, string postalCode, string country,
        int planId, int maxClients, string taxId);

    /// <summary>
    /// Get provider user ID by provider ID (for notifications)
    /// </summary>
    /// <param name="providerId">Provider ID</param>
    /// <returns>User ID if found, 0 otherwise</returns>
    Task<int> GetProviderUserIdByProviderId(int providerId);

    /// <summary>
    /// Update provider balance (add revenue)
    /// </summary>
    /// <param name="providerId">Provider ID</param>
    /// <param name="amount">Amount to add to balance</param>
    /// <param name="description">Description of the transaction</param>
    /// <returns>True if successful, false otherwise</returns>
    Task<bool> UpdateProviderBalance(int providerId, decimal amount, string description);

    /// <summary>
    /// Update owner subscription plan
    /// </summary>
    /// <param name="ownerId">Owner ID</param>
    /// <param name="planId">New plan ID</param>
    /// <param name="maxUnits">New max units limit</param>
    /// <returns>True if successful, false otherwise</returns>
    Task<bool> UpdateOwnerPlan(int ownerId, int planId, int maxUnits);

    /// <summary>
    /// Update provider subscription plan
    /// </summary>
    /// <param name="providerId">Provider ID</param>
    /// <param name="planId">New plan ID</param>
    /// <param name="maxClients">New max clients limit</param>
    /// <returns>True if successful, false otherwise</returns>
    Task<bool> UpdateProviderPlan(int providerId, int planId, int maxClients);

    /// <summary>
    /// Get owner data by owner ID (not userId)
    /// </summary>
    /// <param name="ownerId">Owner ID</param>
    /// <returns>Tuple with (firstName, lastName) or null if not found</returns>
    Task<(string firstName, string lastName)?> GetOwnerNameByOwnerId(int ownerId);

    /// <summary>
    /// Check if an owner exists with the given email
    /// </summary>
    /// <param name="email">Email to check</param>
    /// <returns>True if owner exists with that email, false otherwise</returns>
    Task<bool> CheckOwnerEmailExists(string email);

    /// <summary>
    /// Check if a provider exists with the given email
    /// </summary>
    /// <param name="email">Email to check</param>
    /// <returns>True if provider exists with that email, false otherwise</returns>
    Task<bool> CheckProviderEmailExists(string email);
}
