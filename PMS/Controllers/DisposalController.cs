using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PMS.Application.Application.Interface.Service;
using PMS.Common;
using PMS.Common.DTOs.Disposal;

namespace PMS.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DisposalController : ControllerBase
{
    private readonly IDisposalService _service;

    public DisposalController(IDisposalService service) => _service = service;

    /// <summary>Get all disposal records, newest first.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<DisposalResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
        => Ok(await _service.GetAllAsync());

    /// <summary>Get a single disposal record by ID.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(DisposalResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var data = await _service.GetByIdAsync(id);
        return data is null
            ? NotFound(new { message = $"Disposal with ID {id} not found." })
            : Ok(data);
    }

    /// <summary>
    /// Record a new expired-medicine disposal.
    /// Each item specifies the batch, quantity (in tablets), and reason.
    /// Stock is deducted transactionally.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = $"{AppRoles.SystemAdmin},{AppRoles.PharmacyAdmin},{AppRoles.Pharmacist}")]
    [ProducesResponseType(typeof(DisposalResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create([FromBody] CreateDisposalRequestDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var (success, message, data) = await _service.CreateDisposalAsync(dto);

        if (!success)
        {
            return message.Contains("not found", StringComparison.OrdinalIgnoreCase)
                ? NotFound(new { message })
                : BadRequest(new { message });
        }

        return StatusCode(StatusCodes.Status201Created, data);
    }
}
