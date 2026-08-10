using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PMS.Application.Application.Interface.Service;
using PMS.Common;
using PMS.Common.DTOs.Manufacturer;

namespace PMS.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ManufacturerController : ControllerBase
{
    private readonly IManufacturerService _service;

    public ManufacturerController(IManufacturerService service) => _service = service;

    /// <summary>Get all manufacturers.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ManufacturerListItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
        => Ok(await _service.GetAllAsync());

    /// <summary>Get a manufacturer by ID.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ManufacturerResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var data = await _service.GetByIdAsync(id);
        return data is null ? NotFound(new { message = $"Manufacturer with ID {id} not found." }) : Ok(data);
    }

    /// <summary>Create a new manufacturer.</summary>
    [HttpPost]
    [Authorize(Roles = $"{AppRoles.SystemAdmin},{AppRoles.PharmacyAdmin}")]
    [ProducesResponseType(typeof(ManufacturerResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateManufacturerDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var (success, message, data) = await _service.CreateAsync(dto);
        if (!success) return BadRequest(new { message });

        return CreatedAtAction(nameof(GetById), new { id = data!.ManufacturerId }, data);
    }

    /// <summary>Update an existing manufacturer.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = $"{AppRoles.SystemAdmin},{AppRoles.PharmacyAdmin}")]
    [ProducesResponseType(typeof(ManufacturerResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateManufacturerDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var (success, message, isConflict, data) = await _service.UpdateAsync(id, dto);
        if (!success)
        {
            if (isConflict) return Conflict(new { message });
            return message.Contains("not found")
                ? NotFound(new { message })
                : BadRequest(new { message });
        }

        return Ok(data);
    }

    /// <summary>Delete a manufacturer by ID.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = $"{AppRoles.SystemAdmin},{AppRoles.PharmacyAdmin}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var (success, message) = await _service.DeleteAsync(id);
        if (!success)
            return message.Contains("not found")
                ? NotFound(new { message })
                : BadRequest(new { message });

        return Ok(new { message });
    }
}
