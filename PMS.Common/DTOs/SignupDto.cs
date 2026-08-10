using System.ComponentModel.DataAnnotations;
using PMS.Models.Enums;

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
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters.")]
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

    // ── Tenant creation fields (used only by POST /api/auth/tenant-signup) ────

    /// <summary>
    /// The pharmacy / business name for the new tenant.
    /// Required when creating a new tenant via tenant-signup; ignored for create-user.
    /// </summary>
    [MaxLength(300)]
    public string? PharmacyName { get; set; }

    /// <summary>
    /// Subscription plan for the new tenant. Defaults to Silver when not provided.
    /// </summary>
    public PlanType InitialPlan { get; set; } = PlanType.Silver;
}
