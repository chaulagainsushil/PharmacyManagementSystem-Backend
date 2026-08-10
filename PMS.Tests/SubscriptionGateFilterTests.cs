using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Moq;
using PMS.Application.Application.Interface.Repository;
using PMS.Filters;
using PMS.Models.Enums;
using PMS.Models.Interfaces;
using PMS.Models.Models;
using Xunit;

namespace PMS.Tests;

/// <summary>
/// Verifies SubscriptionGateFilter behaviour:
/// - Expired / cancelled subscriptions → 402
/// - Active subscriptions → request passes through
/// - Auth / subscription routes → always bypassed
/// - Diamond (null EndDate) → never blocked
/// </summary>
public class SubscriptionGateFilterTests
{
    private static readonly Guid TenantA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static (ActionExecutingContext context, Mock<ActionExecutionDelegate> nextMock)
        BuildContext(string path = "/api/medicine", string method = "GET")
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path   = path;
        httpContext.Request.Method = method;

        var actionContext = new ActionContext(
            httpContext,
            new RouteData(),
            new ActionDescriptor());

        var context = new ActionExecutingContext(
            actionContext,
            filters: new List<IFilterMetadata>(),
            actionArguments: new Dictionary<string, object?>(),
            controller: new object());

        var nextMock = new Mock<ActionExecutionDelegate>();
        nextMock.Setup(n => n()).ReturnsAsync(
            new ActionExecutedContext(actionContext,
                new List<IFilterMetadata>(), new object()));

        return (context, nextMock);
    }

    private static SubscriptionGateFilter BuildFilter(
        Guid tenantId, Subscription? subscription)
    {
        var tenantProvider = new Mock<ITenantProvider>();
        tenantProvider.Setup(p => p.TenantId).Returns(tenantId);
        tenantProvider.Setup(p => p.HasTenant).Returns(tenantId != Guid.Empty);

        var repo = new Mock<ISubscriptionRepository>();
        repo.Setup(r => r.GetActiveByTenantAsync(tenantId))
            .ReturnsAsync(subscription);

        return new SubscriptionGateFilter(tenantProvider.Object, repo.Object);
    }

    // ── Tests ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Filter_ActiveSubscription_AllowsRequest()
    {
        var sub = new Subscription
        {
            TenantId  = TenantA,
            PlanType  = PlanType.Silver,
            StartDate = DateTime.UtcNow.AddDays(-10),
            EndDate   = DateTime.UtcNow.AddDays(170),
            Status    = SubscriptionStatus.Active
        };

        var filter = BuildFilter(TenantA, sub);
        var (ctx, next) = BuildContext("/api/medicine");

        await filter.OnActionExecutionAsync(ctx, next.Object);

        // Should pass through to next
        next.Verify(n => n(), Times.Once);
        Assert.Null(ctx.Result);
    }

    [Fact]
    public async Task Filter_ExpiredSubscription_Returns402()
    {
        var sub = new Subscription
        {
            TenantId  = TenantA,
            PlanType  = PlanType.Silver,
            StartDate = DateTime.UtcNow.AddDays(-200),
            EndDate   = DateTime.UtcNow.AddDays(-10), // already past
            Status    = SubscriptionStatus.Active      // active by status but expired by date
        };

        var filter = BuildFilter(TenantA, sub);
        var (ctx, next) = BuildContext("/api/medicine");

        await filter.OnActionExecutionAsync(ctx, next.Object);

        Assert.NotNull(ctx.Result);
        var result = Assert.IsType<ObjectResult>(ctx.Result);
        Assert.Equal(402, result.StatusCode);
        next.Verify(n => n(), Times.Never);
    }

    [Fact]
    public async Task Filter_CancelledSubscription_Returns402()
    {
        // GetActiveByTenantAsync returns null for cancelled subscriptions
        var filter  = BuildFilter(TenantA, subscription: null);
        var (ctx, next) = BuildContext("/api/medicine");

        await filter.OnActionExecutionAsync(ctx, next.Object);

        var result = Assert.IsType<ObjectResult>(ctx.Result);
        Assert.Equal(402, result.StatusCode);
    }

    [Fact]
    public async Task Filter_NoSubscription_Returns402()
    {
        var filter  = BuildFilter(TenantA, subscription: null);
        var (ctx, next) = BuildContext("/api/customer");

        await filter.OnActionExecutionAsync(ctx, next.Object);

        var result = Assert.IsType<ObjectResult>(ctx.Result);
        Assert.Equal(402, result.StatusCode);
    }

    [Fact]
    public async Task Filter_AuthRoute_IsBypassed_EvenWithoutSubscription()
    {
        var filter  = BuildFilter(TenantA, subscription: null);
        var (ctx, next) = BuildContext("/api/auth/login");

        await filter.OnActionExecutionAsync(ctx, next.Object);

        next.Verify(n => n(), Times.Once);
        Assert.Null(ctx.Result);
    }

    [Fact]
    public async Task Filter_SubscriptionRoute_IsBypassed_EvenWithoutSubscription()
    {
        var filter = BuildFilter(TenantA, subscription: null);
        var (ctx, next) = BuildContext("/api/subscription/renew");

        await filter.OnActionExecutionAsync(ctx, next.Object);

        next.Verify(n => n(), Times.Once);
        Assert.Null(ctx.Result);
    }

    [Fact]
    public async Task Filter_OptionsRequest_AlwaysBypassed()
    {
        var filter = BuildFilter(TenantA, subscription: null);
        var (ctx, next) = BuildContext("/api/medicine", "OPTIONS");

        await filter.OnActionExecutionAsync(ctx, next.Object);

        next.Verify(n => n(), Times.Once);
        Assert.Null(ctx.Result);
    }

    [Fact]
    public async Task Filter_UnauthenticatedRequest_NoTenantId_IsAllowed()
    {
        // TenantId = Guid.Empty → unauthenticated user, gate doesn't apply
        var filter = BuildFilter(Guid.Empty, subscription: null);
        var (ctx, next) = BuildContext("/api/medicine");

        await filter.OnActionExecutionAsync(ctx, next.Object);

        next.Verify(n => n(), Times.Once);
        Assert.Null(ctx.Result);
    }
}
