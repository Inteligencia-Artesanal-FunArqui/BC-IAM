namespace OsitoPolar.IAM.Service.Interfaces.REST.Resources;

/// <summary>
/// Registration request with payment and profile information
/// </summary>
public record RegisterWithPaymentResource
{
    // User Account Info
    public string Username { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;

    // User Type - determines which profile to create
    public string UserType { get; init; } = string.Empty; // "Owner" or "Provider"

    // Plan Selection
    public int PlanId { get; init; } // 1-3 for Owner, 4-6 for Provider

    // Payment Info
    public string PaymentToken { get; init; } = string.Empty; // Stripe payment method ID

    // Common Profile Fields
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string Street { get; init; } = string.Empty;
    public string Number { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public string PostalCode { get; init; } = string.Empty;
    public string Country { get; init; } = string.Empty;

    // Provider-Specific Fields (optional, only for Provider)
    public string? CompanyName { get; init; }
    public string? TaxId { get; init; }
}

/// <summary>
/// Registration response with user details and credentials
/// </summary>
public record RegisterWithPaymentResponse
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public int? UserId { get; init; }
    public string? Username { get; init; }
    public string? GeneratedPassword { get; init; } // Only in response, will be emailed
    public string? UserType { get; init; }
    public int? ProfileId { get; init; }
    public string? TransactionId { get; init; } // Stripe payment ID
    public string? ErrorMessage { get; init; }
}
