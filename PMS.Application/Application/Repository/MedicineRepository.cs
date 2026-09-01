using Microsoft.EntityFrameworkCore;
using PMS.Application.Application.Interface.Repository;
using PMS.Common.DTOs.Medicine;
using PMS.Common.DTOs.Uom;
using PMS.Models.Interfaces;
using PMS.Models.Models;
using PMS.Repository.Data;

namespace PMS.Application.Application.Repository;

public class MedicineRepository : IMedicineRepository
{
    private readonly ApplicationDbContext _db;
    private readonly ITenantProvider _tenantProvider;

    public MedicineRepository(ApplicationDbContext db, ITenantProvider tenantProvider)
    {
        _db = db;
        _tenantProvider = tenantProvider;
    }

    public async Task<IEnumerable<MedicineListItemDto>> GetAllAsync()
    {
        return await _db.Medicines
            .AsNoTracking()
            .OrderBy(m => m.Name)
            .Select(m => new MedicineListItemDto
            {
                MedicineId           = m.MedicineId,
                Name                 = m.Name,
                GenericName          = m.GenericName,
                CategoryName         = m.Category != null ? m.Category.CategoryName : null,
                ManufacturerName     = m.Manufacturer != null ? m.Manufacturer.Name : null,
                TotalStockInBaseUnit = m.Batches.Where(b => b.ExpiryDate.Date >= DateTime.UtcNow.Date && b.QuantityInBaseUnit > 0).Sum(b => b.QuantityInBaseUnit),
                ReorderLevel         = m.ReorderLevel,
                IsActive             = m.IsActive,
                RequiresPrescription = m.RequiresPrescription,
                UpdatedAt            = m.UpdatedAt,
                DefaultUnitName      = m.Units
                    .Where(u => u.IsDefault && u.IsActive)
                    .Select(u => u.UnitOfMeasure.Name)
                    .FirstOrDefault(),
                DefaultUnitPrice     = m.Units
                    .Where(u => u.IsDefault && u.IsActive)
                    .Select(u => (decimal?)u.UnitPrice)
                    .FirstOrDefault(),
                Units = m.Units
                    .Where(u => u.IsActive)
                    .OrderByDescending(u => u.IsDefault)
                    .ThenBy(u => u.UnitOfMeasure.Name)
                    .Select(u => new MedicineUnitDto
                    {
                        MedicineUnitId         = u.MedicineUnitId,
                        MedicineId             = u.MedicineId,
                        UnitOfMeasureId        = u.UnitOfMeasureId,
                        UomName                = u.UnitOfMeasure.Name,
                        UomSymbol              = u.UnitOfMeasure.Symbol,
                        ConversionFactorToBase = u.ConversionFactorToBase,
                        IsBaseUnit             = u.IsBaseUnit,
                        IsDefault              = u.IsDefault,
                        IsActive               = u.IsActive,
                        CanPurchase            = u.CanPurchase,
                        CanSell                = u.CanSell,
                        CostPrice              = u.CostPrice,
                        UnitPrice              = u.UnitPrice,
                        Mrp                    = u.Mrp,
                        UpdatedAt              = u.UpdatedAt
                    })
                    .ToList()
            })
            .ToListAsync();
    }

    public async Task<IEnumerable<MedicineListItemDto>> SearchAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return await GetAllAsync();

