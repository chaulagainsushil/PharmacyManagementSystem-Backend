using Microsoft.EntityFrameworkCore;
using PMS.Application.Application.Interface.Repository;
using PMS.Models.Interfaces;
using PMS.Models.Models;
using PMS.Repository.Data;

namespace PMS.Application.Application.Repository;

public class UnitOfMeasureRepository : IUnitOfMeasureRepository
{
    private readonly ApplicationDbContext _db;
    private readonly ITenantProvider _tenantProvider;

    public UnitOfMeasureRepository(ApplicationDbContext db, ITenantProvider tenantProvider)
    {
        _db = db;
        _tenantProvider = tenantProvider;
    }

    public async Task<IReadOnlyList<UnitOfMeasure>> GetAllAsync(bool includeInactive = false)
    {
        var query = _db.UnitsOfMeasure.AsNoTracking();
        if (!includeInactive)
            query = query.Where(u => u.IsActive);
        return await query.OrderBy(u => u.Name).ToListAsync();
    }

    public async Task<UnitOfMeasure?> GetByIdAsync(int id) =>
        await _db.UnitsOfMeasure.AsNoTracking()
            .FirstOrDefaultAsync(u => u.UnitOfMeasureId == id);

    public async Task<bool> ExistsByNameAsync(string name, int? excludeId = null)
    {
        return await _db.UnitsOfMeasure
            .AnyAsync(u =>
                u.Name.ToLower() == name.ToLower() &&
                (excludeId == null || u.UnitOfMeasureId != excludeId));
    }

    public async Task<UnitOfMeasure> CreateAsync(UnitOfMeasure uom)
    {
        _db.UnitsOfMeasure.Add(uom);
        await _db.SaveChangesAsync();
        return uom;
    }

    public async Task<UnitOfMeasure> UpdateAsync(UnitOfMeasure uom)
    {
        var existing = await _db.UnitsOfMeasure.FindAsync(uom.UnitOfMeasureId);
        if (existing != null)
        {
            existing.Name      = uom.Name;
            existing.Symbol    = uom.Symbol;
            existing.IsActive  = uom.IsActive;
            existing.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return existing;
        }

        _db.UnitsOfMeasure.Update(uom);
        await _db.SaveChangesAsync();
        return uom;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var uom = await _db.UnitsOfMeasure.FindAsync(id);
        if (uom is null) return false;
        _db.UnitsOfMeasure.Remove(uom);
        await _db.SaveChangesAsync();
        return true;
    }
}
