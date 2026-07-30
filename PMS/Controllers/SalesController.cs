using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PMS.Application.Application.Interface.Service;
using PMS.Common.DTOs.Sale;

namespace PMS.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SalesController : ControllerBase
{
    private readonly ISaleService _service;

    public SalesController(ISaleService service) => _service = service;

    /// <summary>Get all sales, newest first.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<SaleResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
        => Ok(await _service.GetAllAsync());

    /// <summary>Get a single sale by ID.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(SaleResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var data = await _service.GetByIdAsync(id);
        return data is null ? NotFound(new { message = $"Sale with ID {id} not found." }) : Ok(data);
    }

    /// <summary>
    /// Create a new sale (sell medicine).
    /// Validates stock, applies FEFO batch deduction, calculates discounts,
    /// and persists everything in a single transaction.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(SaleResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateSale([FromBody] CreateSaleRequestDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var (success, message, data) = await _service.CreateSaleAsync(dto);

        if (!success)
        {
            if (message.Contains("not found", StringComparison.OrdinalIgnoreCase))
                return NotFound(new { message });

            return BadRequest(new { message });
        }

        return StatusCode(StatusCodes.Status201Created, data);
    }
}
