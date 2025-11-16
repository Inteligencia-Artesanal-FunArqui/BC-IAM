using OsitoPolar.IAM.Service.Domain.Model.Commands;
using OsitoPolar.IAM.Service.Domain.Model.Queries;
using OsitoPolar.IAM.Service.Domain.Services;

namespace OsitoPolar.IAM.Service.Interfaces.ACL.Services;

public class IamContextFacade(IUserCommandService userCommandService, IUserQueryService userQueryService) : IIamContextFacade
{
    public async Task<int> CreateUser(string username, string password)
    {
        // Create SignUpCommand with default values for new required fields
        // This maintains backward compatibility for other services calling this facade
        var signUpCommand = new SignUpCommand(
            Username: username,
            Password: password,
            FirstName: "Default",
            LastName: "User",
            Email: username, // Assuming username is email
            Street: "N/A",
            Number: "N/A",
            City: "N/A",
            PostalCode: "00000",
            Country: "N/A",
            PlanId: 1,
            MaxUnits: 10
        );
        await userCommandService.Handle(signUpCommand);
        var getUserByUsernameQuery = new GetUserByUsernameQuery(username);
        var result = await userQueryService.Handle(getUserByUsernameQuery);
        return result?.Id ?? 0;
    }

    public async Task<int> FetchUserIdByUsername(string username)
    {
        var getUserByUsernameQuery = new GetUserByUsernameQuery(username);
        var result = await userQueryService.Handle(getUserByUsernameQuery);
        return result?.Id ?? 0;
    }

    public async Task<string> FetchUsernameByUserId(int userId)
    {
        var getUserByIdQuery = new GetUserByIdQuery(userId);
        var result = await userQueryService.Handle(getUserByIdQuery);
        return result?.Username ?? string.Empty;
    }
}