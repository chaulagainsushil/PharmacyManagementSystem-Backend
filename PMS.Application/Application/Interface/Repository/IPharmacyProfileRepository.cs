using PMS.Models.Models;

namespace PMS.Application.Application.Interface.Repository;

public interface IPharmacyProfileRepository
{
    Task<IEnumerable<PharmacyProfile>> GetAllAsync();
    Task<PharmacyProfile?> GetByIdAsync(int id);

    /// <summary>Returns true when any profile already exists (singleton guard).</summary>
    Task<bool> AnyExistsAsync();

    Task<PharmacyProfile> CreateAsync(PharmacyProfile profile);
    Task<PharmacyProfile> UpdateAsync(PharmacyProfile profile);
    Task<bool> DeleteAsync(int id);
}
