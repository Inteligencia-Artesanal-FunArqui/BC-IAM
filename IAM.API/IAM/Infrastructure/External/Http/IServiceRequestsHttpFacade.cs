namespace OsitoPolar.IAM.Service.Infrastructure.External.Http;

/// <summary>
/// HTTP Facade for communicating with the ServiceRequests microservice.
/// Used by UsersController to get service request statistics for user profile.
/// </summary>
public interface IServiceRequestsHttpFacade
{
    /// <summary>
    /// Count active service requests for a specific equipment.
    /// Endpoint: GET /api/v1/service-requests/count/equipment/{equipmentId}/active
    /// </summary>
    Task<int> CountActiveServiceRequestsByEquipmentId(int equipmentId);

    /// <summary>
    /// Count all active service requests in the system.
    /// Endpoint: GET /api/v1/service-requests/count/active
    /// </summary>
    Task<int> CountAllActiveServiceRequests();
}
