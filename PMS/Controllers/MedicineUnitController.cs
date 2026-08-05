using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PMS.Application.Application.Interface.Service;
using PMS.Common;
using PMS.Common.DTOs.Uom;

namespace PMS.Controllers;

/// <summary>
/// Manages units attached to a specific medicine.
/// GET (POS query) is open to all authenticated users.
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
    /// List all units for a medicine (management view — includes inactive).
    /// </summary>
    [HttpGet("by-medicine/{medicineId:int}")]
    [ProducesResponseType(typeof(IReadOnlyList<MedicineUnitDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByMedicine(int medicineId)
        => Ok(await _service.GetByMedicineIdAsync(medicineId));

    /// <summary>
    /// Get active units for the POS unit-selector dropdown, default unit first.
    /// </summary>
    [HttpGet("pos/{medicineId:int}")]
    [ProducesResponseType(typeof(IReadOnlyList<MedicineUnitForPosDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetForPos(int medicineId)
        => Ok(await _service.GetForPosAsync(medicineId));

    // ── Commands ───────────────────────────────────────────────────────────────

    /// <summary>Attach a unit of measure to a medicine with its conversion factor and price.</summary>
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

    /// <summary>Set the default POS unit for a medicine.</summary>
    [HttpPost("set-default")]
    [Authorize(Roles = $"{AppRoles.SystemAdmin},{AppRoles.PharmacyAdmin},{AppRoles.Pharmacist}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetDefault([FromBody] SetDefaultUnitCommand cmd)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var (success, message) = await _service.SetDefaultUnitAsync(cmd);
        if (!success)
            return message.Contains("not found") ? NotFound(new { message }) : BadRequest(new { message });

        return Ok(new { message });
    }

    /// <summary>Update the price for a medicine unit.</summary>
    [HttpPatch("{medicineUnitId:int}/price")]
    [Authorize(Roles = $"{AppRoles.SystemAdmin},{AppRoles.PharmacyAdmin},{AppRoles.Pharmacist}")]
    [ProducesResponseType(typeof(MedicineUnitDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePrice(int medicineUnitId, [FromBody] UpdateMedicineUnitPriceCommand cmd)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var (success, message, data) = await _service.UpdatePriceAsync(medicineUnitId, cmd);
        if (!success)
            return message.Contains("not found") ? NotFound(new { message }) : BadRequest(new { message });

        return Ok(data);
    }

    /// <summary>Deactivate (soft-delete) a unit from a medicine. Base units cannot be deactivated.</summary>
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
