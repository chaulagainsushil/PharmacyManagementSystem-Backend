using Microsoft.EntityFrameworkCore;
using PMS.Application.Application.Interface.Repository;
using PMS.Models.Models;
using PMS.Repository.Data;

namespace PMS.Application.Application.Repository;

public class SupplierRepository : ISupplierRepository
{
    private readonly ApplicationDbContext _db;

    public SupplierRepository(ApplicationDbContext db) => _db = db;

    public async Task<IEnumerable<Supplier>> GetAllAsync() =>
        await _db.Suppliers
            .Include(s => s.Batches)
            .AsNoTracking()
            .OrderBy(s => s.Name)
            .ToListAsync();

    public async Task<Supplier?> GetByIdAsync(int id) =>
        await _db.Suppliers
            .Include(s => s.Batches)
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.SupplierId == id);

    public async Task<bool> ExistsByNameAsync(string name, int? excludeId = null) =>
        await _db.Suppliers
            .AnyAsync(s => s.Name.ToLower() == name.ToLower()
                          && (excludeId == null || s.SupplierId != excludeId));

    public async Task<Supplier> CreateAsync(Supplier supplier)
    {
        _db.Suppliers.Add(supplier);
        await _db.SaveChangesAsync();
        return supplier;
    }

    public async Task<Supplier> UpdateAsync(Supplier supplier)
    {
        _db.Suppliers.Update(supplier);
        await _db.SaveChangesAsync();
        return supplier;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var supplier = await _db.Suppliers.FindAsync(id);
        if (supplier is null) return false;
        _db.Suppliers.Remove(supplier);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> HasBatchesAsync(int id) =>
        await _db.MedicineBatches.AnyAsync(b => b.SupplierId == id);
}
