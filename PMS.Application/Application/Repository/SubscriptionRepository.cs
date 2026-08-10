using Microsoft.EntityFrameworkCore;
using PMS.Application.Application.Interface.Repository;
using PMS.Models.Enums;
using PMS.Models.Models;
using PMS.Repository.Data;

namespace PMS.Application.Application.Repository;

public class SubscriptionRepository : ISubscriptionRepository
{
    private readonly ApplicationDbContext _db;

    public SubscriptionRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    /// <inheritdoc/>
    public async Task<Subscription?> GetActiveByTenantAsync(Guid tenantId)
        => await _db.Subscriptions
            .IgnoreQueryFilters() // bypass global tenant filter — we filter manually
            .Where(s => s.TenantId == tenantId && s.Status == SubscriptionStatus.Active)
            .OrderByDescending(s => s.StartDate)
            .FirstOrDefaultAsync();

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Subscription>> GetAllByTenantAsync(Guid tenantId)
        => await _db.Subscriptions
            .IgnoreQueryFilters()
            .Where(s => s.TenantId == tenantId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();

    /// <inheritdoc/>
    public async Task<Subscription> CreateAsync(Subscription subscription)
    {
        _db.Subscriptions.Add(subscription);
        await _db.SaveChangesAsync();
        return subscription;
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(Subscription subscription)
    {
        _db.Subscriptions.Update(subscription);
        await _db.SaveChangesAsync();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Subscription>> GetExpiredActiveSubscriptionsAsync()
        => await _db.Subscriptions
            .IgnoreQueryFilters()
            .Where(s => s.Status == SubscriptionStatus.Active
                        && s.EndDate != null
                        && s.EndDate < DateTime.UtcNow)
            .ToListAsync();

    /// <inheritdoc/>
    public async Task<SubscriptionPlanDefinition?> GetPlanDefinitionAsync(PlanType planType)
        => await _db.SubscriptionPlanDefinitions
            .FirstOrDefaultAsync(p => p.PlanType == planType);

    /// <inheritdoc/>
    public async Task<SubscriptionPlanDefinition?> GetPlanDefinitionByTenantAsync(Guid tenantId)
    {
        var subscription = await GetActiveByTenantAsync(tenantId);
        if (subscription is null) return null;
        return await GetPlanDefinitionAsync(subscription.PlanType);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<SubscriptionPlanDefinition>> GetAllPlanDefinitionsAsync()
        => await _db.SubscriptionPlanDefinitions.ToListAsync();

    /// <inheritdoc/>
    public async Task<SubscriptionPlanDefinition> CreatePlanDefinitionAsync(SubscriptionPlanDefinition plan)
    {
        _db.SubscriptionPlanDefinitions.Add(plan);
        await _db.SaveChangesAsync();
        return plan;
    }
}
