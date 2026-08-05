using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PMS.Application.Application.Interface.Service;
using PMS.Common;
using PMS.Common.DTOs.PharmacyProfile;

namespace PMS.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PharmacyProfileController : ControllerBase
{
    private readonly IPharmacyProfileService _service;
    private readonly IWebHostEnvironment _env;

    private static readonly string[] AllowedImageExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
    private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB

    public PharmacyProfileController(IPharmacyProfileService service, IWebHostEnvironment env)
    {
        _service = service;
        _env     = env;
    }

    // ── Queries ────────────────────────────────────────────────────────────────

    /// <summary>Get all pharmacy profiles (typically returns one).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<PharmacyProfileListItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var data = await _service.GetAllAsync();
        return Ok(data);
    }

    /// <summary>Get a pharmacy profile by ID.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(PharmacyProfileResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var data = await _service.GetByIdAsync(id);
        if (data is null) return NotFound(new { message = $"Pharmacy profile with ID {id} not found." });
        return Ok(data);
    }

    // ── Commands ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Create the pharmacy profile.
    /// Only one profile may exist — call the update endpoint for subsequent changes.
    /// Logo and signature images are uploaded via their dedicated endpoints after creation.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = $"{AppRoles.SystemAdmin},{AppRoles.PharmacyAdmin}")]
    [ProducesResponseType(typeof(PharmacyProfileResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreatePharmacyProfileDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var (success, message, data) = await _service.CreateAsync(dto);
        if (!success) return BadRequest(new { message });

        return CreatedAtAction(nameof(GetById), new { id = data!.PharmacyProfileId }, data);
    }

    /// <summary>
    /// Update the pharmacy profile details (name, PAN, VAT, location, phone numbers).
    /// Replaces all phone numbers with the supplied list.
    /// Logo and signature are NOT updated here — use the dedicated upload endpoints.
    /// </summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = $"{AppRoles.SystemAdmin},{AppRoles.PharmacyAdmin}")]
    [ProducesResponseType(typeof(PharmacyProfileResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdatePharmacyProfileDto dto)
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

    /// <summary>Delete the pharmacy profile by ID.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = $"{AppRoles.SystemAdmin}")]
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

    // ── File uploads ───────────────────────────────────────────────────────────

    /// <summary>
    /// Upload or replace the pharmacy logo.
    /// Accepted formats: JPG, JPEG, PNG, WebP. Max size: 5 MB.
    /// Returns the stored relative URL and updates the profile record.
    /// </summary>
    [HttpPost("{id:int}/upload-logo")]
    [Authorize(Roles = $"{AppRoles.SystemAdmin},{AppRoles.PharmacyAdmin}")]
    [ProducesResponseType(typeof(PharmacyProfileResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UploadLogo(int id, IFormFile file)
    {
        var validationResult = ValidateImageFile(file);
        if (validationResult is not null) return validationResult;

        var url = await SaveFileAsync(file, "pharmacy/logos");

        var (success, message, data) = await _service.UpdateLogoAsync(id, url);
        if (!success)
        {
            if (message.Contains("not found")) return NotFound(new { message });
            return BadRequest(new { message });
        }

        return Ok(data);
    }

    /// <summary>
    /// Upload or replace the authorised signature image.
    /// Accepted formats: JPG, JPEG, PNG, WebP. Max size: 5 MB.
    /// Returns the stored relative URL and updates the profile record.
    /// </summary>
    [HttpPost("{id:int}/upload-signature")]
    [Authorize(Roles = $"{AppRoles.SystemAdmin},{AppRoles.PharmacyAdmin}")]
    [ProducesResponseType(typeof(PharmacyProfileResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UploadSignature(int id, IFormFile file)
    {
        var validationResult = ValidateImageFile(file);
        if (validationResult is not null) return validationResult;

        var url = await SaveFileAsync(file, "pharmacy/signatures");

        var (success, message, data) = await _service.UpdateSignatureAsync(id, url);
        if (!success)
        {
            if (message.Contains("not found")) return NotFound(new { message });
            return BadRequest(new { message });
        }

        return Ok(data);
    }

    // ── Private helpers ────────────────────────────────────────────────────────

    /// <summary>
    /// Validates the uploaded image file.
    /// Returns a <see cref="BadRequestObjectResult"/> if invalid, or null if valid.
    /// </summary>
    private IActionResult? ValidateImageFile(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "No file provided." });

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedImageExtensions.Contains(ext))
            return BadRequest(new { message = "Only JPG, PNG, or WebP files are accepted." });

        if (file.Length > MaxFileSizeBytes)
            return BadRequest(new { message = "File size must not exceed 5 MB." });

        return null;
    }

    /// <summary>
    /// Saves the file to <c>wwwroot/{subFolder}</c> with a GUID-based filename
    /// and returns the relative URL (e.g. <c>/pharmacy/logos/abc123.png</c>).
    /// </summary>
    private async Task<string> SaveFileAsync(IFormFile file, string subFolder)
    {
        var uploadDir = Path.Combine(_env.WebRootPath ?? "wwwroot", subFolder);
        Directory.CreateDirectory(uploadDir);

        var ext      = Path.GetExtension(file.FileName).ToLowerInvariant();
        var fileName = $"{Guid.NewGuid()}{ext}";
        var filePath = Path.Combine(uploadDir, fileName);

        await using var stream = new FileStream(filePath, FileMode.Create);
        await file.CopyToAsync(stream);

        return $"/{subFolder}/{fileName}";
    }
}
