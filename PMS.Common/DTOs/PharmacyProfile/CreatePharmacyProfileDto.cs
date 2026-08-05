using System.ComponentModel.DataAnnotations;

namespace PMS.Common.DTOs.PharmacyProfile;

public class CreatePharmacyProfileDto
{
    [Required(ErrorMessage = "Pharmacy name is required.")]
    [MaxLength(300, ErrorMessage = "Pharmacy name must not exceed 300 characters.")]
    public string PharmacyName { get; set; } = string.Empty;

    [MaxLength(20, ErrorMessage = "PAN number must not exceed 20 characters.")]
    public string? PanNumber { get; set; }

    [MaxLength(20, ErrorMessage = "VAT number must not exceed 20 characters.")]
    public string? VatNumber { get; set; }

    [MaxLength(500, ErrorMessage = "Location must not exceed 500 characters.")]
    public string? Location { get; set; }

    /// <summary>
    /// At least one phone number is required.
    /// Logo and signature are uploaded via separate file-upload endpoints.
    /// </summary>
    [Required(ErrorMessage = "At least one phone number is required.")]
    [MinLength(1, ErrorMessage = "At least one phone number is required.")]
    public List<PharmacyPhoneNumberDto> PhoneNumbers { get; set; } = new();
}
