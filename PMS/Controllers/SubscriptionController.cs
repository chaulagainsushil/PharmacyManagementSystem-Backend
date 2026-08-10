using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PMS.Application.Application.Interface.Service;
using PMS.Common.DTOs.Subscription;
using PMS.Models.Interfaces;

namespace PMS.Controllers;

/// <summary>
/// Manages subscription lifecycle for the current tenant.
/// All endpoints are exempt from the SubscriptionGateFilter so tenants can
/// always renew/upgrade even after their subscription has lapsed.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SubscriptionController : ControllerBase
{
    private readonly ISubscriptionService _subscriptionService;
    private readonly ITenantProvider      _tenantProvider;

    public SubscriptionController(
        ISubscriptionService subscriptionService,
        ITenantProvider      tenantProvider)
    {
        _subscriptionService = subscriptionService;
        _tenantProvider      = tenantProvider;
    }

    /// <summary>
    /// Returns the current Active subscription for the authenticated tenant.
    /// Returns 404 if no active subscription exists.
    /// </summary>
    [HttpGet("mine")]
    [ProducesResponseType(typeof(SubscriptionResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMine()
    {
        var tenantId = _tenantProvider.TenantId;
        if (tenantId == Guid.Empty)
            return Unauthorized(new { message = "No tenant context found in the current token." });

        var result = await _subscriptionService.GetCurrentAsync(tenantId);
        return result is null
            ? NotFound(new { message = "No active subscription found for this tenant." })
            : Ok(result);
    }

    /// <summary>
    /// Renews the current subscription using the same plan type.
    /// Cancels the existing Active subscription and creates a new one starting today.
    /// </summary>
    [HttpPost("renew")]
    [ProducesResponseType(typeof(SubscriptionResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Renew([FromBody] RenewSubscriptionRequestDto dto)
    {
        var tenantId = _tenantProvider.TenantId;
        if (tenantId == Guid.Empty)
            return Unauthorized(new { message = "No tenant context found in the current token." });

        var result = await _subscriptionService.RenewAsync(tenantId, dto.PaymentReference);
        return Ok(result);
    }

    /// <summary>
    /// Upgrades (or downgrades) the subscription to the specified plan.
    /// Cancels the existing Active subscription and creates a new one for the new plan.
    /// EndDate is recalculated from SubscriptionPlanDefinition.DurationInDays;
    /// Diamond plans produce a null EndDate (lifetime, never expires).
    /// </summary>
    [HttpPost("upgrade")]
    [ProducesResponseType(typeof(SubscriptionResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Upgrade([FromBody] UpgradeSubscriptionRequestDto dto)
    {
        var tenantId = _tenantProvider.TenantId;
        if (tenantId == Guid.Empty)
            return Unauthorized(new { message = "No tenant context found in the current token." });

        try
        {
            var result = await _subscriptionService.UpgradeAsync(
                tenantId, dto.NewPlan, dto.PaymentReference);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
