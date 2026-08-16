using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PMS.Application.Application.Interface.Service;
using PMS.Common;
using PMS.Common.DTOs.Uom;

namespace PMS.Controllers;

/// <summary>
/// Manages units attached to a specific medicine.
/// GET (POS/purchase query) is open to all authenticated users.
/// Write operations require at least Pharmacist role.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MedicineUnitController : ControllerBase
{
    private readonly IMedicineUnitService _service;

    public MedicineUnitController(IMedicineUnitService service) => _service = service;

    // ── Queries ────────────────────────────────────────────────────────────────

    /// <summary>
    /// List all units for a medicine (management view — includes inactive and non-sellable).
    /// </summary>
    [HttpGet("by-medicine/{medicineId:int}")]
    [ProducesResponseType(typeof(IReadOnlyList<MedicineUnitDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByMedicine(int medicineId)
        => Ok(await _service.GetByMedicineIdAsync(medicineId));

    /// <summary>
    /// Get active, sellable units (CanSell = true) for the POS unit-selector dropdown.
    /// Default unit is returned first.
    /// </summary>
    [HttpGet("pos/{medicineId:int}")]
    [ProducesResponseType(typeof(IReadOnlyList<MedicineUnitForPosDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetForPos(int medicineId)
        => Ok(await _service.GetForPosAsync(medicineId));

    /// <summary>
    /// Get active, purchasable units (CanPurchase = true) for the batch-receive unit-selector.
    /// </summary>
    [HttpGet("purchase/{medicineId:int}")]
    [ProducesResponseType(typeof(IReadOnlyList<MedicineUnitForPurchaseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetForPurchase(int medicineId)
        => Ok(await _service.GetForPurchaseAsync(medicineId));

    // ── Commands ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Attach a unit of measure to a medicine with its conversion factor, pricing, and flags.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = $"{AppRoles.SystemAdmin},{AppRoles.PharmacyAdmin},{AppRoles.Pharmacist}")]
    [ProducesResponseType(typeof(MedicineUnitDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AddUnit([FromBody] AddMedicineUnitCommand cmd)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var (success, message, data) = await _service.AddUnitAsync(cmd);
        if (!success) return BadRequest(new { message });

        return StatusCode(StatusCodes.Status201Created, data);
    }

    /// <summary>
    /// Full update of a medicine unit — conversion factor, CanPurchase, CanSell,
    /// CostPrice, UnitPrice (selling), Mrp, and IsActive.
    /// The base unit's ConversionFactorToBase cannot be changed (always 1).
    /// </summary>
    [HttpPut("{medicineUnitId:int}")]
    [Authorize(Roles = $"{AppRoles.SystemAdmin},{AppRoles.PharmacyAdmin},{AppRoles.Pharmacist}")]
    [ProducesResponseType(typeof(MedicineUnitDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(int medicineUnitId, [FromBody] UpdateMedicineUnitCommand cmd)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var (success, message, isConflict, data) = await _service.UpdateAsync(medicineUnitId, cmd);
        if (!success)
        {
            if (isConflict) return Conflict(new { message });
            return message.Contains("not found") ? NotFound(new { message }) : BadRequest(new { message });
        }

        return Ok(data);
    }

    /// <summary>Set the default POS unit for a medicine.</summary>
    [HttpPost("set-default")]
    [Authorize(Roles = $"{AppRoles.SystemAdmin},{AppRoles.PharmacyAdmin},{AppRoles.Pharmacist}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SetDefault([FromBody] SetDefaultUnitCommand cmd)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var (success, message, isConflict) = await _service.SetDefaultUnitAsync(cmd);
        if (!success)
        {
            if (isConflict) return Conflict(new { message });
            return message.Contains("not found") ? NotFound(new { message }) : BadRequest(new { message });
        }

        return Ok(new { message });
    }

    /// <summary>
    /// Quick-update of just the selling price for a medicine unit.
    /// For updating all fields including CostPrice and Mrp, use PUT /{medicineUnitId} instead.
    /// </summary>
    [HttpPatch("{medicineUnitId:int}/price")]
    [Authorize(Roles = $"{AppRoles.SystemAdmin},{AppRoles.PharmacyAdmin},{AppRoles.Pharmacist}")]
    [ProducesResponseType(typeof(MedicineUnitDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdatePrice(int medicineUnitId, [FromBody] UpdateMedicineUnitPriceCommand cmd)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var (success, message, isConflict, data) = await _service.UpdatePriceAsync(medicineUnitId, cmd);
        if (!success)
        {
            if (isConflict) return Conflict(new { message });
            return message.Contains("not found") ? NotFound(new { message }) : BadRequest(new { message });
        }

        return Ok(data);
    }

    /// <summary>
    /// Deactivate (soft-delete) a unit from a medicine.
    /// Base units cannot be deactivated.
    /// </summary>
    [HttpDelete("{medicineUnitId:int}")]
    [Authorize(Roles = $"{AppRoles.SystemAdmin},{AppRoles.PharmacyAdmin}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactivate(int medicineUnitId)
    {
        var (success, message) = await _service.DeactivateAsync(medicineUnitId);
        if (!success)
            return message.Contains("not found") ? NotFound(new { message }) : BadRequest(new { message });

        return Ok(new { message });
    }
}
