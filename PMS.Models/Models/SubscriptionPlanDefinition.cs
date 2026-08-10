using PMS.Models.Enums;

namespace PMS.Models.Models;

/// <summary>
/// Read-only lookup table describing each subscription plan.
/// Seeded on startup; never mutated at runtime.
/// </summary>
public class SubscriptionPlanDefinition
{
    public int SubscriptionPlanDefinitionId { get; set; }

    /// <summary>Unique plan tier identifier.</summary>
    public PlanType PlanType { get; set; }

    /// <summary>
    /// How many days the plan lasts from the start date.
    /// <c>null</c> means lifetime (Diamond) — EndDate will be null on the Subscription.
    /// </summary>
    public int? DurationInDays { get; set; }

    /// <summary>Display price for this plan.</summary>
    public decimal Price { get; set; }

    /// <summary>Human-readable description shown in billing UI.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Maximum number of medicines a tenant on this plan can create.
    /// <c>null</c> means unlimited (Silver, Gold, Diamond).
    /// </summary>
    public int? MedicineLimit { get; set; }
}
