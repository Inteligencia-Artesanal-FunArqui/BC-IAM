using OsitoPolar.IAM.Service.Application.Internal.OutboundServices;
using OsitoPolar.IAM.Service.Domain.Model.Aggregates;
using OsitoPolar.IAM.Service.Domain.Model.Commands;
using OsitoPolar.IAM.Service.Domain.Repositories;
using OsitoPolar.IAM.Service.Domain.Services;
using OsitoPolar.IAM.Service.Shared.Domain.Repositories;
// using OsitoPolarPlatform.API.Notifications.Application.Internal.CommandServices;
// using OsitoPolarPlatform.API.Notifications.Domain.Model.Commands;
// using OsitoPolarPlatform.API.Notifications.Domain.Model.ValueObjects;

namespace OsitoPolar.IAM.Service.Application.Internal.CommandServices;

/**
 * <summary>
 *     The user command service
 * </summary>
 * <remarks>
 *     This class is used to handle user commands
 * </remarks>
 */
public class UserCommandService(
    IUserRepository userRepository,
    ITokenService tokenService,
    IHashingService hashingService,
    IUnitOfWork unitOfWork,
    ITwoFactorService twoFactorService)
    // TODO: Re-enable email notifications when Notifications microservice is available
    // IEmailCommandService emailCommandService)
    : IUserCommandService
{
    /**
     * <summary>
     *     Handle sign in command
     * </summary>
     * <param name="command">The sign in command</param>
     * <returns>The authenticated user and the JWT token, or null token if 2FA setup is required</returns>
     */
    public async Task<(User user, string token)> Handle(SignInCommand command)
    {
        var user = await userRepository.FindByUsernameAsync(command.Username);

        if (user == null || !hashingService.VerifyPassword(command.Password, user.PasswordHash))
            throw new Exception("Invalid username or password");

        // Check if this is first login (no 2FA secret configured yet)
        if (string.IsNullOrEmpty(user.TwoFactorSecret))
        {
            Console.WriteLine($"[SignIn] First login detected for {user.Username} - generating 2FA secret");

            // Generate 2FA secret and QR code for first-time setup
            var twoFactorSetup = twoFactorService.GenerateTwoFactorSecret(user.Username);

            // Save the secret to the user (but don't enable 2FA yet)
            user.SetTwoFactorSecret(twoFactorSetup.Secret);
            userRepository.Update(user);
            await unitOfWork.CompleteAsync();

            Console.WriteLine($"[SignIn] 2FA secret generated and saved for {user.Username}");

            // Return user with empty token (frontend will show QR code setup)
            return (user, string.Empty);
        }

        // If 2FA is enabled, don't generate token yet (user must verify code)
        if (user.TwoFactorEnabled)
        {
            Console.WriteLine($"[SignIn] 2FA is enabled for {user.Username} - waiting for verification code");
            // Return user with empty token (frontend will prompt for 2FA code)
            return (user, string.Empty);
        }

        // 2FA is configured but disabled - allow login
        Console.WriteLine($"[SignIn] 2FA is disabled for {user.Username} - generating token");
        var token = tokenService.GenerateToken(user);
        return (user, token);
    }

    /**
     * <summary>
     *     Handle sign-up command
     * </summary>
     * <param name="command">The sign-up command</param>
     * <returns>A confirmation message on successful creation.</returns>
     */
    public async Task Handle(SignUpCommand command)
    {
        try
        {
            Console.WriteLine($"[SignUp] Attempting to create user: {command.Username}");
            Console.WriteLine($"[SignUp] Password length: {command.Password?.Length ?? 0}");
        
            if (string.IsNullOrWhiteSpace(command.Username) || string.IsNullOrWhiteSpace(command.Password))
            {
                Console.WriteLine("[SignUp] ERROR: Username or password is empty");
                throw new ArgumentException("Username and password cannot be empty");
            }
            
            Console.WriteLine("[SignUp] Checking if username exists...");
            if (userRepository.ExistsByUsername(command.Username))
            {
                Console.WriteLine($"[SignUp] ERROR: Username {command.Username} already exists");
                throw new Exception($"Username {command.Username} is already taken");
            }

            Console.WriteLine("[SignUp] Hashing password...");
            var hashedPassword = hashingService.HashPassword(command.Password);
        
            Console.WriteLine("[SignUp] Creating user object...");
            var user = new User(command.Username, hashedPassword);
    
            Console.WriteLine("[SignUp] Adding user to repository...");
            await userRepository.AddAsync(user);
        
            Console.WriteLine("[SignUp] Completing transaction...");
            await unitOfWork.CompleteAsync();
        
            Console.WriteLine($"[SignUp] SUCCESS: User {command.Username} created successfully");
        }
        catch (Exception e)
        {
            Console.WriteLine($"[SignUp] EXCEPTION: {e.GetType().Name}: {e.Message}");
            Console.WriteLine($"[SignUp] Stack Trace: {e.StackTrace}");
            throw new Exception($"An error occurred while creating user: {e.Message}");
        }
    }

    /**
     * <summary>
     *     Handle verify two-factor command (for first-time setup or login with 2FA enabled)
     * </summary>
     * <param name="command">The verify two-factor command</param>
     * <returns>The authenticated user and the JWT token</returns>
     */
    public async Task<(User user, string token)> Handle(VerifyTwoFactorCommand command)
    {
        Console.WriteLine($"[VerifyTwoFactor] Verifying 2FA code for {command.Username}");

        var user = await userRepository.FindByUsernameAsync(command.Username);
        if (user == null)
            throw new Exception("User not found");

        // Validate the 2FA code
        var isValid = twoFactorService.ValidateTwoFactorCode(user.TwoFactorSecret!, command.Code);
        if (!isValid)
        {
            Console.WriteLine($"[VerifyTwoFactor] Invalid 2FA code for {command.Username}");
            throw new Exception("Invalid 2FA code");
        }

        Console.WriteLine($"[VerifyTwoFactor] Valid 2FA code for {command.Username}");

        // If 2FA is not enabled yet, this is first-time setup - enable it
        if (!user.TwoFactorEnabled)
        {
            Console.WriteLine($"[VerifyTwoFactor] First-time setup - enabling 2FA for {command.Username}");
            user.EnableTwoFactor();
            userRepository.Update(user);
            await unitOfWork.CompleteAsync();

            // TODO: Re-enable email notifications when Notifications microservice is available
            // Send 2FA setup confirmation email
            /*
            try
            {
                await emailCommandService.SendTemplatedEmailAsync(new SendEmailCommand
                {
                    To = user.Username,
                    ToName = user.Username.Split('@')[0],
                    Template = EmailTemplate.TwoFactorSetup,
                    TemplateData = new Dictionary<string, object>
                    {
                        { "UserName", user.Username },
                        { "SetupDate", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC") }
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[VerifyTwoFactor] Failed to send 2FA setup email: {ex.Message}");
                // Don't fail the request if email fails
            }
            */
        }

        // Generate and return JWT token
        var token = tokenService.GenerateToken(user);
        Console.WriteLine($"[VerifyTwoFactor] Token generated for {command.Username}");

        return (user, token);
    }

    /**
     * <summary>
     *     Handle enable two-factor command (re-enable from settings)
     * </summary>
     * <param name="command">The enable two-factor command</param>
     * <returns>Task completion</returns>
     */
    public async Task Handle(EnableTwoFactorCommand command)
    {
        Console.WriteLine($"[EnableTwoFactor] Enabling 2FA for {command.Username}");

        var user = await userRepository.FindByUsernameAsync(command.Username);
        if (user == null)
            throw new Exception("User not found");

        // Validate the 2FA code to ensure user still has access to their authenticator
        var isValid = twoFactorService.ValidateTwoFactorCode(user.TwoFactorSecret!, command.Code);
        if (!isValid)
        {
            Console.WriteLine($"[EnableTwoFactor] Invalid 2FA code for {command.Username}");
            throw new Exception("Invalid 2FA code");
        }

        // Enable 2FA
        user.EnableTwoFactor();
        userRepository.Update(user);
        await unitOfWork.CompleteAsync();

        Console.WriteLine($"[EnableTwoFactor] 2FA enabled for {command.Username}");
    }

    /**
     * <summary>
     *     Handle disable two-factor command (disable from settings)
     * </summary>
     * <param name="command">The disable two-factor command</param>
     * <returns>Task completion</returns>
     */
    public async Task Handle(DisableTwoFactorCommand command)
    {
        Console.WriteLine($"[DisableTwoFactor] Disabling 2FA for {command.Username}");

        var user = await userRepository.FindByUsernameAsync(command.Username);
        if (user == null)
            throw new Exception("User not found");

        // Disable 2FA (keep the secret so user can re-enable easily)
        user.DisableTwoFactor();
        userRepository.Update(user);
        await unitOfWork.CompleteAsync();

        Console.WriteLine($"[DisableTwoFactor] 2FA disabled for {command.Username}");
    }
}
