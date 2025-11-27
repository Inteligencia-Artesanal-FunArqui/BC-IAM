using OsitoPolar.IAM.Service.Application.Internal.OutboundServices;
using OsitoPolar.IAM.Service.Domain.Model.Aggregates;
using OsitoPolar.IAM.Service.Domain.Repositories;
using OsitoPolar.IAM.Service.Domain.Services;
using OsitoPolar.IAM.Service.Interfaces.REST.Resources;
using OsitoPolar.IAM.Service.Infrastructure.External.Http;
using OsitoPolar.IAM.Service.Shared.Domain.Repositories;

namespace OsitoPolar.IAM.Service.Application.Internal.CommandServices;

/// <summary>
/// Service for handling complete user registration with payment and profile creation.
/// Uses HTTP Facades for cross-service communication in microservices architecture.
/// </summary>
public class RegistrationService : IRegistrationService
{
    private readonly IUserRepository _userRepository;
    private readonly IProfilesHttpFacade _profilesFacade;
    private readonly ISubscriptionsHttpFacade _subscriptionFacade;
    private readonly INotificationsHttpFacade _notificationFacade;
    private readonly IPaymentProvider _paymentProvider;
    private readonly IHashingService _hashingService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RegistrationService> _logger;

    public RegistrationService(
        IUserRepository userRepository,
        IProfilesHttpFacade profilesFacade,
        ISubscriptionsHttpFacade subscriptionFacade,
        INotificationsHttpFacade notificationFacade,
        IPaymentProvider paymentProvider,
        IHashingService hashingService,
        IUnitOfWork unitOfWork,
        ILogger<RegistrationService> logger)
    {
        _userRepository = userRepository;
        _profilesFacade = profilesFacade;
        _subscriptionFacade = subscriptionFacade;
        _notificationFacade = notificationFacade;
        _paymentProvider = paymentProvider;
        _hashingService = hashingService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<RegisterWithPaymentResponse> RegisterWithPaymentAsync(RegisterWithPaymentResource request)
    {
        try
        {
            _logger.LogInformation("[Registration] Starting registration for {Username} as {UserType}",
                request.Username, request.UserType);

            // 1. Validate user type
            if (request.UserType != "Owner" && request.UserType != "Provider")
            {
                return new RegisterWithPaymentResponse
                {
                    Success = false,
                    ErrorMessage = "Invalid user type. Must be 'Owner' or 'Provider'"
                };
            }

            // 2. Validate plan ID matches user type
            var subscriptionData = await _subscriptionFacade.GetFullSubscriptionData(request.PlanId);
            if (subscriptionData == null)
            {
                return new RegisterWithPaymentResponse
                {
                    Success = false,
                    ErrorMessage = $"Invalid plan ID: {request.PlanId}"
                };
            }

            // Validate plan type matches user type
            var isOwnerPlan = subscriptionData.Value.maxEquipment.HasValue;
            var isProviderPlan = subscriptionData.Value.maxClients.HasValue;

            if (request.UserType == "Owner" && !isOwnerPlan)
            {
                return new RegisterWithPaymentResponse
                {
                    Success = false,
                    ErrorMessage = "Selected plan is not an Owner plan (plans 1-3)"
                };
            }

            if (request.UserType == "Provider" && !isProviderPlan)
            {
                return new RegisterWithPaymentResponse
                {
                    Success = false,
                    ErrorMessage = "Selected plan is not a Provider plan (plans 4-6)"
                };
            }

            // 3. Check if username already exists
            if (_userRepository.ExistsByUsername(request.Username))
            {
                return new RegisterWithPaymentResponse
                {
                    Success = false,
                    ErrorMessage = $"Username {request.Username} is already taken"
                };
            }

            // 4. Check if email is already used
            var ownerEmailExists = await _profilesFacade.CheckOwnerEmailExists(request.Email);
            var providerEmailExists = await _profilesFacade.CheckProviderEmailExists(request.Email);

            if (ownerEmailExists || providerEmailExists)
            {
                return new RegisterWithPaymentResponse
                {
                    Success = false,
                    ErrorMessage = $"Email {request.Email} is already registered"
                };
            }

            // 5. Process payment with Stripe
            _logger.LogInformation("[Registration] Processing payment of {Price} {Currency}",
                subscriptionData.Value.price, subscriptionData.Value.currency);

            var paymentRequest = new PaymentRequest
            {
                Amount = subscriptionData.Value.price,
                Currency = subscriptionData.Value.currency,
                Description = $"{subscriptionData.Value.planName} subscription for {request.Username}",
                CustomerEmail = request.Email,
                CustomerName = $"{request.FirstName} {request.LastName}",
                PaymentToken = request.PaymentToken,
                Metadata = new Dictionary<string, string>
                {
                    { "username", request.Username },
                    { "userType", request.UserType },
                    { "planId", request.PlanId.ToString() },
                    { "planName", subscriptionData.Value.planName }
                }
            };

            var paymentResult = await _paymentProvider.CreatePaymentAsync(paymentRequest);

            if (!paymentResult.Success)
            {
                _logger.LogWarning("[Registration] Payment failed: {ErrorMessage}", paymentResult.ErrorMessage);
                return new RegisterWithPaymentResponse
                {
                    Success = false,
                    ErrorMessage = $"Payment failed: {paymentResult.ErrorMessage}"
                };
            }

            _logger.LogInformation("[Registration] Payment succeeded: {TransactionId}", paymentResult.TransactionId);

            // 6. Generate random password
            var generatedPassword = GenerateSecurePassword();

            // 7. Create user account
            var hashedPassword = _hashingService.HashPassword(generatedPassword);
            var user = new User(request.Username, hashedPassword);
            await _userRepository.AddAsync(user);
            await _unitOfWork.CompleteAsync();

            _logger.LogInformation("[Registration] User account created with ID: {UserId}", user.Id);

            // 8. Create Owner or Provider profile
            int profileId;

            if (request.UserType == "Owner")
            {
                profileId = await _profilesFacade.CreateOwnerProfile(
                    user.Id,
                    request.FirstName,
                    request.LastName,
                    request.Email,
                    request.Street,
                    request.Number,
                    request.City,
                    request.PostalCode,
                    request.Country,
                    request.PlanId,
                    subscriptionData.Value.maxEquipment!.Value
                );

                _logger.LogInformation("[Registration] Owner profile created with ID: {ProfileId}", profileId);
            }
            else // Provider
            {
                if (string.IsNullOrEmpty(request.CompanyName))
                {
                    return new RegisterWithPaymentResponse
                    {
                        Success = false,
                        ErrorMessage = "Company name is required for Provider registration"
                    };
                }

                profileId = await _profilesFacade.CreateProviderProfile(
                    user.Id,
                    request.CompanyName,
                    request.FirstName,
                    request.LastName,
                    request.Email,
                    request.Street,
                    request.Number,
                    request.City,
                    request.PostalCode,
                    request.Country,
                    request.PlanId,
                    subscriptionData.Value.maxClients!.Value,
                    request.TaxId ?? ""
                );

                _logger.LogInformation("[Registration] Provider profile created with ID: {ProfileId}", profileId);
            }

            // 9. Send welcome email with credentials
            try
            {
                var emailSubject = "Welcome to OsitoPolar - Your Account Details";
                var emailBody = GenerateWelcomeEmailBody(
                    request.FirstName,
                    request.UserType,
                    subscriptionData.Value.planName,
                    subscriptionData.Value.price,
                    subscriptionData.Value.maxEquipment,
                    subscriptionData.Value.maxClients,
                    request.Username,
                    generatedPassword);

                await _notificationFacade.SendEmailNotification(
                    request.Email,
                    $"{request.FirstName} {request.LastName}",
                    emailSubject,
                    emailBody
                );

                _logger.LogInformation("[Registration] Welcome email sent to {Email}", request.Email);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[Registration] Failed to send welcome email to {Email}", request.Email);
                // Don't fail registration if email fails
            }

            // 10. Return success response
            return new RegisterWithPaymentResponse
            {
                Success = true,
                Message = "Registration completed successfully! Check your email for login credentials.",
                UserId = user.Id,
                Username = request.Username,
                GeneratedPassword = generatedPassword,
                UserType = request.UserType,
                ProfileId = profileId,
                TransactionId = paymentResult.TransactionId
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Registration] Error during registration for {Username}", request.Username);

            return new RegisterWithPaymentResponse
            {
                Success = false,
                ErrorMessage = $"Registration failed: {ex.Message}"
            };
        }
    }

    private string GenerateWelcomeEmailBody(string firstName, string userType, string planName,
        decimal price, int? maxEquipment, int? maxClients, string username, string password)
    {
        var limitInfo = userType == "Owner"
            ? $"<li>Max Equipment: {maxEquipment} units</li>"
            : $"<li>Max Clients: {(maxClients == 0 ? "Unlimited" : maxClients.ToString())}</li>";

        return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #2c3e50; color: white; padding: 20px; text-align: center; }}
        .content {{ background-color: #f4f4f4; padding: 20px; }}
        .credentials {{ background-color: white; padding: 15px; margin: 15px 0; border-left: 4px solid #3498db; }}
        .button {{ background-color: #3498db; color: white; padding: 12px 30px; text-decoration: none; display: inline-block; margin: 15px 0; border-radius: 5px; }}
        .footer {{ text-align: center; padding: 20px; color: #777; font-size: 12px; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>Welcome to OsitoPolar!</h1>
        </div>
        <div class=""content"">
            <h2>Hello {firstName}!</h2>
            <p>Thank you for registering as a <strong>{userType}</strong> with OsitoPolar.</p>
            <p>Your subscription to <strong>{planName}</strong> has been activated.</p>

            <div class=""credentials"">
                <h3>Your Login Credentials:</h3>
                <p><strong>Username:</strong> {username}</p>
                <p><strong>Password:</strong> {password}</p>
                <p style=""color: #e74c3c; font-size: 14px;"">Please change your password after your first login for security.</p>
            </div>

            <p><strong>Plan Details:</strong></p>
            <ul>
                <li>Plan: {planName}</li>
                <li>Price: ${price}/month</li>
                {limitInfo}
            </ul>

            <p>If you have any questions, please don't hesitate to contact our support team.</p>
        </div>
        <div class=""footer"">
            <p>2025 OsitoPolar. All rights reserved.</p>
            <p>This is an automated email. Please do not reply.</p>
        </div>
    </div>
</body>
</html>";
    }

    private string GenerateSecurePassword()
    {
        const string uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string lowercase = "abcdefghijklmnopqrstuvwxyz";
        const string digits = "0123456789";
        const string special = "!@#$%^&*";
        const string allChars = uppercase + lowercase + digits + special;

        var random = new Random();
        var password = new char[12];

        // Ensure at least one of each type
        password[0] = uppercase[random.Next(uppercase.Length)];
        password[1] = lowercase[random.Next(lowercase.Length)];
        password[2] = digits[random.Next(digits.Length)];
        password[3] = special[random.Next(special.Length)];

        // Fill the rest randomly
        for (int i = 4; i < password.Length; i++)
        {
            password[i] = allChars[random.Next(allChars.Length)];
        }

        // Shuffle
        return new string(password.OrderBy(x => random.Next()).ToArray());
    }
}
