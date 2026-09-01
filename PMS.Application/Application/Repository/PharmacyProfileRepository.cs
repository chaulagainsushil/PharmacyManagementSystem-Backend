using Microsoft.EntityFrameworkCore;
using PMS.Application.Application.Interface.Repository;
using PMS.Models.Models;
using PMS.Repository.Data;

namespace PMS.Application.Application.Repository;

public class PharmacyProfileRepository : IPharmacyProfileRepository
{
    private readonly ApplicationDbContext _db;

    public PharmacyProfileRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<PharmacyProfile>> GetAllAsync() =>
        await _db.PharmacyProfiles
            .AsNoTracking()
            .OrderBy(p => p.PharmacyName)
            .Select(p => new PharmacyProfile
            {
                PharmacyProfileId = p.PharmacyProfileId,
                TenantId          = p.TenantId,
                PharmacyName      = p.PharmacyName,
                LogoUrl           = p.LogoUrl,
                SignatureUrl      = p.SignatureUrl,
                PanNumber         = p.PanNumber,
                VatNumber         = p.VatNumber,
                Location          = p.Location,
                CreatedAt         = p.CreatedAt,
                UpdatedAt         = p.UpdatedAt,
                PhoneNumbers = p.PhoneNumbers
                    .Select(ph => new PharmacyPhoneNumber
                    {
                        PharmacyPhoneNumberId = ph.PharmacyPhoneNumberId,
                        PharmacyProfileId     = ph.PharmacyProfileId,
                        PhoneNumber           = ph.PhoneNumber,
                        Label                 = ph.Label,
                        UpdatedAt             = ph.UpdatedAt
                    })
                    .ToList()
            })
            .ToListAsync();

    public async Task<PharmacyProfile?> GetByIdAsync(int id) =>
        await _db.PharmacyProfiles
            .Where(p => p.PharmacyProfileId == id)
            .AsNoTracking()
            .Select(p => new PharmacyProfile
            {
                PharmacyProfileId = p.PharmacyProfileId,
                TenantId          = p.TenantId,
                PharmacyName      = p.PharmacyName,
                LogoUrl           = p.LogoUrl,
                SignatureUrl      = p.SignatureUrl,
                PanNumber         = p.PanNumber,
                VatNumber         = p.VatNumber,
                Location          = p.Location,
                CreatedAt         = p.CreatedAt,
                UpdatedAt         = p.UpdatedAt,
                PhoneNumbers = p.PhoneNumbers
                    .Select(ph => new PharmacyPhoneNumber
                    {
                        PharmacyPhoneNumberId = ph.PharmacyPhoneNumberId,
                        PharmacyProfileId     = ph.PharmacyProfileId,
                        PhoneNumber           = ph.PhoneNumber,
                        Label                 = ph.Label,
                        UpdatedAt             = ph.UpdatedAt
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync();

    public async Task<bool> AnyExistsAsync() =>
        await _db.PharmacyProfiles.AnyAsync();

    public async Task<PharmacyProfile> CreateAsync(PharmacyProfile profile)
    {
        _db.PharmacyProfiles.Add(profile);
        await _db.SaveChangesAsync();
        return profile;
    }

    public async Task<PharmacyProfile> UpdateAsync(PharmacyProfile profile)
    {
        var existing = await _db.PharmacyProfiles
            .Include(p => p.PhoneNumbers)
            .FirstOrDefaultAsync(p => p.PharmacyProfileId == profile.PharmacyProfileId);

        if (existing is null)
            return profile;

        existing.PharmacyName = profile.PharmacyName;
        existing.PanNumber    = profile.PanNumber;
        existing.VatNumber    = profile.VatNumber;
        existing.Location     = profile.Location;
        if (!string.IsNullOrWhiteSpace(profile.LogoUrl))
            existing.LogoUrl  = profile.LogoUrl;
        if (!string.IsNullOrWhiteSpace(profile.SignatureUrl))
            existing.SignatureUrl = profile.SignatureUrl;
        existing.UpdatedAt    = DateTime.UtcNow;

        // Replace phone numbers cleanly
        _db.PharmacyPhoneNumbers.RemoveRange(existing.PhoneNumbers);
        existing.PhoneNumbers = profile.PhoneNumbers.Select(ph => new PharmacyPhoneNumber
        {
            PharmacyProfileId = existing.PharmacyProfileId,
            PhoneNumber       = ph.PhoneNumber,
            Label             = ph.Label,
            UpdatedAt         = DateTime.UtcNow
        }).ToList();

        await _db.SaveChangesAsync();
        return existing;
    }

    public async Task<bool> UpdateLogoAsync(int id, string logoUrl)
    {
        var existing = await _db.PharmacyProfiles.FirstOrDefaultAsync(p => p.PharmacyProfileId == id);
        if (existing is null) return false;

        existing.LogoUrl   = logoUrl;
        existing.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateSignatureAsync(int id, string signatureUrl)
    {
        var existing = await _db.PharmacyProfiles.FirstOrDefaultAsync(p => p.PharmacyProfileId == id);
        if (existing is null) return false;

        existing.SignatureUrl = signatureUrl;
        existing.UpdatedAt    = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var profile = await _db.PharmacyProfiles
            .Where(p => p.PharmacyProfileId == id)
            .Select(p => new PharmacyProfile
            {
                PharmacyProfileId = p.PharmacyProfileId,
                PharmacyName      = p.PharmacyName,
                PhoneNumbers = p.PhoneNumbers
                    .Select(ph => new PharmacyPhoneNumber
                    {
                        PharmacyPhoneNumberId = ph.PharmacyPhoneNumberId,
                        PharmacyProfileId     = ph.PharmacyProfileId,
                        PhoneNumber           = ph.PhoneNumber,
                        Label                 = ph.Label
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync();

        if (profile is null) return false;

        _db.PharmacyProfiles.Remove(profile);
        await _db.SaveChangesAsync();
        return true;
    }
}
