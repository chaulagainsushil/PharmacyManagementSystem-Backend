namespace PMS.Models.Enums;

/// <summary>
/// Subscription plan tiers. Controls duration only — no feature differences.
/// Silver = 6 months, Gold = 1 year, Diamond = lifetime (EndDate = null).
/// </summary>
public enum PlanType
{
    Silver = 1,
    Gold   = 2,
    Diamond = 3
}
