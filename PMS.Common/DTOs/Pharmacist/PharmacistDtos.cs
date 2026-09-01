using System.ComponentModel.DataAnnotations;
using PMS.Common;

namespace PMS.Common.DTOs.Pharmacist;

public class PharmacistListItemDto
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = AppRoles.Pharmacist;
    public int? AppUserId { get; set; }
    public int TotalSalesCount { get; set; }
    public DateTime? CreatedAt { get; set; }
}

public class CreatePharmacistRequestDto
{
    [Required]
    [MaxLength(200)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters.")]
    public string Password { get; set; } = string.Empty;

    public string Role { get; set; } = AppRoles.Pharmacist;
}

public class UpdatePharmacistRequestDto
{
    [Required]
    [MaxLength(200)]
    public string FullName { get; set; } = string.Empty;

    public string Role { get; set; } = AppRoles.Pharmacist;

    /// <summary>
    /// Optional: if provided, updates the pharmacist's password.
    /// </summary>
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters if provided.")]
    public string? NewPassword { get; set; }
}
