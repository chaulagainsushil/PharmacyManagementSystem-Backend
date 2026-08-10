namespace PMS.Models.Enums;

/// <summary>
/// Subscription plan tiers.
/// Trial = 15 days, 20-medicine limit (auto-assigned on signup, free).
/// Silver = 6 months, Gold = 1 year, Diamond = lifetime (EndDate = null).
/// </summary>
public enum PlanType
{
    Trial   = 0,
    Silver  = 1,
    Gold    = 2,
    Diamond = 3
}
