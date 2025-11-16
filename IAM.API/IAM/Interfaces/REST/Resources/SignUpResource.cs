namespace OsitoPolar.IAM.Service.Interfaces.REST.Resources;

public record SignUpResource(
    string Username,
    string Password,
    string FirstName,
    string LastName,
    string Email,
    string PhoneNumber,
    string DocumentType,
    string DocumentNumber,
    string Street,
    string Number,
    string City,
    string PostalCode,
    string Country,
    int PlanId = 1,
    int MaxUnits = 10
);