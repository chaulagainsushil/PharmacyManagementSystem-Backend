namespace PMS.Common.DTOs.PharmacyProfile;

/// <summary>Phone number entry in a response.</summary>
public class PharmacyPhoneNumberResponseDto
{
    public int PharmacyPhoneNumberId { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Label { get; set; }
}
