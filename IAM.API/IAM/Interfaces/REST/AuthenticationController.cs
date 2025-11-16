using System.Net.Mime;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OsitoPolar.IAM.Service.Application.Internal.OutboundServices;
using OsitoPolar.IAM.Service.Domain.Model.Commands;
using OsitoPolar.IAM.Service.Domain.Repositories;
using OsitoPolar.IAM.Service.Domain.Services;
using OsitoPolar.IAM.Service.Interfaces.REST.Resources;
using OsitoPolar.IAM.Service.Interfaces.REST.Transform;
using OsitoPolar.IAM.Service.Shared.Domain.Repositories;
using OsitoPolar.IAM.Service.Shared.Interfaces.ACL;
using Stripe;
using Stripe.Checkout;
using Swashbuckle.AspNetCore.Annotations;
using Authorize = OsitoPolar.IAM.Service.Infrastructure.Pipeline.Middleware.Attributes.AuthorizeAttribute;

namespace OsitoPolar.IAM.Service.Interfaces.REST;

[AllowAnonymous]
[ApiController]
[Route("api/v1/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
[SwaggerTag("Available Authentication endpoints")]
public class AuthenticationController(
    IUserCommandService userCommandService,
    IRegistrationService registrationService,
    ITwoFactorService twoFactorService,
    IUserRepository userRepository,
    IProfilesContextFacade profilesFacade,
    ISubscriptionContextFacade subscriptionFacade,
    IUnitOfWork unitOfWork,
    INotificationContextFacade notificationFacade) : ControllerBase
{
    private static string GenerateSecurePassword()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";
        var password = new char[16];
        var randomBytes = new byte[16];

        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
        }

        for (int i = 0; i < 16; i++)
        {
            password[i] = chars[randomBytes[i] % chars.Length];
        }

        return new string(password);
    }

    private static string FormatSecretForDisplay(string secret)
    {
        var formatted = new List<string>();
        for (var i = 0; i < secret.Length; i += 4)
        {
            var remaining = secret.Length - i;
            var length = Math.Min(4, remaining);
            formatted.Add(secret.Substring(i, length));
        }
        return string.Join(" ", formatted);
    }
    /**
     * <summary>
     *     Sign in endpoint. It allows authenticating a user
     * </summary>
     * <param name="signInResource">The sign-in resource containing username and password.</param>
     * <returns>The authenticated user resource, JWT token, or 2FA setup/verification requirement</returns>
     */
    [HttpPost("sign-in")]
    [AllowAnonymous]
    [SwaggerOperation(
        Summary = "Sign in",
        Description = "Sign in a user. May require 2FA setup on first login or 2FA verification if enabled.",
        OperationId = "SignIn")]
    [SwaggerResponse(StatusCodes.Status200OK, "The user was authenticated or needs 2FA setup/verification")]
    public async Task<IActionResult> SignIn([FromBody] SignInResource signInResource)
    {
        try
        {
            var signInCommand = SignInCommandFromResourceAssembler.ToCommandFromResource(signInResource);
            var authenticatedUser = await userCommandService.Handle(signInCommand);

            // If token is empty, check if 2FA setup or verification is required
            if (string.IsNullOrEmpty(authenticatedUser.token))
            {
                var userFor2FA = authenticatedUser.user;

                // First login - needs 2FA setup (secret was just generated)
                if (!userFor2FA.TwoFactorEnabled && !string.IsNullOrEmpty(userFor2FA.TwoFactorSecret))
                {
                    // Use the existing secret from the database to generate QR code
                    var secret = userFor2FA.TwoFactorSecret;
                    var label = userFor2FA.Username;
                    var issuer = "OsitoPolar";
                    var otpUrl = $"otpauth://totp/{Uri.EscapeDataString(issuer)}:{Uri.EscapeDataString(label)}?secret={secret}&issuer={Uri.EscapeDataString(issuer)}";

                    // Generate QR code from the existing secret
                    using var qrGenerator = new QRCoder.QRCodeGenerator();
                    var qrCodeData = qrGenerator.CreateQrCode(otpUrl, QRCoder.QRCodeGenerator.ECCLevel.Q);
                    using var qrCode = new QRCoder.PngByteQRCode(qrCodeData);
                    var qrCodeBytes = qrCode.GetGraphic(20);
                    var qrCodeBase64 = Convert.ToBase64String(qrCodeBytes);
                    var qrCodeDataUrl = $"data:image/png;base64,{qrCodeBase64}";

                    // Format secret for manual entry
                    var manualEntryKey = FormatSecretForDisplay(secret);

                    return Ok(new
                    {
                        requiresTwoFactorSetup = true,
                        username = userFor2FA.Username,
                        qrCodeDataUrl = qrCodeDataUrl,
                        manualEntryKey = manualEntryKey,
                        message = "First login detected. Please scan the QR code with Google Authenticator and enter the 6-digit code."
                    });
                }

                // 2FA is enabled - needs verification code
                if (userFor2FA.TwoFactorEnabled)
                {
                    return Ok(new
                    {
                        requires2FA = true,
                        username = userFor2FA.Username,
                        message = "Please enter your 6-digit code from Google Authenticator."
                    });
                }
            }

            // Normal authentication - detect user type and return token + profile info
            var user = authenticatedUser.user;
            var token = authenticatedUser.token;

            // Check if user is an Owner using Facade
            var ownerProfile = await profilesFacade.GetOwnerProfileForAuthByUserId(user.Id);
            if (ownerProfile.HasValue)
            {
                return Ok(new
                {
                    id = user.Id,
                    username = user.Username,
                    token,
                    userType = "Owner",
                    profileId = ownerProfile.Value.ownerId,
                    balance = ownerProfile.Value.balance,
                    planId = ownerProfile.Value.planId,
                    maxUnits = ownerProfile.Value.maxUnits
                });
            }

            // Check if user is a Provider using Facade
            var providerProfile = await profilesFacade.GetProviderProfileForAuthByUserId(user.Id);
            if (providerProfile.HasValue)
            {
                return Ok(new
                {
                    id = user.Id,
                    username = user.Username,
                    token,
                    userType = "Provider",
                    profileId = providerProfile.Value.providerId,
                    balance = providerProfile.Value.balance,
                    planId = providerProfile.Value.planId,
                    maxClients = providerProfile.Value.maxClients,
                    companyName = providerProfile.Value.companyName
                });
            }

            // User has no profile yet - return basic authentication
            var resource =
                AuthenticatedUserResourceFromEntityAssembler.ToResourceFromEntity(user, token);
            return Ok(resource);
        }
        catch (Exception ex)
        {
            return Unauthorized(new
            {
                message = "An error occurred while signing in",
                error = ex.Message
            });
        }
    }

    /**
     * <summary>
     *     Create Stripe checkout session for registration (Step 1: Payment)
     * </summary>
     * <param name="request">Request containing planId and userType</param>
     * <returns>Stripe checkout URL</returns>
     */
    [HttpPost("create-registration-checkout")]
    [AllowAnonymous]
    [SwaggerOperation(
        Summary = "Create registration checkout session",
        Description = "Step 1: Creates Stripe checkout session for new user registration. User pays first, then completes registration form.",
        OperationId = "CreateRegistrationCheckout")]
    [SwaggerResponse(StatusCodes.Status200OK, "Checkout session created successfully")]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid request")]
    public async Task<IActionResult> CreateRegistrationCheckout([FromBody] CreateRegistrationCheckoutResource request)
    {
        try
        {
            // Get plan details using Facade
            var planData = await subscriptionFacade.GetSubscriptionDataById(request.PlanId);
            if (!planData.HasValue)
                return BadRequest(new { message = "Invalid plan ID" });

            // Validate userType matches plan type
            var isOwnerPlan = request.PlanId >= 1 && request.PlanId <= 3;
            var isProviderPlan = request.PlanId >= 4 && request.PlanId <= 6;

            if (request.UserType == "Owner" && !isOwnerPlan)
                return BadRequest(new { message = "Owner must select an Owner plan (1-3)" });

            if (request.UserType == "Provider" && !isProviderPlan)
                return BadRequest(new { message = "Provider must select a Provider plan (4-6)" });

            // Create Stripe checkout session
            var options = new Stripe.Checkout.SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = new List<Stripe.Checkout.SessionLineItemOptions>
                {
                    new Stripe.Checkout.SessionLineItemOptions
                    {
                        PriceData = new Stripe.Checkout.SessionLineItemPriceDataOptions
                        {
                            Currency = "usd",
                            ProductData = new Stripe.Checkout.SessionLineItemPriceDataProductDataOptions
                            {
                                Name = planData.Value.planName,
                                Description = $"OsitoPolar {request.UserType} Plan - {planData.Value.planName}"
                            },
                            UnitAmount = (long)(planData.Value.price * 100), // Convert to cents
                        },
                        Quantity = 1
                    }
                },
                Mode = "payment",
                SuccessUrl = request.SuccessUrl + "?session_id={CHECKOUT_SESSION_ID}",
                CancelUrl = request.CancelUrl,
                Metadata = new Dictionary<string, string>
                {
                    { "paymentType", "registration" },
                    { "planId", request.PlanId.ToString() },
                    { "userType", request.UserType }
                }
            };

            var service = new Stripe.Checkout.SessionService();
            var session = await service.CreateAsync(options);

            return Ok(new
            {
                sessionId = session.Id,
                checkoutUrl = session.Url,
                planName = planData.Value.planName,
                amount = planData.Value.price
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = "Failed to create checkout session", error = ex.Message });
        }
    }

    /**
     * <summary>
     *     Complete registration after successful payment (Step 2: Create Account)
     * </summary>
     * <param name="request">Registration data with Stripe session ID</param>
     * <returns>Registration response with credentials</returns>
     */
    [HttpPost("complete-registration")]
    [AllowAnonymous]
    [SwaggerOperation(
        Summary = "Complete registration after payment",
        Description = "Step 2: Completes user registration after successful Stripe payment. Validates payment and creates user account with profile.",
        OperationId = "CompleteRegistration")]
    [SwaggerResponse(StatusCodes.Status200OK, "Registration completed successfully")]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Registration failed")]
    public async Task<IActionResult> CompleteRegistration([FromBody] CompleteRegistrationResource request)
    {
        try
        {
            // Verify Stripe session
            var sessionService = new Stripe.Checkout.SessionService();
            var session = await sessionService.GetAsync(request.SessionId);

            if (session == null)
                return BadRequest(new { message = "Invalid session ID" });

            if (session.PaymentStatus != "paid")
                return BadRequest(new { message = "Payment not completed. Please complete payment first." });

            if (session.Metadata["paymentType"] != "registration")
                return BadRequest(new { message = "Invalid session type" });

            // Check if session already used
            var existingUser = await userRepository.FindByUsernameAsync(request.Username);
            if (existingUser != null)
                return BadRequest(new { message = "Username already exists" });

            // Get plan and userType from session metadata
            var planId = int.Parse(session.Metadata["planId"]);
            var userType = session.Metadata["userType"];

            // Get subscription plan to retrieve maxUnits/maxClients
            var planInfo = await subscriptionFacade.GetSubscriptionDataById(planId);
            if (!planInfo.HasValue)
                return BadRequest(new { message = "Invalid plan ID from session" });

            // Generate secure password
            var generatedPassword = GenerateSecurePassword();

            // Get subscription limits
            var limits = await subscriptionFacade.GetSubscriptionLimits(planId);
            var maxUnits = limits?.maxEquipment ?? 10;
            var maxClients = limits?.maxClients ?? 50;

            // Create user with all profile information
            var signUpCommand = new SignUpCommand(
                request.Username,
                generatedPassword,
                request.FirstName,
                request.LastName,
                request.Email,
                request.Street,
                request.Number,
                request.City,
                request.PostalCode,
                request.Country,
                planId,
                maxUnits
            );
            await userCommandService.Handle(signUpCommand);

            var user = await userRepository.FindByUsernameAsync(request.Username);
            if (user == null)
                return BadRequest(new { message = "Failed to create user" });

            // Create profile based on userType using Facade
            if (userType == "Owner")
            {
                await profilesFacade.CreateOwnerProfile(
                    user.Id, request.FirstName, request.LastName, request.Email,
                    request.Street, request.Number, request.City, request.PostalCode, request.Country,
                    planId, maxUnits);
            }
            else
            {
                await profilesFacade.CreateProviderProfile(
                    user.Id, request.CompanyName ?? "Company", request.FirstName, request.LastName, request.Email,
                    request.Street, request.Number, request.City, request.PostalCode, request.Country,
                    planId, maxClients, request.TaxId ?? "");
            }

            // Save all changes to database
            await unitOfWork.CompleteAsync();

            // Send welcome email with credentials using Facade
            try
            {
                var loginUrl = $"{Request.Scheme}://{Request.Host}/sign-in";
                var emailSubject = "Welcome to OsitoPolar - Your Credentials";
                var emailBody = $@"
                    <h2>Welcome to OsitoPolar!</h2>
                    <p>Hello {request.FirstName} {request.LastName},</p>
                    <p>Your account has been created successfully. Here are your credentials:</p>
                    <ul>
                        <li><strong>Email:</strong> {request.Email}</li>
                        <li><strong>Temporary Password:</strong> {generatedPassword}</li>
                    </ul>
                    <p>Please login at: <a href='{loginUrl}'>{loginUrl}</a></p>
                    <p>For security reasons, please change your password after your first login.</p>
                    <p>Best regards,<br/>OsitoPolar Team</p>
                ";

                await notificationFacade.SendEmailNotification(request.Email, $"{request.FirstName} {request.LastName}", emailSubject, emailBody);
            }
            catch (Exception emailEx)
            {
                // Log email error but don't fail the registration
                Console.WriteLine($"Warning: Failed to send welcome email: {emailEx.Message}");
            }

            return Ok(new
            {
                success = true,
                message = "Registration completed successfully",
                userId = user.Id,
                username = request.Username,
                generatedPassword,
                userType,
                email = request.Email
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = "Registration failed", error = ex.Message });
        }
    }

    /**
     * <summary>
     *     Register with payment endpoint - DEPRECATED (Use create-registration-checkout + complete-registration instead)
     * </summary>
     * <param name="request">Registration request with payment and profile information</param>
     * <returns>Registration response with generated credentials</returns>
     */
    [HttpPost("register")]
    [AllowAnonymous]
    [SwaggerOperation(
        Summary = "Register with payment (DEPRECATED)",
        Description = "DEPRECATED: Use create-registration-checkout + complete-registration flow instead. This synchronous payment method is no longer recommended.",
        OperationId = "RegisterWithPayment")]
    [SwaggerResponse(StatusCodes.Status200OK, "Registration completed successfully", typeof(RegisterWithPaymentResponse))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Registration failed")]
    public async Task<IActionResult> RegisterWithPayment([FromBody] RegisterWithPaymentResource request)
    {
        try
        {
            var response = await registrationService.RegisterWithPaymentAsync(request);

            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            return BadRequest(new RegisterWithPaymentResponse
            {
                Success = false,
                ErrorMessage = $"Registration failed: {ex.Message}"
            });
        }
    }

    /**
     * <summary>
     *     Verify two-factor authentication code
     * </summary>
     * <param name="resource">The verification resource containing username and code</param>
     * <returns>The authenticated user resource with JWT token</returns>
     */
    [HttpPost("verify-2fa")]
    [AllowAnonymous]
    [SwaggerOperation(
        Summary = "Verify 2FA code",
        Description = "Verify a 6-digit 2FA code from Google Authenticator. Used for first-time setup or login with 2FA enabled.",
        OperationId = "VerifyTwoFactor")]
    [SwaggerResponse(StatusCodes.Status200OK, "The 2FA code was verified and user is authenticated", typeof(AuthenticatedUserResource))]
    public async Task<IActionResult> VerifyTwoFactor([FromBody] VerifyTwoFactorResource resource)
    {
        try
        {
            var command = new VerifyTwoFactorCommand(resource.Username, resource.Code);
            var authenticatedUser = await userCommandService.Handle(command);

            var user = authenticatedUser.user;
            var token = authenticatedUser.token;

            // Check if user is an Owner
            var ownerData = await profilesFacade.GetOwnerProfileForAuthByUserId(user.Id);
            if (ownerData != null)
            {
                return Ok(new
                {
                    id = user.Id,
                    username = user.Username,
                    token,
                    userType = "Owner",
                    profileId = ownerData.Value.ownerId,
                    balance = ownerData.Value.balance,
                    planId = ownerData.Value.planId,
                    maxUnits = ownerData.Value.maxUnits
                });
            }

            // Check if user is a Provider
            var providerData = await profilesFacade.GetProviderProfileForAuthByUserId(user.Id);
            if (providerData != null)
            {
                return Ok(new
                {
                    id = user.Id,
                    username = user.Username,
                    token,
                    userType = "Provider",
                    profileId = providerData.Value.providerId,
                    balance = providerData.Value.balance,
                    planId = providerData.Value.planId,
                    maxClients = providerData.Value.maxClients,
                    companyName = providerData.Value.companyName
                });
            }

            // User has no profile yet - return basic authentication
            var response = AuthenticatedUserResourceFromEntityAssembler.ToResourceFromEntity(user, token);
            return Ok(response);
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                message = "Failed to verify 2FA code",
                error = ex.Message
            });
        }
    }

    /**
     * <summary>
     *     Initiate two-factor authentication setup
     * </summary>
     * <param name="request">The request containing username</param>
     * <returns>QR code and manual entry key for setting up 2FA</returns>
     */
    [HttpPost("initiate-2fa")]
    [AllowAnonymous]
    [SwaggerOperation(
        Summary = "Initiate 2FA setup",
        Description = "Generate QR code and manual entry key to set up or reset two-factor authentication",
        OperationId = "InitiateTwoFactor")]
    [SwaggerResponse(StatusCodes.Status200OK, "2FA setup information generated successfully")]
    public async Task<IActionResult> InitiateTwoFactor([FromBody] UsernameRequest request)
    {
        try
        {
            var user = await userRepository.FindByUsernameAsync(request.Username);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            var twoFactorSetup = twoFactorService.GenerateTwoFactorSecret(user.Username);

            return Ok(new
            {
                qrCodeDataUrl = twoFactorSetup.QrCodeDataUrl,
                manualEntryKey = twoFactorSetup.ManualEntryKey,
                message = "Scan the QR code with Google Authenticator or enter the key manually. Then verify with a code to complete setup."
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                message = "Failed to initiate 2FA setup",
                error = ex.Message
            });
        }
    }

    /**
     * <summary>
     *     Enable two-factor authentication
     * </summary>
     * <param name="resource">The verification resource containing username and code</param>
     * <returns>Success message</returns>
     */
    [HttpPost("enable-2fa")]
    [AllowAnonymous]
    [SwaggerOperation(
        Summary = "Enable 2FA",
        Description = "Re-enable two-factor authentication from user settings. Requires verifying a code to ensure user still has access to their authenticator.",
        OperationId = "EnableTwoFactor")]
    [SwaggerResponse(StatusCodes.Status200OK, "2FA was enabled successfully")]
    public async Task<IActionResult> EnableTwoFactor([FromBody] VerifyTwoFactorResource resource)
    {
        try
        {
            var command = new EnableTwoFactorCommand(resource.Username, resource.Code);
            await userCommandService.Handle(command);

            return Ok(new { message = "Two-factor authentication enabled successfully" });
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                message = "Failed to enable 2FA",
                error = ex.Message
            });
        }
    }

    /**
     * <summary>
     *     Disable two-factor authentication
     * </summary>
     * <param name="request">The request containing username</param>
     * <returns>Success message</returns>
     */
    [HttpPost("disable-2fa")]
    [AllowAnonymous]
    [SwaggerOperation(
        Summary = "Disable 2FA",
        Description = "Disable two-factor authentication from user settings. The secret is kept so user can re-enable easily.",
        OperationId = "DisableTwoFactor")]
    [SwaggerResponse(StatusCodes.Status200OK, "2FA was disabled successfully")]
    public async Task<IActionResult> DisableTwoFactor([FromBody] UsernameRequest request)
    {
        try
        {
            var command = new DisableTwoFactorCommand(request.Username);
            await userCommandService.Handle(command);

            return Ok(new { message = "Two-factor authentication disabled successfully" });
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                message = "Failed to disable 2FA",
                error = ex.Message
            });
        }
    }

    /**
     * <summary>
     *     Get two-factor authentication status
     * </summary>
     * <param name="username">The username</param>
     * <returns>2FA status information</returns>
     */
    [HttpGet("2fa-status")]
    [AllowAnonymous]
    [SwaggerOperation(
        Summary = "Get 2FA status",
        Description = "Get the two-factor authentication status for a user",
        OperationId = "GetTwoFactorStatus")]
    [SwaggerResponse(StatusCodes.Status200OK, "2FA status retrieved successfully", typeof(TwoFactorStatusResource))]
    public async Task<IActionResult> GetTwoFactorStatus([FromQuery] string username)
    {
        try
        {
            var user = await userRepository.FindByUsernameAsync(username);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            var resource = new TwoFactorStatusResource(
                Username: user.Username,
                TwoFactorEnabled: user.TwoFactorEnabled,
                TwoFactorConfigured: !string.IsNullOrEmpty(user.TwoFactorSecret)
            );

            return Ok(resource);
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                message = "Failed to get 2FA status",
                error = ex.Message
            });
        }
    }
}
