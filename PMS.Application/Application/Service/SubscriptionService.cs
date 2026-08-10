using PMS.Application.Application.Interface.Repository;
using PMS.Application.Application.Interface.Service;
using PMS.Common.DTOs.Subscription;
using PMS.Models.Enums;
using PMS.Models.Models;

namespace PMS.Application.Application.Service;

public class SubscriptionService : ISubscriptionService
{
    private readonly ISubscriptionRepository _repo;

    public SubscriptionService(ISubscriptionRepository repo)
    {
        _repo = repo;
    }

    /// <inheritdoc/>
    public async Task<SubscriptionResponseDto?> GetCurrentAsync(Guid tenantId)
    {
        var subscription = await _repo.GetActiveByTenantAsync(tenantId);
        return subscription is null ? null : MapToDto(subscription);
    }

    /// <inheritdoc/>
    public async Task<SubscriptionResponseDto> RenewAsync(Guid tenantId, string? paymentReference)
    {
        // Determine current plan type for renewal
        var existing = await _repo.GetActiveByTenantAsync(tenantId);
        var planType  = existing?.PlanType ?? PlanType.Silver;

        return await UpgradeAsync(tenantId, planType, paymentReference);
    }

    /// <inheritdoc/>
    public async Task<SubscriptionResponseDto> UpgradeAsync(
        Guid tenantId, PlanType newPlan, string? paymentReference)
    {
        // Cancel any existing active subscription
        var existing = await _repo.GetActiveByTenantAsync(tenantId);
        if (existing is not null)
        {
            existing.Status    = SubscriptionStatus.Cancelled;
            existing.UpdatedAt = DateTime.UtcNow;
            await _repo.UpdateAsync(existing);
        }

        return await CreateSubscriptionAsync(tenantId, newPlan, paymentReference);
    }

    /// <inheritdoc/>
    public async Task<SubscriptionResponseDto> CreateInitialAsync(
        Guid tenantId, PlanType planType)
        => await CreateSubscriptionAsync(tenantId, planType, paymentReference: null);

    /// <inheritdoc/>
    public async Task ExpireOverdueSubscriptionsAsync()
    {
        var overdueList = await _repo.GetExpiredActiveSubscriptionsAsync();

        foreach (var sub in overdueList)
        {
            sub.Status    = SubscriptionStatus.Expired;
            sub.UpdatedAt = DateTime.UtcNow;
            await _repo.UpdateAsync(sub);
        }
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private async Task<SubscriptionResponseDto> CreateSubscriptionAsync(
        Guid tenantId, PlanType planType, string? paymentReference)
    {
        var plan = await _repo.GetPlanDefinitionAsync(planType)
            ?? throw new InvalidOperationException(
                $"Plan definition for '{planType}' not found. Ensure plans are seeded on startup.");

        var startDate = DateTime.UtcNow;
        var endDate   = plan.DurationInDays.HasValue
            ? startDate.AddDays(plan.DurationInDays.Value)
            : (DateTime?)null; // Diamond — no expiry

        var subscription = new Subscription
        {
            TenantId         = tenantId,
            PlanType         = planType,
            StartDate        = startDate,
            EndDate          = endDate,
            Status           = SubscriptionStatus.Active,
            PaymentReference = paymentReference,
            CreatedAt        = DateTime.UtcNow,
            UpdatedAt        = DateTime.UtcNow
        };

        await _repo.CreateAsync(subscription);
        return MapToDto(subscription);
    }

    private static SubscriptionResponseDto MapToDto(Subscription s)
    {
        var daysRemaining = s.EndDate.HasValue
            ? (int?)Math.Max(0, (int)(s.EndDate.Value - DateTime.UtcNow).TotalDays)
            : null;

        return new SubscriptionResponseDto
        {
            SubscriptionId   = s.SubscriptionId,
            TenantId         = s.TenantId,
            PlanType         = s.PlanType,
            StartDate        = s.StartDate,
            EndDate          = s.EndDate,
            Status           = s.Status,
            PaymentReference = s.PaymentReference,
            IsActive         = s.IsCurrentlyActive(),
            DaysRemaining    = daysRemaining
        };
    }
}
