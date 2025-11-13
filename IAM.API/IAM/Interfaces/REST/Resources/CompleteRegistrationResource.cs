namespace OsitoPolar.IAM.Service.Interfaces.REST.Resources;

/// <summary>
/// Resource for completing registration after payment
/// </summary>
public record CompleteRegistrationResource(
    string SessionId,
    string Username,
    string FirstName,
    string LastName,
    string Email,
    string Street,
    string Number,
    string City,
    string PostalCode,
    string Country,
    string? CompanyName = null,
    string? TaxId = null
);
