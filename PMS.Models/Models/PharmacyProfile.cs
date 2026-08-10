namespace PMS.Models.Models;

/// <summary>
/// Tenant root — each PharmacyProfile represents one independent tenant.
/// TenantId is the stable, globally-unique identifier used across all tenant-scoped entities.
/// PharmacyProfileId remains the int PK for FK relationships within this tenant's data.
/// </summary>
public class PharmacyProfile
{
    public int PharmacyProfileId { get; set; }

    /// <summary>
    /// Stable, globally-unique tenant identifier.
    /// Embedded into every JWT as the "tenantId" claim and used by
    /// global query filters to isolate each tenant's data.
    /// </summary>
    public Guid TenantId { get; set; } = Guid.NewGuid();

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
    public ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
}
