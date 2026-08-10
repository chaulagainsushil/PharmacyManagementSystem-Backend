using PMS.Models.Enums;
using PMS.Models.Models;

namespace PMS.Application.Application.Interface.Repository;

/// <summary>
/// Data access for <see cref="Subscription"/> and <see cref="SubscriptionPlanDefinition"/> records.
/// All operations bypass the global tenant query filter where needed (e.g. expiry job).
/// </summary>
public interface ISubscriptionRepository
{
    /// <summary>Returns the current Active subscription for the given tenant, or null.</summary>
    Task<Subscription?> GetActiveByTenantAsync(Guid tenantId);

    /// <summary>Returns all subscriptions for the given tenant (including historic ones).</summary>
    Task<IReadOnlyList<Subscription>> GetAllByTenantAsync(Guid tenantId);

    /// <summary>Persists a new subscription.</summary>
    Task<Subscription> CreateAsync(Subscription subscription);

    /// <summary>Updates an existing subscription (status change, end-date extension, etc.).</summary>
    Task UpdateAsync(Subscription subscription);

    /// <summary>
    /// Returns every Active subscription whose EndDate is not null and is in the past.
    /// Used by the expiry background job — ignores the global tenant filter.
    /// </summary>
    Task<IReadOnlyList<Subscription>> GetExpiredActiveSubscriptionsAsync();

    /// <summary>Returns the plan definition for the given plan type.</summary>
    Task<SubscriptionPlanDefinition?> GetPlanDefinitionAsync(PlanType planType);

    /// <summary>
    /// Returns the plan definition for the active subscription of the given tenant.
    /// Used to check feature limits (e.g. MedicineLimit) in service layer.
    /// </summary>
    Task<SubscriptionPlanDefinition?> GetPlanDefinitionByTenantAsync(Guid tenantId);

    /// <summary>Returns all plan definitions (used for seeding checks).</summary>
    Task<IReadOnlyList<SubscriptionPlanDefinition>> GetAllPlanDefinitionsAsync();

    /// <summary>Persists a new plan definition (only used during seeding).</summary>
    Task<SubscriptionPlanDefinition> CreatePlanDefinitionAsync(SubscriptionPlanDefinition plan);
}
