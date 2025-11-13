using OsitoPolar.IAM.Service.Domain.Model.Commands;
using OsitoPolar.IAM.Service.Interfaces.REST.Resources;

namespace OsitoPolar.IAM.Service.Interfaces.REST.Transform;

public static class SignUpCommandFromResourceAssembler
{
    public static SignUpCommand ToCommandFromResource(SignUpResource resource)
    {
        return new SignUpCommand(resource.Username, resource.Password);
    }
}