using PMS.Application.Application.Interface.Repository;
using PMS.Application.Application.Interface.Service;
using PMS.Common;
using PMS.Common.DTOs.PharmacyProfile;
using PMS.Models.Models;

namespace PMS.Application.Application.Service;

public class PharmacyProfileService : IPharmacyProfileService
{
    private readonly IPharmacyProfileRepository _repo;

    public PharmacyProfileService(IPharmacyProfileRepository repo)
    {
        _repo = repo;
    }

    // ── Queries ────────────────────────────────────────────────────────────────

    public async Task<IEnumerable<PharmacyProfileListItemDto>> GetAllAsync()
    {
        var profiles = await _repo.GetAllAsync();
        return profiles.Select(MapToListItem);
    }

    public async Task<PharmacyProfileResponseDto?> GetByIdAsync(int id)
    {
        var profile = await _repo.GetByIdAsync(id);
        return profile is null ? null : MapToResponse(profile);
    }

    // ── Commands ───────────────────────────────────────────────────────────────

    public async Task<(bool Success, string Message, PharmacyProfileResponseDto? Data)> CreateAsync(
        CreatePharmacyProfileDto dto)
    {
        if (await _repo.AnyExistsAsync())
            return (false, "A pharmacy profile already exists. Use the update endpoint to modify it.", null);

        if (dto.PhoneNumbers == null || dto.PhoneNumbers.Count == 0)
            return (false, "At least one phone number is required.", null);

        var profile = new PharmacyProfile
        {
            PharmacyName = dto.PharmacyName.Trim(),
            PanNumber    = dto.PanNumber?.Trim(),
            VatNumber    = dto.VatNumber?.Trim(),
            Location     = dto.Location?.Trim(),
            CreatedAt    = DateTime.UtcNow,
            PhoneNumbers = dto.PhoneNumbers.Select(p => new PharmacyPhoneNumber
            {
                PhoneNumber = p.PhoneNumber.Trim(),
                Label       = p.Label?.Trim()
            }).ToList()
        };

        var created = await _repo.CreateAsync(profile);

        var full = await _repo.GetByIdAsync(created.PharmacyProfileId);
        return (true, "Pharmacy profile created successfully.", MapToResponse(full!));
    }

    public async Task<(bool Success, string Message, bool IsConflict, PharmacyProfileResponseDto? Data)> UpdateAsync(
        int id, UpdatePharmacyProfileDto dto)
    {
        // 1. Read current entity (includes UpdatedAt)
        var existing = await _repo.GetByIdAsync(id);
        if (existing is null)
            return (false, $"Pharmacy profile with ID {id} not found.", false, null);

        // 2. Optimistic concurrency check
        if (!ConcurrencyHelper.Matches(existing.UpdatedAt, dto.UpdatedAt))
            return (false, "The record was modified by another user. Please refresh and try again.", true, null);

        // 3. Validate phone numbers
        if (dto.PhoneNumbers == null || dto.PhoneNumbers.Count == 0)
            return (false, "At least one phone number is required.", false, null);

        // 4. Build update entity, preserving logo/signature URLs
        var profile = new PharmacyProfile
        {
            PharmacyProfileId = id,
            PharmacyName      = dto.PharmacyName.Trim(),
            LogoUrl           = existing.LogoUrl,
            SignatureUrl      = existing.SignatureUrl,
            PanNumber         = dto.PanNumber?.Trim(),
            VatNumber         = dto.VatNumber?.Trim(),
            Location          = dto.Location?.Trim(),
            CreatedAt         = existing.CreatedAt,
            PhoneNumbers      = dto.PhoneNumbers.Select(p => new PharmacyPhoneNumber
            {
                PharmacyProfileId = id,
                PhoneNumber       = p.PhoneNumber.Trim(),
                Label             = p.Label?.Trim()
            }).ToList()
        };

        await _repo.UpdateAsync(profile);

        var full = await _repo.GetByIdAsync(id);
        return (true, "Pharmacy profile updated successfully.", false, MapToResponse(full!));
    }

