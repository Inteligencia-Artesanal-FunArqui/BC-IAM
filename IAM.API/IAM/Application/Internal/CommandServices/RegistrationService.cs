using OsitoPolar.IAM.Service.Domain.Services;
using OsitoPolar.IAM.Service.Interfaces.REST.Resources;

namespace OsitoPolar.IAM.Service.Application.Internal.CommandServices;

/// <summary>
/// Registration service - Stub implementation
/// TODO: Implement full registration with Stripe payment
/// </summary>
public class RegistrationService : IRegistrationService
{
    public Task<RegisterWithPaymentResponse> RegisterWithPaymentAsync(RegisterWithPaymentResource request)
    {
        throw new NotImplementedException("RegisterWithPayment endpoint not yet implemented for microservices");
    }
}
