namespace PMS.Models.Models;

/// <summary>
/// One phone number entry belonging to a <see cref="PharmacyProfile"/>.
/// A pharmacy can have multiple numbers (landline, mobile, emergency, etc.).
/// </summary>
public class PharmacyPhoneNumber
{
    public int PharmacyPhoneNumberId { get; set; }

    public int PharmacyProfileId { get; set; }

    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>Optional label — e.g. "Landline", "Mobile", "Emergency".</summary>
    public string? Label { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // ── Navigation ────────────────────────────────────────────────────────────
    public PharmacyProfile PharmacyProfile { get; set; } = null!;
}
