using PMS.Common.DTOs;

namespace PMS.Application.Application.Interface.Service;

public interface IAuthService
{
    /// <summary>
    /// Creates a new tenant (PharmacyProfile) + initial Subscription + PharmacyAdmin user
    /// all in a single transaction. Returns a JWT on success.
    /// </summary>
    Task<AuthResponseDto> TenantSignupAsync(SignupDto dto);

    /// <summary>
    /// Signs up a new user inside an existing tenant.
    /// The caller must already be authenticated and their tenantId claim must resolve
    /// to a valid PharmacyProfile — the new user is scoped to the same tenant.
    /// Returns token on success.
    /// </summary>
    Task<AuthResponseDto> SignupAsync(SignupDto dto);

    /// <summary>Logs in an existing user. Returns token on success.</summary>
    Task<AuthResponseDto> LoginAsync(LoginDto dto);

    /// <summary>
    /// Creates a new user and assigns the specified role.
    /// Only callable by a SystemAdmin — enforced at the controller layer.
    /// </summary>
    Task<AuthResponseDto> CreateUserAsync(SignupDto dto);

    /// <summary>
    /// Assigns (or re-assigns) a role to an existing user identified by email.
    /// Only callable by a SystemAdmin — enforced at the controller layer.
    /// </summary>
    Task<AuthResponseDto> AssignRoleAsync(AssignRoleDto dto);

    /// <summary>
    /// Changes the password for the currently authenticated user.
    /// Requires the user's current password for verification.
    /// The new password must satisfy all configured PasswordPolicy rules.
    /// </summary>
    Task<AuthResponseDto> ChangePasswordAsync(string userId, ChangePasswordDto dto);

    /// <summary>
    /// Initiates a password reset for the given email address.
    /// Generates a time-limited token — in production this should be emailed to the user.
    /// Always returns success to avoid leaking whether the email is registered (prevents enumeration).
    /// </summary>
    Task<AuthResponseDto> ForgotPasswordAsync(ForgotPasswordDto dto);

    /// <summary>
    /// Completes the password reset using the token issued by <see cref="ForgotPasswordAsync"/>.
    /// The new password must satisfy all configured PasswordPolicy rules.
    /// </summary>
    Task<AuthResponseDto> ResetPasswordAsync(ResetPasswordDto dto);
}
