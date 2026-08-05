using System.ComponentModel.DataAnnotations;

namespace PMS.Common.DTOs.PharmacyProfile;

/// <summary>Represents one phone number entry sent in a create/update request.</summary>
public class PharmacyPhoneNumberDto
{
    [Required(ErrorMessage = "Phone number is required.")]
    [MaxLength(20, ErrorMessage = "Phone number must not exceed 20 characters.")]
    public string PhoneNumber { get; set; } = string.Empty;

    [MaxLength(50, ErrorMessage = "Label must not exceed 50 characters.")]
    public string? Label { get; set; }
}
