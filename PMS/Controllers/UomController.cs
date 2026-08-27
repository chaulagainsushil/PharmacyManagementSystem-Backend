using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PMS.Application.Application.Interface.Service;
using PMS.Common;
using PMS.Common.DTOs.Uom;

namespace PMS.Controllers;

/// <summary>
/// Manages the UnitOfMeasure master list.
/// All authenticated users may read; write operations require PharmacyAdmin or SystemAdmin.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UomController : ControllerBase
{
    private readonly IUnitOfMeasureService _service;

    public UomController(IUnitOfMeasureService service) => _service = service;

    /// <summary>Get all active units of measure.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<UomDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] bool includeInactive = false)
        => Ok(await _service.GetAllAsync(includeInactive));

    /// <summary>Get a unit of measure by ID.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(UomDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var data = await _service.GetByIdAsync(id);
        return data is null ? NotFound(new { message = $"UoM with ID {id} not found." }) : Ok(data);
    }

    /// <summary>Create a new unit of measure.</summary>
    [HttpPost]
    [Authorize(Roles = $"{AppRoles.SystemAdmin},{AppRoles.PharmacyAdmin}")]
    [ProducesResponseType(typeof(UomDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateUomDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var (success, message, data) = await _service.CreateAsync(dto);
        if (!success) return BadRequest(new { message });

        return CreatedAtAction(nameof(GetById), new { id = data!.UnitOfMeasureId }, data);
    }

    /// <summary>Update a unit of measure.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = $"{AppRoles.SystemAdmin},{AppRoles.PharmacyAdmin}")]
    [ProducesResponseType(typeof(UomDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateUomDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var (success, message, isConflict, data) = await _service.UpdateAsync(id, dto);
        if (!success)
        {
            if (isConflict) return Conflict(new { message });
            return message.Contains("not found") ? NotFound(new { message }) : BadRequest(new { message });
        }

        return Ok(data);
    }

    /// <summary>Delete a unit of measure.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = $"{AppRoles.SystemAdmin},{AppRoles.PharmacyAdmin}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var (success, message) = await _service.DeleteAsync(id);
        if (!success)
            return message.Contains("not found") ? NotFound(new { message }) : BadRequest(new { message });

        return Ok(new { message });
    }
}
