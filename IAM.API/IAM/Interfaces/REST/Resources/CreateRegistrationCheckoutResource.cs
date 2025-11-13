namespace OsitoPolar.IAM.Service.Interfaces.REST.Resources;

/// <summary>
/// Resource for creating registration checkout session
/// </summary>
public record CreateRegistrationCheckoutResource(
    int PlanId,
    string UserType, // "Owner" or "Provider"
    string SuccessUrl,
    string CancelUrl
);
