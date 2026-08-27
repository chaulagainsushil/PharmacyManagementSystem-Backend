namespace PMS.Common.DTOs.PharmacyProfile;

/// <summary>Full detail response — used for GET by ID, Create, and Update responses.</summary>
public class PharmacyProfileResponseDto
{
    public int PharmacyProfileId { get; set; }
    public Guid TenantId { get; set; }
    public string PharmacyName { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public string? SignatureUrl { get; set; }
    public string? PanNumber { get; set; }
    public string? VatNumber { get; set; }
    public string? Location { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<PharmacyPhoneNumberResponseDto> PhoneNumbers { get; set; } = new();
}
