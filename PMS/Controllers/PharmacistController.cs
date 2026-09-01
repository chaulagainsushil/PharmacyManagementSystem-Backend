using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PMS.Application.Application.Interface.Service;
using PMS.Common;
using PMS.Common.DTOs.Pharmacist;

namespace PMS.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = $"{AppRoles.PharmacyAdmin},{AppRoles.SystemAdmin}")]
public class PharmacistController : ControllerBase
{
    private readonly IPharmacistService _pharmacistService;

    public PharmacistController(IPharmacistService pharmacistService)
    {
        _pharmacistService = pharmacistService;
    }

    /// <summary>
    /// Returns all staff/pharmacists belonging to the currently authenticated pharmacy tenant.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<PharmacistListItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var list = await _pharmacistService.GetAllPharmacistsAsync();
        return Ok(list);
    }

    /// <summary>
    /// Gets a single pharmacist by their Identity User ID.
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(PharmacistListItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(string id)
    {
        var pharmacist = await _pharmacistService.GetByIdAsync(id);
        if (pharmacist is null) return NotFound(new { message = $"Pharmacist '{id}' not found." });
        return Ok(pharmacist);
    }

    /// <summary>
    /// Creates a new pharmacist or staff member in the current pharmacy.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(PharmacistListItemDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreatePharmacistRequestDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var (success, message, data) = await _pharmacistService.CreatePharmacistAsync(dto);
        if (!success)
            return BadRequest(new { message });

        return StatusCode(StatusCodes.Status201Created, data);
    }

    /// <summary>
    /// Updates a pharmacist's details (Name, Role, Password).
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(PharmacistListItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(string id, [FromBody] UpdatePharmacistRequestDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var (success, message, data) = await _pharmacistService.UpdatePharmacistAsync(id, dto);
        if (!success)
            return BadRequest(new { message });

        return Ok(data);
    }

    /// <summary>
    /// Deletes a pharmacist account from this pharmacy.
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Delete(string id)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var (success, message) = await _pharmacistService.DeletePharmacistAsync(id, currentUserId);
        if (!success)
            return BadRequest(new { message });

        return Ok(new { message });
    }
}
