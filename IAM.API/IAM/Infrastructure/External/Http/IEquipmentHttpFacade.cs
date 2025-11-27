namespace OsitoPolar.IAM.Service.Infrastructure.External.Http;

/// <summary>
/// HTTP Facade for communicating with the Equipment microservice.
/// Used by UsersController to get equipment statistics for user profile.
/// </summary>
public interface IEquipmentHttpFacade
{
    /// <summary>
    /// Count total equipment owned by an owner.
    /// Endpoint: GET /api/v1/equipment/count/owner/{ownerId}
    /// </summary>
    Task<int> CountEquipmentByOwnerId(int ownerId);

    /// <summary>
    /// Fetch all equipment IDs owned by an owner.
    /// Endpoint: GET /api/v1/equipment/ids/owner/{ownerId}
    /// </summary>
    Task<IEnumerable<int>> FetchEquipmentIdsByOwnerId(int ownerId);
}
