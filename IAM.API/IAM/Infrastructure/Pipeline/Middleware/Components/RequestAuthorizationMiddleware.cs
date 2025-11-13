using OsitoPolar.IAM.Service.Application.Internal.OutboundServices;
using OsitoPolar.IAM.Service.Domain.Model.Queries;
using OsitoPolar.IAM.Service.Domain.Services;
using OsitoPolar.IAM.Service.Infrastructure.Pipeline.Middleware.Attributes;

namespace OsitoPolar.IAM.Service.Infrastructure.Pipeline.Middleware.Components;

public class RequestAuthorizationMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(
        HttpContext context,
        IUserQueryService userQueryService,
        ITokenService tokenService)
    {
        Console.WriteLine($"[Middleware] Processing: {context.Request.Method} {context.Request.Path}");
        
        var path = context.Request.Path.Value?.ToLower() ?? "";
        if (path.Contains("/authentication/") || path.Contains("/swagger"))
        {
            Console.WriteLine("[Middleware] Skipping authorization for auth/swagger endpoints");
            await next(context);
            return;
        }
        
        var endpoint = context.Request.HttpContext.GetEndpoint();
        if (endpoint != null)
        {
            var allowAnonymous = endpoint.Metadata
                .Any(m => m.GetType() == typeof(AllowAnonymousAttribute));
            
            if (allowAnonymous)
            {
                Console.WriteLine("[Middleware] Skipping authorization - AllowAnonymous");
                await next(context);
                return;
            }
        }

        Console.WriteLine("[Middleware] Checking authorization...");
        
        try
        {
            var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            {
                Console.WriteLine("[Middleware] No valid authorization header found");
                await next(context);
                return;
            }

            var token = authHeader.Substring("Bearer ".Length).Trim();
            Console.WriteLine($"[Middleware] Token found: {token.Substring(0, Math.Min(20, token.Length))}...");

            
            var userId = await tokenService.ValidateToken(token);
            if (userId == null)
            {
                Console.WriteLine("[Middleware] Token validation failed");
                await next(context);
                return;
            }

            Console.WriteLine($"[Middleware] Token valid for userId: {userId}");

            
            var getUserByIdQuery = new GetUserByIdQuery(userId.Value);
            var user = await userQueryService.Handle(getUserByIdQuery);
            
            if (user != null)
            {
                Console.WriteLine($"[Middleware] User found: {user.Username}");
                context.Items["User"] = user;
            }
            else
            {
                Console.WriteLine("[Middleware] User not found in database");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Middleware] Error in authorization: {ex.Message}");

        }

        Console.WriteLine("[Middleware] Continuing to next middleware...");
        await next(context);
    }
}