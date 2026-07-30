using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PMS.Application.Application.Interface.Service;
using PMS.Common.DTOs.MedicineBatch;

namespace PMS.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MedicineBatchController : ControllerBase
{
    private readonly IMedicineBatchService _service;

    public MedicineBatchController(IMedicineBatchService service) => _service = service;

    /// <summary>Get all medicine batches.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<MedicineBatchListItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
        => Ok(await _service.GetAllAsync());

    /// <summary>Get all batches for a specific medicine.</summary>
    [HttpGet("by-medicine/{medicineId:int}")]
    [ProducesResponseType(typeof(IEnumerable<MedicineBatchListItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByMedicineId(int medicineId)
        => Ok(await _service.GetByMedicineIdAsync(medicineId));

    /// <summary>Get a medicine batch by ID.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(MedicineBatchResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var data = await _service.GetByIdAsync(id);
        return data is null ? NotFound(new { message = $"Medicine batch with ID {id} not found." }) : Ok(data);
    }

    /// <summary>Create a new medicine batch.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(MedicineBatchResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateMedicineBatchDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var (success, message, data) = await _service.CreateAsync(dto);
        if (!success) return BadRequest(new { message });

        return CreatedAtAction(nameof(GetById), new { id = data!.BatchId }, data);
    }

    /// <summary>Update an existing medicine batch.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(MedicineBatchResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateMedicineBatchDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var (success, message, data) = await _service.UpdateAsync(id, dto);
        if (!success)
            return message.Contains("not found")
                ? NotFound(new { message })
                : BadRequest(new { message });

        return Ok(data);
    }

    /// <summary>Delete a medicine batch by ID.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
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
