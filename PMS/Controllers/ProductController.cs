using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PMS.Application.Application.Interface.Service;
using PMS.Common;
using PMS.Common.DTOs.Product;

namespace PMS.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProductController : ControllerBase
{
    private readonly IProductUnitService _service;

    public ProductController(IProductUnitService service) => _service = service;

    /// <summary>
    /// Attach a base unit and optionally a sub unit to a product in one call.
    /// Auto-calculates sub-unit quantity and pricing if not provided.
    /// </summary>
    [HttpPost("{productId:int}/units")]
    [Authorize(Roles = $"{AppRoles.SystemAdmin},{AppRoles.PharmacyAdmin}")]
    [ProducesResponseType(typeof(IReadOnlyList<ProductUnitDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddDualUnit(int productId, [FromBody] AddDualUnitCommand cmd)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var (success, message, data) = await _service.AddDualUnitAsync(productId, cmd);

        if (!success)
        {
            if (message.Contains("not found"))
                return NotFound(new { message });
            return BadRequest(new { message });
        }

        return StatusCode(StatusCodes.Status201Created, data);
    }

    /// <summary>
    /// Get all units for a product with computed values.
    /// </summary>
    [HttpGet("{productId:int}/units")]
    [ProducesResponseType(typeof(ProductUnitsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUnits(int productId)
    {
        var (found, data) = await _service.GetUnitsAsync(productId);

        if (!found)
            return NotFound(new { message = $"Product with ID {productId} not found." });

        return Ok(data);
    }
}
