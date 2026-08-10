using PMS.Models.Enums;

namespace PMS.Models.Models;

/// <summary>
/// Represents a tenant's active (or historic) subscription.
/// Each tenant has at most one Active subscription at any time.
/// </summary>
public class Subscription
{
    public int SubscriptionId { get; set; }

    /// <summary>FK to PharmacyProfile.TenantId — the tenant that owns this subscription.</summary>
    public Guid TenantId { get; set; }

    public PlanType PlanType { get; set; }

    public DateTime StartDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Calculated from SubscriptionPlanDefinition.DurationInDays.
    /// <c>null</c> for Diamond (lifetime) plans — a null EndDate never expires.
    /// </summary>
    public DateTime? EndDate { get; set; }

    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Active;

    /// <summary>
    /// Optional external payment reference (e.g. Stripe charge ID, invoice number).
    /// </summary>
    public string? PaymentReference { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // ── Navigation ────────────────────────────────────────────────────────────
    /// <summary>Navigation to the tenant (PharmacyProfile) that owns this subscription.</summary>
    public PharmacyProfile Tenant { get; set; } = null!;

    // ── Domain logic ──────────────────────────────────────────────────────────

    /// <summary>
    /// Returns true if the subscription is currently valid (Active + not expired).
    /// A null EndDate (Diamond) never expires.
    /// </summary>
    public bool IsCurrentlyActive()
        => Status == SubscriptionStatus.Active
           && (EndDate == null || EndDate.Value > DateTime.UtcNow);
}
