using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PMS.Application.Application.Interface.Service;
using PMS.Common.DTOs.Medicine;

namespace PMS.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MedicineController : ControllerBase
{
    private readonly IMedicineService _service;

    public MedicineController(IMedicineService service)
    {
        _service = service;
    }

    /// <summary>Get all medicines.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<MedicineListItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var data = await _service.GetAllAsync();
        return Ok(data);
    }

    /// <summary>Get a medicine by ID.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(MedicineResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var data = await _service.GetByIdAsync(id);
        if (data is null) return NotFound(new { message = $"Medicine with ID {id} not found." });
        return Ok(data);
    }

    /// <summary>Create a new medicine.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(MedicineResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateMedicineDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var (success, message, data) = await _service.CreateAsync(dto);
        if (!success) return BadRequest(new { message });

        return CreatedAtAction(nameof(GetById), new { id = data!.MedicineId }, data);
    }

    /// <summary>
    /// Bulk-create medicines. Each row is validated independently;
    /// valid rows are inserted in a single DB round-trip.
    /// Returns a per-item result so the caller knows which rows succeeded and which failed.
    /// </summary>
    [HttpPost("bulk")]
    [ProducesResponseType(typeof(BulkCreateMedicineResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateBulk([FromBody] List<CreateMedicineDto> dtos)
    {
        if (dtos == null || dtos.Count == 0)
            return BadRequest(new { message = "Request body must contain at least one medicine." });

        var result = await _service.CreateBulkAsync(dtos);
        return Ok(result);
    }

    /// <summary>Update an existing medicine.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(MedicineResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateMedicineDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var (success, message, data) = await _service.UpdateAsync(id, dto);
        if (!success)
        {
            if (message.Contains("not found")) return NotFound(new { message });
            return BadRequest(new { message });
        }

        return Ok(data);
    }

    /// <summary>Delete a medicine by ID.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var (success, message) = await _service.DeleteAsync(id);
        if (!success)
        {
            if (message.Contains("not found")) return NotFound(new { message });
            return BadRequest(new { message });
        }

        return Ok(new { message });
    }
}
