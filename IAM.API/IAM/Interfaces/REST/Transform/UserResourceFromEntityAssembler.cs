using OsitoPolar.IAM.Service.Domain.Model.Aggregates;
using OsitoPolar.IAM.Service.Interfaces.REST.Resources;

namespace OsitoPolar.IAM.Service.Interfaces.REST.Transform;

/// <summary>
/// Assembler to transform User entity to UserResource DTO.
/// Used by UsersController GET endpoint.
/// </summary>
public static class UserResourceFromEntityAssembler
{
    /// <summary>
    /// Transform User entity to UserResource with profile and subscription data.
    /// </summary>
    public static UserResource ToResourceFromEntity(
        User user,
        OwnerProfileData? ownerProfile = null,
        ProviderProfileData? providerProfile = null)
    {
        return new UserResource(
            Id: user.Id,
            Username: user.Username,
            UserType: ownerProfile != null ? "Owner" : (providerProfile != null ? "Provider" : null),
            MemberSince: DateTime.UtcNow, // TODO: Add CreatedAt field to User aggregate
            TwoFactorEnabled: user.TwoFactorEnabled,
            OwnerProfile: ownerProfile,
            ProviderProfile: providerProfile
        );
    }
}
