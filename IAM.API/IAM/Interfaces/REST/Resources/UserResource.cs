namespace OsitoPolar.IAM.Service.Interfaces.REST.Resources;

/// <summary>
/// Enhanced user resource with profile and subscription information
/// </summary>
public record UserResource(
    int Id,
    string Username,
    string? UserType, // "Owner", "Provider", or null (incomplete profile)
    DateTime MemberSince,
    bool TwoFactorEnabled,
    OwnerProfileData? OwnerProfile,
    ProviderProfileData? ProviderProfile
);

/// <summary>
/// Owner profile data
/// </summary>
public record OwnerProfileData(
    int ProfileId,
    decimal Balance,
    SubscriptionPlanData Plan,
    int MaxEquipment,
    int CurrentEquipmentCount,
    int ActiveServiceRequests
);

/// <summary>
/// Provider profile data
/// </summary>
public record ProviderProfileData(
    int ProfileId,
    string CompanyName,
    string? TaxId,
    decimal Balance,
    SubscriptionPlanData Plan,
    int MaxClients,
    int CurrentClientCount,
    int ActiveServiceRequests
);

/// <summary>
/// Subscription plan data
/// </summary>
public record SubscriptionPlanData(
    int Id,
    string PlanName,
    string PlanType, // "Owner" or "Provider"
    decimal Price,
    string BillingCycle,
    int? MaxEquipment, // For Owner plans (1-3)
    int? MaxClients,   // For Provider plans (4-6)
    List<string> Features
);