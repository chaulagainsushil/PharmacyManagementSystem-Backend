using Microsoft.EntityFrameworkCore;
using PMS.Application.Application.Interface.Repository;
using PMS.Models.Models;
using PMS.Repository.Data;

namespace PMS.Application.Application.Repository;

public class MedicineBatchRepository : IMedicineBatchRepository
{
    private readonly ApplicationDbContext _db;

    public MedicineBatchRepository(ApplicationDbContext db) => _db = db;

    public async Task<IEnumerable<MedicineBatch>> GetAllAsync() =>
        await _db.MedicineBatches
            .Include(b => b.Medicine)
            .Include(b => b.Supplier)
            .AsNoTracking()
            .OrderBy(b => b.ExpiryDate)
            .ToListAsync();

    public async Task<IEnumerable<MedicineBatch>> GetByMedicineIdAsync(int medicineId) =>
        await _db.MedicineBatches
            .Include(b => b.Medicine)
            .Include(b => b.Supplier)
            .AsNoTracking()
            .Where(b => b.MedicineId == medicineId)
            .OrderBy(b => b.ExpiryDate)
            .ToListAsync();

    public async Task<MedicineBatch?> GetByIdAsync(int id) =>
        await _db.MedicineBatches
            .Include(b => b.Medicine)
            .Include(b => b.Supplier)
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.BatchId == id);

    public async Task<bool> ExistsByBatchNumberAsync(string batchNumber, int? excludeId = null) =>
        await _db.MedicineBatches
            .AnyAsync(b => b.BatchNumber.ToLower() == batchNumber.ToLower()
                          && (excludeId == null || b.BatchId != excludeId));

    public async Task<MedicineBatch> CreateAsync(MedicineBatch batch)
    {
        _db.MedicineBatches.Add(batch);
        await _db.SaveChangesAsync();
        return batch;
    }

    public async Task<MedicineBatch> UpdateAsync(MedicineBatch batch)
    {
        _db.MedicineBatches.Update(batch);
        await _db.SaveChangesAsync();
        return batch;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var batch = await _db.MedicineBatches.FindAsync(id);
        if (batch is null) return false;
        _db.MedicineBatches.Remove(batch);
        await _db.SaveChangesAsync();
        return true;
    }
}