    public async Task<(bool Success, string Message, PharmacyProfileResponseDto? Data)> UpdateLogoAsync(
        int id, string logoUrl)
    {
        var profile = await _repo.GetByIdAsync(id);
        if (profile is null)
            return (false, $"Pharmacy profile with ID {id} not found.", null);

        var updated = new PharmacyProfile
        {
            PharmacyProfileId = id,
            PharmacyName      = profile.PharmacyName,
            LogoUrl           = logoUrl,
            SignatureUrl      = profile.SignatureUrl,
            PanNumber         = profile.PanNumber,
            VatNumber         = profile.VatNumber,
            Location          = profile.Location,
            CreatedAt         = profile.CreatedAt,
            PhoneNumbers      = profile.PhoneNumbers.Select(p => new PharmacyPhoneNumber
            {
                PharmacyPhoneNumberId = p.PharmacyPhoneNumberId,
                PharmacyProfileId     = id,
                PhoneNumber           = p.PhoneNumber,
                Label                 = p.Label
            }).ToList()
        };

        await _repo.UpdateAsync(updated);

        var full = await _repo.GetByIdAsync(id);
        return (true, "Logo updated successfully.", MapToResponse(full!));
    }

    public async Task<(bool Success, string Message, PharmacyProfileResponseDto? Data)> UpdateSignatureAsync(
        int id, string signatureUrl)
    {
        var profile = await _repo.GetByIdAsync(id);
        if (profile is null)
            return (false, $"Pharmacy profile with ID {id} not found.", null);

        var updated = new PharmacyProfile
        {
            PharmacyProfileId = id,
            PharmacyName      = profile.PharmacyName,
            LogoUrl           = profile.LogoUrl,
            SignatureUrl      = signatureUrl,
            PanNumber         = profile.PanNumber,
            VatNumber         = profile.VatNumber,
            Location          = profile.Location,
            CreatedAt         = profile.CreatedAt,
            PhoneNumbers      = profile.PhoneNumbers.Select(p => new PharmacyPhoneNumber
            {
                PharmacyPhoneNumberId = p.PharmacyPhoneNumberId,
                PharmacyProfileId     = id,
                PhoneNumber           = p.PhoneNumber,
                Label                 = p.Label
            }).ToList()
        };

        await _repo.UpdateAsync(updated);

        var full = await _repo.GetByIdAsync(id);
        return (true, "Signature updated successfully.", MapToResponse(full!));
    }

    public async Task<(bool Success, string Message)> DeleteAsync(int id)
    {
        var exists = await _repo.GetByIdAsync(id);
        if (exists is null)
            return (false, $"Pharmacy profile with ID {id} not found.");

        await _repo.DeleteAsync(id);
        return (true, "Pharmacy profile deleted successfully.");
    }

    // ── Mapping helpers ────────────────────────────────────────────────────────

    private static PharmacyProfileResponseDto MapToResponse(PharmacyProfile p) => new()
    {
        PharmacyProfileId = p.PharmacyProfileId,
        PharmacyName      = p.PharmacyName,
        LogoUrl           = p.LogoUrl,
        SignatureUrl      = p.SignatureUrl,
        PanNumber         = p.PanNumber,
        VatNumber         = p.VatNumber,
        Location          = p.Location,
        CreatedAt         = p.CreatedAt,
        UpdatedAt         = p.UpdatedAt,
        PhoneNumbers      = p.PhoneNumbers.Select(ph => new PharmacyPhoneNumberResponseDto
        {
            PharmacyPhoneNumberId = ph.PharmacyPhoneNumberId,
            PhoneNumber           = ph.PhoneNumber,
            Label                 = ph.Label
        }).ToList()
    };

    private static PharmacyProfileListItemDto MapToListItem(PharmacyProfile p) => new()
    {
        PharmacyProfileId = p.PharmacyProfileId,
        PharmacyName      = p.PharmacyName,
        LogoUrl           = p.LogoUrl,
        PanNumber         = p.PanNumber,
        VatNumber         = p.VatNumber,
        Location          = p.Location,
        UpdatedAt         = p.UpdatedAt,
        PhoneNumbers      = p.PhoneNumbers.Select(ph => new PharmacyPhoneNumberResponseDto
        {
            PharmacyPhoneNumberId = ph.PharmacyPhoneNumberId,
            PhoneNumber           = ph.PhoneNumber,
            Label                 = ph.Label
        }).ToList()
    };
}
