using PMS.Models.Enums;

namespace PMS.Common.DTOs.Subscription;

/// <summary>Returned by GET /api/subscription/mine and POST renew/upgrade.</summary>
public class SubscriptionResponseDto
{
    public int SubscriptionId { get; set; }
    public Guid TenantId { get; set; }
    public PlanType PlanType { get; set; }
    public DateTime StartDate { get; set; }

    /// <summary>Null for Diamond (lifetime) plans.</summary>
    public DateTime? EndDate { get; set; }

    public SubscriptionStatus Status { get; set; }
    public string? PaymentReference { get; set; }

    /// <summary>Convenience flag — true when the subscription is currently valid.</summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Days remaining until expiry.
    /// Null for Diamond plans (no expiry) or when EndDate has already passed.
    /// </summary>
    public int? DaysRemaining { get; set; }
}

/// <summary>Request body for POST /api/subscription/renew</summary>
public class RenewSubscriptionRequestDto
{
    public string? PaymentReference { get; set; }
}

/// <summary>Request body for POST /api/subscription/upgrade</summary>
public class UpgradeSubscriptionRequestDto
{
    public PlanType NewPlan { get; set; }
    public string? PaymentReference { get; set; }
}
