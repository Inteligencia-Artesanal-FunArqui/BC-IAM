using OsitoPolar.IAM.Service.Domain.Model.Aggregates;
using OsitoPolar.IAM.Service.Interfaces.REST.Resources;

namespace OsitoPolar.IAM.Service.Interfaces.REST.Transform;

public static class AuthenticatedUserResourceFromEntityAssembler
{
    public static AuthenticatedUserResource ToResourceFromEntity(
        User user, string token)
    {
        return new AuthenticatedUserResource(user.Id, user.Username, token);
    }
}