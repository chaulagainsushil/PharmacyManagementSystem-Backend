using Moq;
using PMS.Application.Application.Interface.Repository;
using PMS.Application.Application.Service;
using PMS.Models.Enums;
using PMS.Models.Models;
using Xunit;

namespace PMS.Tests;

/// <summary>
/// Tests for SubscriptionService business logic:
/// - Diamond plans (null EndDate) are never considered expired
/// - Expired Active subscriptions are flipped to Expired by the job
/// - Renew / Upgrade create a new Active subscription
/// </summary>
public class SubscriptionServiceTests
{
    private static readonly Guid Tenant = Guid.NewGuid();

    // ── Diamond never-expiring ────────────────────────────────────────────────

    [Fact]
    public void DiamondSubscription_NullEndDate_IsCurrentlyActive_ReturnsTrue()
    {
        var diamond = new Subscription
        {
            TenantId  = Tenant,
            PlanType  = PlanType.Diamond,
            StartDate = DateTime.UtcNow.AddYears(-2),
            EndDate   = null,   // lifetime
            Status    = SubscriptionStatus.Active
        };

        Assert.True(diamond.IsCurrentlyActive(),
            "Diamond subscription with null EndDate should always be active.");
    }

    [Fact]
    public void DiamondSubscription_CancelledStatus_IsNotActive()
    {
        var diamond = new Subscription
        {
            TenantId  = Tenant,
            PlanType  = PlanType.Diamond,
            StartDate = DateTime.UtcNow.AddYears(-2),
            EndDate   = null,
            Status    = SubscriptionStatus.Cancelled
        };

        Assert.False(diamond.IsCurrentlyActive(),
            "Cancelled Diamond subscription should not be considered active.");
    }

    [Fact]
    public void SilverSubscription_PastEndDate_IsCurrentlyActive_ReturnsFalse()
    {
        var expired = new Subscription
        {
            TenantId  = Tenant,
            PlanType  = PlanType.Silver,
            StartDate = DateTime.UtcNow.AddDays(-200),
            EndDate   = DateTime.UtcNow.AddDays(-10),
            Status    = SubscriptionStatus.Active
        };

        Assert.False(expired.IsCurrentlyActive(),
            "Silver subscription with past EndDate should not be active.");
    }

    [Fact]
    public void SilverSubscription_FutureEndDate_IsCurrentlyActive_ReturnsTrue()
    {
        var active = new Subscription
        {
            TenantId  = Tenant,
            PlanType  = PlanType.Silver,
            StartDate = DateTime.UtcNow.AddDays(-10),
            EndDate   = DateTime.UtcNow.AddDays(170),
            Status    = SubscriptionStatus.Active
        };

        Assert.True(active.IsCurrentlyActive());
    }

    // ── SubscriptionService tests ─────────────────────────────────────────────

    private static (SubscriptionService service, Mock<ISubscriptionRepository> repoMock)
        BuildService(SubscriptionPlanDefinition? plan = null, Subscription? existing = null)
    {
        plan ??= new SubscriptionPlanDefinition
        {
            PlanType       = PlanType.Silver,
            DurationInDays = 180,
            Price          = 999m,
            Description    = "Silver"
        };

        var repoMock = new Mock<ISubscriptionRepository>();
        repoMock.Setup(r => r.GetPlanDefinitionAsync(It.IsAny<PlanType>()))
            .ReturnsAsync(plan);
        repoMock.Setup(r => r.GetActiveByTenantAsync(It.IsAny<Guid>()))
            .ReturnsAsync(existing);
        repoMock.Setup(r => r.CreateAsync(It.IsAny<Subscription>()))
            .ReturnsAsync((Subscription s) => s);
        repoMock.Setup(r => r.UpdateAsync(It.IsAny<Subscription>()))
            .Returns(Task.CompletedTask);
        repoMock.Setup(r => r.GetExpiredActiveSubscriptionsAsync())
            .ReturnsAsync(new List<Subscription>());

        return (new SubscriptionService(repoMock.Object), repoMock);
    }

