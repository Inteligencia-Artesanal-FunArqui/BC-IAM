using OsitoPolar.IAM.Service.Domain.Model.Aggregates;
using OsitoPolar.IAM.Service.Domain.Model.Commands;

namespace OsitoPolar.IAM.Service.Domain.Services;

/**
 * <summary>
 *     The user command service
 * </summary>
 * <remarks>
 *     This interface is used to handle user commands
 * </remarks>
 */
public interface IUserCommandService
{
    /**
        * <summary>
        *     Handle sign in command
        * </summary>
        * <param name="command">The sign in command</param>
        * <returns>The authenticated user and the JWT token</returns>
        */
    Task<(User user, string token)> Handle(SignInCommand command);

    /**
        * <summary>
        *     Handle sign up command
        * </summary>
        * <param name="command">The sign up command</param>
        * <returns>A confirmation message on successful creation.</returns>
        */
    Task Handle(SignUpCommand command);

    /**
        * <summary>
        *     Handle verify two-factor command (for first-time setup)
        * </summary>
        * <param name="command">The verify two-factor command</param>
        * <returns>The authenticated user and the JWT token</returns>
        */
    Task<(User user, string token)> Handle(VerifyTwoFactorCommand command);

    /**
        * <summary>
        *     Handle enable two-factor command (re-enable from settings)
        * </summary>
        * <param name="command">The enable two-factor command</param>
        * <returns>Task completion</returns>
        */
    Task Handle(EnableTwoFactorCommand command);

    /**
        * <summary>
        *     Handle disable two-factor command (disable from settings)
        * </summary>
        * <param name="command">The disable two-factor command</param>
        * <returns>Task completion</returns>
        */
    Task Handle(DisableTwoFactorCommand command);
}