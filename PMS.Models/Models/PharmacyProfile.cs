namespace PMS.Models.Models;

/// <summary>
/// Stores the single pharmacy's identity information —
/// name, logo, signature image, PAN, VAT, location and contact numbers.
/// Only one active profile should exist at a time; the API enforces this.
/// </summary>
public class PharmacyProfile
{
    public int PharmacyProfileId { get; set; }

    public string PharmacyName { get; set; } = string.Empty;

    /// <summary>Relative URL to the stored logo file, e.g. /pharmacy/logo_abc.png</summary>
    public string? LogoUrl { get; set; }

    /// <summary>Relative URL to the stored authorised-signature image.</summary>
    public string? SignatureUrl { get; set; }

    /// <summary>Permanent Account Number (PAN).</summary>
    public string? PanNumber { get; set; }

    /// <summary>VAT registration number.</summary>
    public string? VatNumber { get; set; }

    public string? Location { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // ── Navigation ────────────────────────────────────────────────────────────
    public ICollection<PharmacyPhoneNumber> PhoneNumbers { get; set; } = new List<PharmacyPhoneNumber>();
}
