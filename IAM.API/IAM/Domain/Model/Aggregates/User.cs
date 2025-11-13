using System.Text.Json.Serialization;

namespace OsitoPolar.IAM.Service.Domain.Model.Aggregates;

/**
 * <summary>
 *     The user aggregate
 * </summary>
 * <remarks>
 *     This class is used to represent a user
 * </remarks>
 */
public partial class User(string username, string passwordHash)
{
    public User() : this(string.Empty, string.Empty)
    {
    }

    public int Id { get; }
    public string Username { get; private set; } = username;

    [JsonIgnore] public string PasswordHash { get; private set; } = passwordHash;

    // Two-Factor Authentication fields
    [JsonIgnore] public string? TwoFactorSecret { get; private set; }
    public bool TwoFactorEnabled { get; private set; }
    public bool PasswordMustChange { get; private set; }

    /**
     * <summary>
     *     Update the username
     * </summary>
     * <param name="username">The new username</param>
     * <returns>The updated user</returns>
     */
    public User UpdateUsername(string username)
    {
        Username = username;
        return this;
    }

    /**
     * <summary>
     *     Update the password hash
     * </summary>
     * <param name="passwordHash">The new password hash</param>
     * <returns>The updated user</returns>
     */
    public User UpdatePasswordHash(string passwordHash)
    {
        PasswordHash = passwordHash;
        return this;
    }

    /**
     * <summary>
     *     Set the two-factor secret for the user
     * </summary>
     * <param name="secret">The Base32-encoded secret key</param>
     * <returns>The updated user</returns>
     */
    public User SetTwoFactorSecret(string secret)
    {
        TwoFactorSecret = secret;
        return this;
    }

    /**
     * <summary>
     *     Enable two-factor authentication for the user
     * </summary>
     * <returns>The updated user</returns>
     */
    public User EnableTwoFactor()
    {
        if (string.IsNullOrEmpty(TwoFactorSecret))
            throw new InvalidOperationException("Cannot enable 2FA without a secret. Generate a secret first.");

        TwoFactorEnabled = true;
        return this;
    }

    /**
     * <summary>
     *     Disable two-factor authentication for the user
     * </summary>
     * <returns>The updated user</returns>
     */
    public User DisableTwoFactor()
    {
        TwoFactorEnabled = false;
        return this;
    }

    /**
     * <summary>
     *     Set whether the user must change their password on next login
     * </summary>
     * <param name="mustChange">True if password must be changed, false otherwise</param>
     * <returns>The updated user</returns>
     */
    public User SetPasswordMustChange(bool mustChange)
    {
        PasswordMustChange = mustChange;
        return this;
    }
}