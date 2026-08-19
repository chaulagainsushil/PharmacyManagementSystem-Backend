using Microsoft.EntityFrameworkCore;
using PMS.Application.Application.Interface.Repository;
using PMS.Common.DTOs.Supplier;
using PMS.Models.Interfaces;
using PMS.Models.Models;
using PMS.Repository.Data;

namespace PMS.Application.Application.Repository;

public class SupplierRepository : ISupplierRepository
{
    private readonly ApplicationDbContext _db;
    private readonly ITenantProvider _tenantProvider;

    public SupplierRepository(ApplicationDbContext db, ITenantProvider tenantProvider)
    {
        _db = db;
        _tenantProvider = tenantProvider;
    }

    public async Task<IEnumerable<SupplierListItemDto>> GetAllAsync() =>
        await _db.Suppliers
            .AsNoTracking()
            .OrderBy(s => s.Name)
            .Select(s => new SupplierListItemDto
            {
                SupplierId  = s.SupplierId,
                Name        = s.Name,
                Phone       = s.Phone,
                Address     = s.Address,
                BatchCount  = s.Batches.Count(),
                UpdatedAt   = s.UpdatedAt
            })
            .ToListAsync();

    public async Task<SupplierResponseDto?> GetByIdAsync(int id) =>
        await _db.Suppliers
            .AsNoTracking()
            .Where(s => s.SupplierId == id)
            .Select(s => new SupplierResponseDto
            {
                SupplierId  = s.SupplierId,
                Name        = s.Name,
                Phone       = s.Phone,
                Address     = s.Address,
                BatchCount  = s.Batches.Count(),
                UpdatedAt   = s.UpdatedAt
            })
            .FirstOrDefaultAsync();

    public async Task<Supplier?> GetEntityByIdAsync(int id) =>
        await _db.Suppliers.FindAsync(id);

    public async Task<bool> ExistsByNameAsync(string name, int? excludeId = null)
    {
        return await _db.Suppliers
            .AnyAsync(s => s.Name.ToLower() == name.ToLower()
                          && (excludeId == null || s.SupplierId != excludeId));
    }

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