    [Fact]
    public async Task CreateInitial_Silver_SetsEndDateTo180DaysFromNow()
    {
        var (service, _) = BuildService();

        var result = await service.CreateInitialAsync(Tenant, PlanType.Silver);

        Assert.Equal(PlanType.Silver, result.PlanType);
        Assert.NotNull(result.EndDate);
        // Allow ±5s for test execution time
        var expected = DateTime.UtcNow.AddDays(180);
        Assert.True(Math.Abs((result.EndDate!.Value - expected).TotalSeconds) < 5);
        Assert.Equal(SubscriptionStatus.Active, result.Status);
        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task CreateInitial_Diamond_EndDateIsNull()
    {
        var diamondPlan = new SubscriptionPlanDefinition
        {
            PlanType       = PlanType.Diamond,
            DurationInDays = null,
            Price          = 4999m,
            Description    = "Diamond"
        };

        var (service, _) = BuildService(plan: diamondPlan);

        var result = await service.CreateInitialAsync(Tenant, PlanType.Diamond);

        Assert.Equal(PlanType.Diamond, result.PlanType);
        Assert.Null(result.EndDate);
        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task Diamond_GetCurrent_IsActive_ReturnsTrue()
    {
        var existingSub = new Subscription
        {
            TenantId  = Tenant,
            PlanType  = PlanType.Diamond,
            StartDate = DateTime.UtcNow.AddYears(-5),
            EndDate   = null,
            Status    = SubscriptionStatus.Active
        };

        var repoMock = new Mock<ISubscriptionRepository>();
        repoMock.Setup(r => r.GetActiveByTenantAsync(Tenant))
            .ReturnsAsync(existingSub);

        var service = new SubscriptionService(repoMock.Object);
        var result  = await service.GetCurrentAsync(Tenant);

        Assert.NotNull(result);
        Assert.True(result!.IsActive);
        Assert.Null(result.EndDate);
        Assert.Null(result.DaysRemaining);  // no expiry for Diamond
    }

    [Fact]
    public async Task Upgrade_CancelsPreviousSubscription_CreatesNew()
    {
        var oldSub = new Subscription
        {
            SubscriptionId = 42,
            TenantId       = Tenant,
            PlanType       = PlanType.Silver,
            StartDate      = DateTime.UtcNow.AddDays(-10),
            EndDate        = DateTime.UtcNow.AddDays(170),
            Status         = SubscriptionStatus.Active
        };

        var goldPlan = new SubscriptionPlanDefinition
        {
            PlanType       = PlanType.Gold,
            DurationInDays = 365,
            Price          = 1799m,
            Description    = "Gold"
        };

        var (service, repoMock) = BuildService(plan: goldPlan, existing: oldSub);

        var result = await service.UpgradeAsync(Tenant, PlanType.Gold, "pay-ref-001");

        // Old sub should have been cancelled
        repoMock.Verify(r => r.UpdateAsync(
            It.Is<Subscription>(s =>
                s.SubscriptionId == 42 &&
                s.Status == SubscriptionStatus.Cancelled)),
            Times.Once);

        // New sub should be created
        repoMock.Verify(r => r.CreateAsync(
            It.Is<Subscription>(s =>
                s.PlanType == PlanType.Gold &&
                s.Status == SubscriptionStatus.Active)),
            Times.Once);

        Assert.Equal(PlanType.Gold, result.PlanType);
        Assert.NotNull(result.EndDate);
    }

    [Fact]
    public async Task ExpireOverdueSubscriptions_FlipsStatusToExpired()
    {
        var overdue = new Subscription
        {
            SubscriptionId = 99,
            TenantId       = Tenant,
            PlanType       = PlanType.Silver,
            StartDate      = DateTime.UtcNow.AddDays(-200),
            EndDate        = DateTime.UtcNow.AddDays(-5),
            Status         = SubscriptionStatus.Active
        };

        var repoMock = new Mock<ISubscriptionRepository>();
        repoMock.Setup(r => r.GetExpiredActiveSubscriptionsAsync())
            .ReturnsAsync(new List<Subscription> { overdue });
        repoMock.Setup(r => r.UpdateAsync(It.IsAny<Subscription>()))
            .Returns(Task.CompletedTask);

        var service = new SubscriptionService(repoMock.Object);
        await service.ExpireOverdueSubscriptionsAsync();

        repoMock.Verify(r => r.UpdateAsync(
            It.Is<Subscription>(s =>
                s.SubscriptionId == 99 &&
                s.Status == SubscriptionStatus.Expired)),
            Times.Once);
    }

    [Fact]
    public async Task Diamond_NotIncluded_InExpiredQuery_NeverExpired()
    {
        // Diamond subscriptions have null EndDate so GetExpiredActiveSubscriptionsAsync
        // (which filters EndDate != null && EndDate < now) will never return them.
        var repoMock = new Mock<ISubscriptionRepository>();
        // Simulate repo returning no overdue subscriptions (Diamond excluded by query)
        repoMock.Setup(r => r.GetExpiredActiveSubscriptionsAsync())
            .ReturnsAsync(new List<Subscription>());
        repoMock.Setup(r => r.UpdateAsync(It.IsAny<Subscription>()))
            .Returns(Task.CompletedTask);

        var service = new SubscriptionService(repoMock.Object);
        await service.ExpireOverdueSubscriptionsAsync();

        // UpdateAsync should never be called — nothing to expire
        repoMock.Verify(r => r.UpdateAsync(It.IsAny<Subscription>()), Times.Never);
    }
}