        var pattern = $"%{query.Trim()}%";
        return await _db.Medicines
            .AsNoTracking()
            .Where(m =>
                EF.Functions.ILike(m.Name, pattern) ||
                (m.GenericName != null && EF.Functions.ILike(m.GenericName, pattern)) ||
                (m.Category != null && EF.Functions.ILike(m.Category.CategoryName, pattern)) ||
                (m.Manufacturer != null && EF.Functions.ILike(m.Manufacturer.Name, pattern)))
            .OrderBy(m => m.Name)
            .Select(m => new MedicineListItemDto
            {
                MedicineId           = m.MedicineId,
                Name                 = m.Name,
                GenericName          = m.GenericName,
                CategoryName         = m.Category != null ? m.Category.CategoryName : null,
                ManufacturerName     = m.Manufacturer != null ? m.Manufacturer.Name : null,
                TotalStockInBaseUnit = m.Batches.Where(b => b.ExpiryDate.Date >= DateTime.UtcNow.Date && b.QuantityInBaseUnit > 0).Sum(b => b.QuantityInBaseUnit),
                ReorderLevel         = m.ReorderLevel,
                IsActive             = m.IsActive,
                RequiresPrescription = m.RequiresPrescription,
                UpdatedAt            = m.UpdatedAt,
                DefaultUnitName      = m.Units
                    .Where(u => u.IsDefault && u.IsActive)
                    .Select(u => u.UnitOfMeasure.Name)
                    .FirstOrDefault(),
                DefaultUnitPrice     = m.Units
                    .Where(u => u.IsDefault && u.IsActive)
                    .Select(u => (decimal?)u.UnitPrice)
                    .FirstOrDefault(),
                Units = m.Units
                    .Where(u => u.IsActive)
                    .OrderByDescending(u => u.IsDefault)
                    .ThenBy(u => u.UnitOfMeasure.Name)
                    .Select(u => new MedicineUnitDto
                    {
                        MedicineUnitId         = u.MedicineUnitId,
                        MedicineId             = u.MedicineId,
                        UnitOfMeasureId        = u.UnitOfMeasureId,
                        UomName                = u.UnitOfMeasure.Name,
                        UomSymbol              = u.UnitOfMeasure.Symbol,
                        ConversionFactorToBase = u.ConversionFactorToBase,
                        IsBaseUnit             = u.IsBaseUnit,
                        IsDefault              = u.IsDefault,
                        IsActive               = u.IsActive,
                        CanPurchase            = u.CanPurchase,
                        CanSell                = u.CanSell,
                        CostPrice              = u.CostPrice,
                        UnitPrice              = u.UnitPrice,
                        Mrp                    = u.Mrp,
                        UpdatedAt              = u.UpdatedAt
                    })
                    .ToList()
            })
            .ToListAsync();
    }

    public async Task<MedicineResponseDto?> GetByIdAsync(int id)
    {
        return await _db.Medicines
            .AsNoTracking()
            .Where(m => m.MedicineId == id)
            .Select(m => new MedicineResponseDto
            {
                MedicineId           = m.MedicineId,
                Name                 = m.Name,
                GenericName          = m.GenericName,
                CategoryId           = m.CategoryId,
                CategoryName         = m.Category != null ? m.Category.CategoryName : null,
                ManufacturerId       = m.ManufacturerId,
                ManufacturerName     = m.Manufacturer != null ? m.Manufacturer.Name : null,
                ReorderLevel         = m.ReorderLevel,
                RequiresPrescription = m.RequiresPrescription,
                IsActive             = m.IsActive,
                TotalStockInBaseUnit = m.Batches.Where(b => b.ExpiryDate.Date >= DateTime.UtcNow.Date && b.QuantityInBaseUnit > 0).Sum(b => b.QuantityInBaseUnit),
                CreatedAt            = m.CreatedAt,
                UpdatedAt            = m.UpdatedAt,
                Units = m.Units
                    .OrderByDescending(u => u.IsDefault)
                    .ThenBy(u => u.UnitOfMeasure.Name)
                    .Select(u => new MedicineUnitDto
                    {
                        MedicineUnitId         = u.MedicineUnitId,
                        MedicineId             = u.MedicineId,
                        UnitOfMeasureId        = u.UnitOfMeasureId,
                        UomName                = u.UnitOfMeasure.Name,
                        UomSymbol              = u.UnitOfMeasure.Symbol,
                        ConversionFactorToBase = u.ConversionFactorToBase,
                        IsBaseUnit             = u.IsBaseUnit,
                        IsDefault              = u.IsDefault,
                        IsActive               = u.IsActive,
                        CanPurchase            = u.CanPurchase,
                        CanSell                = u.CanSell,
                        CostPrice              = u.CostPrice,
                        UnitPrice              = u.UnitPrice,
                        Mrp                    = u.Mrp,
                        UpdatedAt              = u.UpdatedAt
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync();
    }

    public async Task<Medicine?> GetEntityByIdAsync(int id)
    {
        return await _db.Medicines.FindAsync(id);
    }

    public async Task<bool> ExistsByNameAsync(string name, int? excludeId = null)
    {
        // The global query filter in ApplicationDbContext automatically filters by tenant
        var tenantId = _tenantProvider.TenantId;
        Console.WriteLine($"[DEBUG] ExistsByNameAsync - TenantId: {tenantId}, Name: {name}");
        
        var result = await _db.Medicines
            .AnyAsync(m => m.Name.ToLower() == name.ToLower()
                          && (excludeId == null || m.MedicineId != excludeId));
        
        Console.WriteLine($"[DEBUG] ExistsByNameAsync - Result: {result}");
        return result;
    }

    public async Task<int> CountAsync()
    {
        return await _db.Medicines.CountAsync();
    }

    public async Task<Medicine> CreateAsync(Medicine medicine)
    {
        _db.Medicines.Add(medicine);
        await _db.SaveChangesAsync();
        return medicine;
    }

    /// <summary>
    /// Persists the medicine and all rows in <see cref="Medicine.Units"/> in a single
    /// <see cref="Microsoft.EntityFrameworkCore.DbContext.SaveChangesAsync"/> call so the
    /// operation is atomic — either both the Medicine and its units are written or neither is.
    /// </summary>
    public async Task<Medicine> CreateWithUnitsAsync(Medicine medicine)
    {
        // Adding the aggregate root is enough: EF Core will track the Units
        // collection via the navigation property and INSERT them in the same transaction.
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
            var existing = await _db.MedicineUnits
                .Where(mu => mu.MedicineId == medicineId)
                .ToListAsync();

            _db.MedicineUnits.RemoveRange(existing);
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

    public async Task UpdateStockQuantityAsync(int medicineId, int newTotalQuantity)
    {
        // 1. Fetch non-expired batches for this medicine ordered by expiry date descending
        var batches = await _db.MedicineBatches
            .Where(b => b.MedicineId == medicineId && b.ExpiryDate.Date >= DateTime.UtcNow.Date)
            .OrderByDescending(b => b.ExpiryDate)
            .ToListAsync();

        if (batches.Count > 0)
        {
            var currentTotal = batches.Sum(b => b.QuantityInBaseUnit);
            var difference = newTotalQuantity - currentTotal;

            if (difference == 0) return;

            if (difference > 0)
            {
                // Add the extra quantity to the latest batch
                var latestBatch = batches[0];
                latestBatch.QuantityInBaseUnit += difference;
                latestBatch.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                // Reduce quantity across batches
                var toDeduct = Math.Abs(difference);
                foreach (var batch in batches)
                {
                    if (batch.QuantityInBaseUnit >= toDeduct)
                    {
                        batch.QuantityInBaseUnit -= toDeduct;
                        batch.UpdatedAt = DateTime.UtcNow;
                        toDeduct = 0;
                        break;
                    }
                    else
                    {
                        toDeduct -= batch.QuantityInBaseUnit;
                        batch.QuantityInBaseUnit = 0;
                        batch.UpdatedAt = DateTime.UtcNow;
                    }
                }
            }
        }
        else
        {
            if (newTotalQuantity <= 0) return;

            // Create a default initial batch
            var newBatch = new MedicineBatch
            {
                MedicineId           = medicineId,
                BatchNumber          = $"INIT-{DateTime.UtcNow:yyyyMMdd}-{medicineId}",
                ExpiryDate           = DateTime.UtcNow.AddYears(2),
                QuantityInBaseUnit   = newTotalQuantity,
                CostPricePerBaseUnit = 0,
                ReceivedDate         = DateTime.UtcNow,
                UpdatedAt            = DateTime.UtcNow
            };
            _db.MedicineBatches.Add(newBatch);
        }

        await _db.SaveChangesAsync();
    }
}
