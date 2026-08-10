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
        // Replace phone numbers: remove old ones then let EF add the new collection.
        var existingPhones = await _db.PharmacyPhoneNumbers
            .Where(p => p.PharmacyProfileId == profile.PharmacyProfileId)
            .ToListAsync();

        _db.PharmacyPhoneNumbers.RemoveRange(existingPhones);
        _db.PharmacyProfiles.Update(profile);
        await _db.SaveChangesAsync();
        return profile;
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
