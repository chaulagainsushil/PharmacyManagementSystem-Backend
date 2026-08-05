namespace PMS.Common.DTOs.PharmacyProfile;

/// <summary>
/// Lightweight response — used for GET all (list view).
/// Includes the primary phone number for quick display.
/// </summary>
public class PharmacyProfileListItemDto
{
    public int PharmacyProfileId { get; set; }
    public string PharmacyName { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public string? PanNumber { get; set; }
    public string? VatNumber { get; set; }
    public string? Location { get; set; }
    public List<PharmacyPhoneNumberResponseDto> PhoneNumbers { get; set; } = new();
    public DateTime UpdatedAt { get; set; }
}
