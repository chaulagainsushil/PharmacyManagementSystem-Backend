namespace PMS.Models.Enums;

/// <summary>Lifecycle state of a tenant's Subscription record.</summary>
public enum SubscriptionStatus
{
    Active    = 1,
    Expired   = 2,
    Cancelled = 3
}
