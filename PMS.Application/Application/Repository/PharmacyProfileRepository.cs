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

    public async Task<IEnumerable<PharmacyProfile>> GetAllAsync()
    {
        return await _db.PharmacyProfiles
            .Include(p => p.PhoneNumbers)
            .AsNoTracking()
            .OrderBy(p => p.PharmacyName)
            .ToListAsync();
    }

    public async Task<PharmacyProfile?> GetByIdAsync(int id)
    {
        return await _db.PharmacyProfiles
            .Include(p => p.PhoneNumbers)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.PharmacyProfileId == id);
    }

    public async Task<bool> AnyExistsAsync()
    {
        return await _db.PharmacyProfiles.AnyAsync();
    }

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
            .Include(p => p.PhoneNumbers)
            .FirstOrDefaultAsync(p => p.PharmacyProfileId == id);

        if (profile is null) return false;

        _db.PharmacyProfiles.Remove(profile);
        await _db.SaveChangesAsync();
        return true;
    }
}
