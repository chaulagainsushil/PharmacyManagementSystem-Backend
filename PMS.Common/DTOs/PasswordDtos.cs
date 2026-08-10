using System.ComponentModel.DataAnnotations;

namespace PMS.Common.DTOs;

/// <summary>
/// Payload for the change-password endpoint.
/// The authenticated user must supply their current password and a new password
/// that satisfies all configured PasswordPolicy rules.
/// </summary>
public sealed class ChangePasswordDto
{
    [Required(ErrorMessage = "Current password is required.")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "New password is required.")]
    [MinLength(8, ErrorMessage = "New password must be at least 8 characters.")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please confirm your new password.")]
    [Compare(nameof(NewPassword), ErrorMessage = "New password and confirmation do not match.")]
    public string ConfirmNewPassword { get; set; } = string.Empty;
}

/// <summary>
/// Payload for initiating a password reset.
/// The API will generate a time-limited reset token and (in production) email it to the user.
/// In this implementation the token is returned in the response for integration convenience —
/// in a real deployment, send it via email and remove it from the response.
/// </summary>
public sealed class ForgotPasswordDto
{
    [Required(ErrorMessage = "Email address is required.")]
    [EmailAddress(ErrorMessage = "A valid email address is required.")]
    public string Email { get; set; } = string.Empty;
}

/// <summary>
/// Payload for completing a password reset using the token that was issued
/// by the forgot-password endpoint.
/// </summary>
public sealed class ResetPasswordDto
{
    [Required(ErrorMessage = "Email address is required.")]
    [EmailAddress(ErrorMessage = "A valid email address is required.")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// The base64-encoded reset token returned by POST /api/auth/forgot-password.
    /// Pass the value exactly as received (URL encoding will be handled server-side).
    /// </summary>
    [Required(ErrorMessage = "Reset token is required.")]
    public string Token { get; set; } = string.Empty;

    [Required(ErrorMessage = "New password is required.")]
    [MinLength(8, ErrorMessage = "New password must be at least 8 characters.")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please confirm your new password.")]
    [Compare(nameof(NewPassword), ErrorMessage = "New password and confirmation do not match.")]
    public string ConfirmNewPassword { get; set; } = string.Empty;
}
