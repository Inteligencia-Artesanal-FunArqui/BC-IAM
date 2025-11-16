namespace OsitoPolar.IAM.Service.Domain.Model.Commands;

/**
 * <summary>
 *     The sign up command
 * </summary>
 * <remarks>
 *     This command object includes user registration data and address information
 * </remarks>
 */
public record SignUpCommand(
    string Username,
    string Password,
    string FirstName,
    string LastName,
    string Email,
    string Street,
    string Number,
    string City,
    string PostalCode,
    string Country,
    int PlanId = 1,
    int MaxUnits = 10
);