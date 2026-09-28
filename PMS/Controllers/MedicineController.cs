using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PMS.Application.Application.Interface.Service;
using PMS.Common;
using PMS.Common.DTOs.Medicine;
using PMS.Common.DTOs.Uom;

namespace PMS.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MedicineController : ControllerBase
{
    private readonly IMedicineService _service;
    private readonly string _fileStorageBasePath;

    public MedicineController(IMedicineService service, IConfiguration configuration, IWebHostEnvironment env)
    {
        _service = service;
        var configured = configuration["FileStorage:BasePath"] ?? "uploads";
        _fileStorageBasePath = Path.IsPathRooted(configured)
            ? configured
            : Path.Combine(env.ContentRootPath, configured);
    }

    /// <summary>Get all medicines.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<MedicineListItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var data = await _service.GetAllAsync();
        return Ok(data);
    }

    /// <summary>Search medicines by name or generic name (server-side, case-insensitive, partial match).</summary>
    [HttpGet("search")]
    [ProducesResponseType(typeof(IEnumerable<MedicineListItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Search([FromQuery] string q = "")
    {
        var data = await _service.SearchAsync(q);
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
    [Authorize(Roles = $"{AppRoles.SystemAdmin},{AppRoles.PharmacyAdmin}")]
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
    /// </summary>
    [HttpPost("bulk")]
    [Authorize(Roles = $"{AppRoles.SystemAdmin},{AppRoles.PharmacyAdmin}")]
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
    [Authorize(Roles = $"{AppRoles.SystemAdmin},{AppRoles.PharmacyAdmin}")]
    [ProducesResponseType(typeof(MedicineResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateMedicineDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var (success, message, isConflict, data) = await _service.UpdateAsync(id, dto);
        if (!success)
        {
            if (isConflict) return Conflict(new { message });
            if (message.Contains("not found")) return NotFound(new { message });
            return BadRequest(new { message });
        }

        return Ok(data);
    }

    /// <summary>Delete a medicine by ID.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = $"{AppRoles.SystemAdmin},{AppRoles.PharmacyAdmin}")]
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

    /// <summary>Upload a prescription image and return the stored URL.</summary>
    [HttpPost("prescription-upload")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadPrescription(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "No file provided." });

        var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp", ".pdf" };
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowed.Contains(ext))
            return BadRequest(new { message = "Only JPG, PNG, WebP, or PDF files are accepted." });

        if (file.Length > 10 * 1024 * 1024)
            return BadRequest(new { message = "File size must not exceed 10 MB." });

        var uploadDir = Path.Combine(_fileStorageBasePath, "prescriptions");
        Directory.CreateDirectory(uploadDir);

        var fileName = $"{Guid.NewGuid()}{ext}";
        var filePath = Path.Combine(uploadDir, fileName);

        await using var stream = new FileStream(filePath, FileMode.Create);
        await file.CopyToAsync(stream);

        var url = $"/prescriptions/{fileName}";
        return Ok(new { url, fileName });
    }

    // ── Medicine Unit endpoints (convenience endpoints for dual-unit attach) ─────

    /// <summary>
    /// Attach a base unit and optionally a sub unit to a medicine in one call.
    /// When UseAutoCalculatedPrice=true, the sub-unit's selling price is computed
    /// server-side as BaseUnit.UnitPrice / SubUnit.ConversionFactorToBase.
    /// </summary>
    [HttpPost("{medicineId:int}/units")]
    [Authorize(Roles = $"{AppRoles.SystemAdmin},{AppRoles.PharmacyAdmin},{AppRoles.Pharmacist}")]
    [ProducesResponseType(typeof(IReadOnlyList<MedicineUnitDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddDualUnit(int medicineId, [FromBody] AddDualUnitCommand cmd)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var medicineUnitService = HttpContext.RequestServices.GetRequiredService<IMedicineUnitService>();
        var (success, message, data) = await medicineUnitService.AddDualUnitAsync(medicineId, cmd);

        if (!success)
        {
            if (message.Contains("not found"))
                return NotFound(new { message });
            return BadRequest(new { message });
        }

        return StatusCode(StatusCodes.Status201Created, data);
    }

    /// <summary>
    /// Get all units for a medicine with TotalInBaseUnits (medicine's total stock expressed in each unit's terms).
    /// </summary>
    [HttpGet("{medicineId:int}/units")]
    [ProducesResponseType(typeof(MedicineUnitsWithTotalsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUnitsWithTotals(int medicineId)
    {
        var medicineUnitService = HttpContext.RequestServices.GetRequiredService<IMedicineUnitService>();
        var (found, data) = await medicineUnitService.GetUnitsWithTotalsAsync(medicineId);

        if (!found)
            return NotFound(new { message = $"Medicine with ID {medicineId} not found." });

        return Ok(data);
    }
}
