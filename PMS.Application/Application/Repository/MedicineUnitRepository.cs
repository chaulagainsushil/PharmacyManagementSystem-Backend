using Microsoft.EntityFrameworkCore;
using PMS.Application.Application.Interface.Repository;
using PMS.Models.Models;
using PMS.Repository.Data;

namespace PMS.Application.Application.Repository;

public class MedicineUnitRepository : IMedicineUnitRepository
{
    private readonly ApplicationDbContext _db;

    public MedicineUnitRepository(ApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<MedicineUnit>> GetByMedicineIdAsync(int medicineId) =>
        await _db.MedicineUnits
            .Where(mu => mu.MedicineId == medicineId)
            .Include(mu => mu.UnitOfMeasure)
            .AsNoTracking()
            .OrderByDescending(mu => mu.IsDefault)
            .ThenBy(mu => mu.UnitOfMeasure.Name)
            .ToListAsync();

    public async Task<IReadOnlyList<MedicineUnit>> GetActiveForPosAsync(int medicineId) =>
        await _db.MedicineUnits
            .Where(mu => mu.MedicineId == medicineId && mu.IsActive)
            .Include(mu => mu.UnitOfMeasure)
            .AsNoTracking()
            .OrderByDescending(mu => mu.IsDefault)   // default first
            .ThenBy(mu => mu.UnitOfMeasure.Name)
            .ToListAsync();

    public async Task<MedicineUnit?> GetByIdAsync(int medicineUnitId) =>
        await _db.MedicineUnits
            .Include(mu => mu.UnitOfMeasure)
            .AsNoTracking()
            .FirstOrDefaultAsync(mu => mu.MedicineUnitId == medicineUnitId);

    public async Task<bool> ExistsAsync(int medicineId, int unitOfMeasureId, int? excludeId = null) =>
        await _db.MedicineUnits.AnyAsync(mu =>
            mu.MedicineId == medicineId &&
            mu.UnitOfMeasureId == unitOfMeasureId &&
            (excludeId == null || mu.MedicineUnitId != excludeId));

    public async Task<MedicineUnit> CreateAsync(MedicineUnit unit)
    {
        _db.MedicineUnits.Add(unit);
        await _db.SaveChangesAsync();
        return unit;
    }

    public async Task<MedicineUnit> UpdateAsync(MedicineUnit unit)
    {
        _db.MedicineUnits.Update(unit);
        await _db.SaveChangesAsync();
        return unit;
    }

    public async Task<Medicine?> GetMedicineWithUnitsAsync(int medicineId) =>
        await _db.Medicines
            .Where(m => m.MedicineId == medicineId && m.IsActive)
            .Include(m => m.Units.Where(u => u.IsActive))
                .ThenInclude(u => u.UnitOfMeasure)
            .FirstOrDefaultAsync();

    public async Task<Medicine?> GetMedicineWithUnitsAndFefoStockAsync(int medicineId) =>
        await _db.Medicines
            .Where(m => m.MedicineId == medicineId && m.IsActive)
            .Include(m => m.Units.Where(u => u.IsActive))
                .ThenInclude(u => u.UnitOfMeasure)
            .Include(m => m.Batches
                .Where(b => b.ExpiryDate.Date > DateTime.UtcNow.Date && b.QuantityInTablets > 0)
                .OrderBy(b => b.ExpiryDate))
            .FirstOrDefaultAsync();
}
