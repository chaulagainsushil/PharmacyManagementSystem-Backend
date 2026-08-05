using System.ComponentModel.DataAnnotations;

namespace PMS.Common.DTOs;

public class SignupDto
{
    [Required]
    [MaxLength(200)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;

    /// <summary>
    /// Role to assign to the new account.
    /// Must be one of the values defined in <see cref="AppRoles"/>.
    /// Only a SystemAdmin may assign SystemAdmin or PharmacyAdmin roles.
    /// Defaults to <see cref="AppRoles.Pharmacist"/> when not provided.
    /// </summary>
    public string? Role { get; set; }
}
