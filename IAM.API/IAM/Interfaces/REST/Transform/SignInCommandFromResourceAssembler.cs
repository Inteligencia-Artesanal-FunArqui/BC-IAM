using OsitoPolar.IAM.Service.Domain.Model.Commands;
using OsitoPolar.IAM.Service.Interfaces.REST.Resources;

namespace OsitoPolar.IAM.Service.Interfaces.REST.Transform;

public static class SignInCommandFromResourceAssembler
{
    public static SignInCommand ToCommandFromResource(SignInResource resource)
    {
        return new SignInCommand(resource.Username, resource.Password);
    }
}