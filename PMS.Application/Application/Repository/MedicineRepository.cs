using Microsoft.EntityFrameworkCore;
using PMS.Application.Application.Interface.Repository;
using PMS.Models.Models;
using PMS.Repository.Data;

namespace PMS.Application.Application.Repository;

public class MedicineRepository : IMedicineRepository
{
    private readonly ApplicationDbContext _db;

    public MedicineRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<Medicine>> GetAllAsync()
    {
        return await _db.Medicines
            .Include(m => m.Category)
            .Include(m => m.Manufacturer)
            .Include(m => m.Batches)
            .AsNoTracking()
            .OrderBy(m => m.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<Medicine>> SearchAsync(string query)
    {
        var q = query.Trim().ToLower();
        return await _db.Medicines
            .Include(m => m.Category)
            .Include(m => m.Manufacturer)
            .Include(m => m.Batches)
            .AsNoTracking()
            .Where(m =>
                m.Name.ToLower().Contains(q) ||
                (m.GenericName != null && m.GenericName.ToLower().Contains(q)) ||
                (m.Category != null && m.Category.CategoryName.ToLower().Contains(q)) ||
                (m.Manufacturer != null && m.Manufacturer.Name.ToLower().Contains(q)))
            .OrderBy(m => m.Name)
            .ToListAsync();
    }

    public async Task<Medicine?> GetByIdAsync(int id)
    {
        return await _db.Medicines
            .Include(m => m.Category)
            .Include(m => m.Manufacturer)
            .Include(m => m.Batches)
            .Include(m => m.Units.OrderByDescending(u => u.IsDefault).ThenBy(u => u.UnitOfMeasure.Name))
                .ThenInclude(u => u.UnitOfMeasure)
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.MedicineId == id);
    }

    public async Task<bool> ExistsByNameAsync(string name, int? excludeId = null)
    {
        return await _db.Medicines
            .AnyAsync(m => m.Name.ToLower() == name.ToLower()
                          && (excludeId == null || m.MedicineId != excludeId));
    }

    public async Task<Medicine> CreateAsync(Medicine medicine)
    {
        _db.Medicines.Add(medicine);
        await _db.SaveChangesAsync();
        return medicine;
    }

    public async Task<IReadOnlyList<Medicine>> CreateBulkAsync(IEnumerable<Medicine> medicines)
    {
        var list = medicines.ToList();
        _db.Medicines.AddRange(list);
        await _db.SaveChangesAsync();
        return list;
    }

    public async Task<Medicine> UpdateAsync(Medicine medicine)
    {
        _db.Medicines.Update(medicine);
        await _db.SaveChangesAsync();
        return medicine;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var medicine = await _db.Medicines.FindAsync(id);
        if (medicine is null) return false;

        _db.Medicines.Remove(medicine);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task ReplaceUnitsAsync(int medicineId, IEnumerable<MedicineUnit> newUnits)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync();
        try
        {
            // Remove all existing units for this medicine
            var existing = await _db.MedicineUnits
                .Where(mu => mu.MedicineId == medicineId)
                .ToListAsync();

            _db.MedicineUnits.RemoveRange(existing);

            // Insert the new units
            _db.MedicineUnits.AddRange(newUnits);

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
