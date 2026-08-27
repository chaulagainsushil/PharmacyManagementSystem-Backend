using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PMS.Application.Application.Interface.Service;
using PMS.Common;
using PMS.Common.DTOs.Customer;

namespace PMS.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CustomerController : ControllerBase
{
    private readonly ICustomerService _service;

    public CustomerController(ICustomerService service) => _service = service;

    /// <summary>Get all customers.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<CustomerListItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
        => Ok(await _service.GetAllAsync());

    /// <summary>Search customers by name or phone (server-side, case-insensitive, partial match).</summary>
    [HttpGet("search")]
    [ProducesResponseType(typeof(IEnumerable<CustomerListItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Search([FromQuery] string q = "")
        => Ok(await _service.SearchAsync(q));

    /// <summary>Get the default customer for billing (if configured).</summary>
    [HttpGet("default")]
    [ProducesResponseType(typeof(CustomerResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> GetDefault()
    {
        var data = await _service.GetDefaultAsync();
        return data is null ? NoContent() : Ok(data);
    }

    /// <summary>Get a customer by ID.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(CustomerResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var data = await _service.GetByIdAsync(id);
        return data is null ? NotFound(new { message = $"Customer with ID {id} not found." }) : Ok(data);
    }

    /// <summary>Set a customer as the default billing customer.</summary>
    [HttpPatch("{id:int}/set-default")]
    [Authorize(Roles = $"{AppRoles.SystemAdmin},{AppRoles.PharmacyAdmin},{AppRoles.Pharmacist},{AppRoles.Receptionist}")]
    [ProducesResponseType(typeof(CustomerResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetDefault(int id)
    {
        var (success, message, data) = await _service.SetDefaultAsync(id);
        if (!success)
        {
            return message.Contains("not found")
                ? NotFound(new { message })
                : BadRequest(new { message });
        }

        return Ok(data);
    }

    /// <summary>Create a new customer.</summary>
    [HttpPost]
    [Authorize(Roles = $"{AppRoles.SystemAdmin},{AppRoles.PharmacyAdmin},{AppRoles.Pharmacist},{AppRoles.Receptionist}")]
    [ProducesResponseType(typeof(CustomerResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateCustomerDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var (success, message, data) = await _service.CreateAsync(dto);
        if (!success) return BadRequest(new { message });

        return CreatedAtAction(nameof(GetById), new { id = data!.CustomerId }, data);
    }

    /// <summary>Update an existing customer.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = $"{AppRoles.SystemAdmin},{AppRoles.PharmacyAdmin},{AppRoles.Pharmacist},{AppRoles.Receptionist}")]
    [ProducesResponseType(typeof(CustomerResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCustomerDto dto)
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

    /// <summary>Delete a customer by ID.</summary>
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
