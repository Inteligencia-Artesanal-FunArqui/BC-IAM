using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using OsitoPolar.IAM.Service.Domain.Model.Queries;
using OsitoPolar.IAM.Service.Domain.Services;
using OsitoPolar.IAM.Service.Infrastructure.Pipeline.Middleware.Attributes;
using OsitoPolar.IAM.Service.Interfaces.REST.Resources;
using OsitoPolar.IAM.Service.Interfaces.REST.Transform;
using OsitoPolar.IAM.Service.Infrastructure.External.Http;
using Swashbuckle.AspNetCore.Annotations;

namespace OsitoPolar.IAM.Service.Interfaces.REST;

/**
 * <summary>
 *     The user's controller
 * </summary>
 * <remarks>
 *     This class is used to handle user requests
 * </remarks>
 */
[Authorize]
[ApiController]
[Route("api/v1/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
[SwaggerTag("Available User endpoints")]
public class UsersController(
    IUserQueryService userQueryService,
    IProfilesHttpFacade profilesFacade,
    ISubscriptionsHttpFacade subscriptionFacade,
    IEquipmentHttpFacade equipmentFacade,
    IServiceRequestsHttpFacade serviceRequestFacade) : ControllerBase
{
    /**
     * <summary>
     *     Get user by id endpoint. It allows to get a user by id with full profile information
     * </summary>
     * <param name="id">The user id</param>
     * <returns>The user resource with profile and subscription data</returns>
     */
    [HttpGet("{id}")]
    [SwaggerOperation(
        Summary = "Get a user by its id",
        Description = "Get a user by its id with complete profile and subscription information",
        OperationId = "GetUserById")]
    [SwaggerResponse(StatusCodes.Status200OK, "The user was found", typeof(UserResource))]
    [SwaggerResponse(StatusCodes.Status404NotFound, "The user was not found")]
    public async Task<IActionResult> GetUserById(int id)
    {
        // Get user from IAM
        var getUserByIdQuery = new GetUserByIdQuery(id);
        var user = await userQueryService.Handle(getUserByIdQuery);

        if (user is null)
            return NotFound(new { message = $"User with id {id} not found" });

        // Try to get Owner profile data using Facade
        var ownerData = await profilesFacade.GetOwnerDataByUserId(id);
        var ownerSubscription = ownerData.HasValue
            ? await subscriptionFacade.GetSubscriptionDataById(ownerData.Value.planId)
            : null;

        // Try to get Provider profile data using Facade
        var providerData = await profilesFacade.GetProviderDataByUserId(id);
        var providerSubscription = providerData.HasValue
            ? await subscriptionFacade.GetSubscriptionDataById(providerData.Value.planId)
            : null;

        // Get counts for statistics
        int equipmentCount = 0;
        int activeServiceRequestsCount = 0;
        int clientCount = 0;

        if (ownerData.HasValue)
        {
            // Use Facade to get equipment count
            equipmentCount = await equipmentFacade.CountEquipmentByOwnerId(ownerData.Value.ownerId);

            // Count service requests for this owner's equipment
            var equipmentIds = await equipmentFacade.FetchEquipmentIdsByOwnerId(ownerData.Value.ownerId);
            foreach (var equipmentId in equipmentIds)
            {
                activeServiceRequestsCount += await serviceRequestFacade.CountActiveServiceRequestsByEquipmentId(equipmentId);
            }
        }

        if (providerData.HasValue)
        {
            // Count active service requests assigned to this provider's technicians
            // For now, we'll count all service requests (we can refine this later with provider-specific logic)
            activeServiceRequestsCount = await serviceRequestFacade.CountAllActiveServiceRequests();

            // TODO: Implement client count logic when we have the relationship set up
            // For now, we'll use a placeholder value
            clientCount = 0;
        }

        // Build profile data DTOs from tuples
        OwnerProfileData? ownerProfileData = null;
        ProviderProfileData? providerProfileData = null;

        if (ownerData.HasValue && ownerSubscription.HasValue)
        {
            ownerProfileData = new OwnerProfileData(
                ProfileId: ownerData.Value.ownerId,
                Balance: ownerData.Value.balance,
                Plan: new SubscriptionPlanData(
                    Id: ownerSubscription.Value.planId,
                    PlanName: ownerSubscription.Value.planName,
                    PlanType: "Owner",
                    Price: ownerSubscription.Value.price,
                    BillingCycle: "Monthly",
                    MaxEquipment: ownerSubscription.Value.maxEquipment,
                    MaxClients: null,
                    Features: new List<string>()
                ),
                MaxEquipment: ownerData.Value.maxUnits,
                CurrentEquipmentCount: equipmentCount,
                ActiveServiceRequests: activeServiceRequestsCount
            );
        }

        if (providerData.HasValue && providerSubscription.HasValue)
        {
            providerProfileData = new ProviderProfileData(
                ProfileId: providerData.Value.providerId,
                CompanyName: providerData.Value.companyName,
                TaxId: null,
                Balance: providerData.Value.balance,
                Plan: new SubscriptionPlanData(
                    Id: providerSubscription.Value.planId,
                    PlanName: providerSubscription.Value.planName,
                    PlanType: "Provider",
                    Price: providerSubscription.Value.price,
                    BillingCycle: "Monthly",
                    MaxEquipment: null,
                    MaxClients: providerSubscription.Value.maxClients,
                    Features: new List<string>()
                ),
                MaxClients: providerData.Value.maxClients,
                CurrentClientCount: clientCount,
                ActiveServiceRequests: activeServiceRequestsCount
            );
        }

        var userResource = UserResourceFromEntityAssembler.ToResourceFromEntity(
            user,
            ownerProfileData,
            providerProfileData
        );

        return Ok(userResource);
    }

    /**
     * <summary>
     *     Get all users' endpoint. It allows getting all users (basic info only)
     * </summary>
     * <returns>The user resources with basic profile information</returns>
     */
    [HttpGet]
    [SwaggerOperation(
        Summary = "Get all users",
        Description = "Get all users with basic information. Use GET /api/v1/users/{id} for complete profile details.",
        OperationId = "GetAllUsers")]
    [SwaggerResponse(StatusCodes.Status200OK, "The users were found", typeof(IEnumerable<UserResource>))]
    public async Task<IActionResult> GetAllUsers()
    {
        var getAllUsersQuery = new GetAllUsersQuery();
        var users = await userQueryService.Handle(getAllUsersQuery);

        // For list view, return basic info without detailed profile data
        var userResources = users.Select(user => UserResourceFromEntityAssembler.ToResourceFromEntity(user));
        return Ok(userResources);
    }
}
