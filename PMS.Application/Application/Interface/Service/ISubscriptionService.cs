using PMS.Common.DTOs.Subscription;
using PMS.Models.Enums;

namespace PMS.Application.Application.Interface.Service;

/// <summary>
/// Business logic for tenant subscription lifecycle management.
/// </summary>
public interface ISubscriptionService
{
    /// <summary>
    /// Returns the current active subscription for the calling tenant.
    /// Returns null if no active subscription exists.
    /// </summary>
    Task<SubscriptionResponseDto?> GetCurrentAsync(Guid tenantId);

    /// <summary>
    /// Renews the subscription for the given tenant using the same plan type.
    /// Cancels any existing Active subscription and creates a new one starting today.
    /// </summary>
    Task<SubscriptionResponseDto> RenewAsync(Guid tenantId, string? paymentReference);

    /// <summary>
    /// Upgrades (or downgrades) the subscription to the specified plan.
    /// Cancels any existing Active subscription and creates a new one for the new plan.
    /// </summary>
    Task<SubscriptionResponseDto> UpgradeAsync(Guid tenantId, PlanType newPlan, string? paymentReference);

    /// <summary>
    /// Creates the initial subscription for a brand-new tenant.
    /// Called during signup — uses the plan from the signup request (or defaults to Silver).
    /// </summary>
    Task<SubscriptionResponseDto> CreateInitialAsync(Guid tenantId, PlanType planType);

    /// <summary>
    /// Flips all Active subscriptions with a past EndDate to Expired.
    /// Intended for use by the background expiry job only.
    /// </summary>
    Task ExpireOverdueSubscriptionsAsync();
}
