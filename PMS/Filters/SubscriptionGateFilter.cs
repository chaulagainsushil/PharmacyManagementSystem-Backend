using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using PMS.Application.Application.Interface.Repository;
using PMS.Models.Interfaces;

namespace PMS.Filters;

/// <summary>
/// Action filter that gates every tenant-scoped request behind a valid Active subscription.
/// Short-circuits with HTTP 402 Payment Required when:
/// <list type="bullet">
///   <item>No Active subscription exists for the tenant.</item>
///   <item>The subscription's <c>Status</c> is <c>Cancelled</c> or <c>Expired</c>.</item>
///   <item>The subscription's <c>EndDate</c> is in the past (skipped when <c>EndDate</c> is null — Diamond plans).</item>
/// </list>
/// Bypassed for:
/// <list type="bullet">
///   <item>Unauthenticated requests (no tenantId claim).</item>
///   <item>OPTIONS and health-check requests.</item>
///   <item><c>/api/auth/*</c> — login / signup routes.</item>
///   <item><c>/api/subscription/*</c> — renew / upgrade routes.</item>
/// </list>
/// </summary>
public class SubscriptionGateFilter : IAsyncActionFilter
{
    private readonly ITenantProvider           _tenantProvider;
    private readonly ISubscriptionRepository   _subscriptionRepo;

    // Routes that are exempt from the subscription gate
    private static readonly string[] ExemptPrefixes =
    [
        "/api/auth",
        "/api/subscription"
    ];

    public SubscriptionGateFilter(
        ITenantProvider         tenantProvider,
        ISubscriptionRepository subscriptionRepo)
    {
        _tenantProvider  = tenantProvider;
        _subscriptionRepo = subscriptionRepo;
    }

    public async Task OnActionExecutionAsync(
        ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var request = context.HttpContext.Request;

        // Always allow OPTIONS (CORS pre-flight)
        if (HttpMethods.IsOptions(request.Method))
        {
            await next();
            return;
        }

        // No tenantId claim → unauthenticated, not our concern here
        if (!_tenantProvider.HasTenant)
        {
            await next();
            return;
        }

        // Check exempt path prefixes (case-insensitive)
        var path = request.Path.Value ?? string.Empty;
        if (ExemptPrefixes.Any(prefix =>
                path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
        {
            await next();
            return;
        }

        // Look up the Active subscription for this tenant
        var subscription = await _subscriptionRepo
            .GetActiveByTenantAsync(_tenantProvider.TenantId);

        // No active subscription — block access
        if (subscription is null || !subscription.IsCurrentlyActive())
        {
            context.Result = new ObjectResult(new
            {
                statusCode = 402,
                message    = "Subscription expired or inactive. Please renew your subscription to continue using PMS.",
                renewUrl   = "/api/subscription/renew"
            })
            {
                StatusCode = StatusCodes.Status402PaymentRequired
            };
            return;
        }

        await next();
    }
}
