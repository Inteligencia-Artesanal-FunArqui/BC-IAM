using OsitoPolar.IAM.Service.Interfaces.REST.Resources;

namespace OsitoPolar.IAM.Service.Domain.Services;

/// <summary>
/// Service for handling complete user registration with payment and profile creation
/// </summary>
public interface IRegistrationService
{
    /// <summary>
    /// Register a new user with payment, creating user account and Owner/Provider profile
    /// </summary>
    /// <param name="request">Registration request with payment and profile information</param>
    /// <returns>Registration response with user details and generated password</returns>
    Task<RegisterWithPaymentResponse> RegisterWithPaymentAsync(RegisterWithPaymentResource request);
}
