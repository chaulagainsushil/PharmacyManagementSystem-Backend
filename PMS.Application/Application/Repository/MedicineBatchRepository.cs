using Microsoft.EntityFrameworkCore;
using PMS.Application.Application.Interface.Repository;
using PMS.Common.DTOs.MedicineBatch;
using PMS.Models.Models;
using PMS.Repository.Data;

namespace PMS.Application.Application.Repository;

public class MedicineBatchRepository : IMedicineBatchRepository
{
    private readonly ApplicationDbContext _db;

    public MedicineBatchRepository(ApplicationDbContext db) => _db = db;

    public async Task<IEnumerable<MedicineBatchListItemDto>> GetAllAsync() =>
        await _db.MedicineBatches
            .AsNoTracking()
            .OrderBy(b => b.ExpiryDate)
            .Select(b => new MedicineBatchListItemDto
            {
                BatchId                        = b.BatchId,
                MedicineId                     = b.MedicineId,
                MedicineName                   = b.Medicine.Name,
                BatchNumber                    = b.BatchNumber,
                SupplierName                   = b.Supplier != null ? b.Supplier.Name : null,
                ExpiryDate                     = b.ExpiryDate,
                QuantityInBaseUnit             = b.QuantityInBaseUnit,
                CostPricePerBaseUnit           = b.CostPricePerBaseUnit,
                PurchaseUnitName               = b.PurchaseMedicineUnit != null
                                                     ? b.PurchaseMedicineUnit.UnitOfMeasure.Name
                                                     : null,
                PurchaseQuantityInPurchaseUnit = b.PurchaseQuantityInPurchaseUnit,
                UpdatedAt                      = b.UpdatedAt,
                IsExpired                      = b.ExpiryDate.Date < DateTime.UtcNow.Date
            })
            .ToListAsync();

    public async Task<IEnumerable<MedicineBatchListItemDto>> GetByMedicineIdAsync(int medicineId) =>
        await _db.MedicineBatches
            .AsNoTracking()
            .Where(b => b.MedicineId == medicineId)
            .OrderBy(b => b.ExpiryDate)
            .Select(b => new MedicineBatchListItemDto
            {
                BatchId                        = b.BatchId,
                MedicineId                     = b.MedicineId,
                MedicineName                   = b.Medicine.Name,
                BatchNumber                    = b.BatchNumber,
                SupplierName                   = b.Supplier != null ? b.Supplier.Name : null,
                ExpiryDate                     = b.ExpiryDate,
                QuantityInBaseUnit             = b.QuantityInBaseUnit,
                CostPricePerBaseUnit           = b.CostPricePerBaseUnit,
                PurchaseUnitName               = b.PurchaseMedicineUnit != null
                                                     ? b.PurchaseMedicineUnit.UnitOfMeasure.Name
                                                     : null,
                PurchaseQuantityInPurchaseUnit = b.PurchaseQuantityInPurchaseUnit,
                UpdatedAt                      = b.UpdatedAt,
                IsExpired                      = b.ExpiryDate.Date < DateTime.UtcNow.Date
            })
            .ToListAsync();

    public async Task<MedicineBatchResponseDto?> GetByIdAsync(int id) =>
        await _db.MedicineBatches
            .AsNoTracking()
            .Where(b => b.BatchId == id)
            .Select(b => new MedicineBatchResponseDto
            {
                BatchId                        = b.BatchId,
                MedicineId                     = b.MedicineId,
                MedicineName                   = b.Medicine.Name,
                BatchNumber                    = b.BatchNumber,
                SupplierId                     = b.SupplierId,
                SupplierName                   = b.Supplier != null ? b.Supplier.Name : null,
                ManufactureDate                = b.ManufactureDate,
                ExpiryDate                     = b.ExpiryDate,
                QuantityInBaseUnit             = b.QuantityInBaseUnit,
                CostPricePerBaseUnit           = b.CostPricePerBaseUnit,
                PurchaseMedicineUnitId         = b.PurchaseMedicineUnitId,
                PurchaseUnitName               = b.PurchaseMedicineUnit != null
                                                     ? b.PurchaseMedicineUnit.UnitOfMeasure.Name
                                                     : null,
                PurchaseQuantityInPurchaseUnit = b.PurchaseQuantityInPurchaseUnit,
                ReceivedDate                   = b.ReceivedDate,
                UpdatedAt                      = b.UpdatedAt,
                IsExpired                      = b.ExpiryDate.Date < DateTime.UtcNow.Date
            })
            .FirstOrDefaultAsync();

    public async Task<MedicineBatch?> GetEntityByIdAsync(int id) =>
        await _db.MedicineBatches.FindAsync(id);

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

    public async Task<IEnumerable<MedicineBatchListItemDto>> GetNearExpiryAsync(int days) =>
        await _db.MedicineBatches
            .AsNoTracking()
            .Where(b => b.ExpiryDate.Date > DateTime.UtcNow.Date
                     && b.ExpiryDate.Date <= DateTime.UtcNow.Date.AddDays(days)
                     && b.QuantityInBaseUnit > 0)
            .OrderBy(b => b.ExpiryDate)
            .Select(b => new MedicineBatchListItemDto
            {
                BatchId                        = b.BatchId,
                MedicineId                     = b.MedicineId,
                MedicineName                   = b.Medicine.Name,
                BatchNumber                    = b.BatchNumber,
                SupplierName                   = b.Supplier != null ? b.Supplier.Name : null,
                ExpiryDate                     = b.ExpiryDate,
                QuantityInBaseUnit             = b.QuantityInBaseUnit,
                CostPricePerBaseUnit           = b.CostPricePerBaseUnit,
                PurchaseUnitName               = b.PurchaseMedicineUnit != null
                                                     ? b.PurchaseMedicineUnit.UnitOfMeasure.Name
                                                     : null,
                PurchaseQuantityInPurchaseUnit = b.PurchaseQuantityInPurchaseUnit,
                IsExpired                      = false  // near-expiry batches are not yet expired
            })
            .ToListAsync();

    /// <summary>
    /// Fetches the MedicineUnit with its conversion factor — used during batch creation
    /// to resolve quantity and cost price when purchasing in a non-base unit.
    /// Returns null if the unit is not found or does not belong to the medicine.
    /// </summary>
    public async Task<MedicineUnit?> GetPurchaseUnitAsync(int medicineId, int medicineUnitId) =>
        await _db.MedicineUnits
            .AsNoTracking()
            .Where(mu => mu.MedicineUnitId == medicineUnitId
                      && mu.MedicineId == medicineId
                      && mu.IsActive)
            .Select(mu => new MedicineUnit
            {
                MedicineUnitId         = mu.MedicineUnitId,
                MedicineId             = mu.MedicineId,
                UnitOfMeasureId        = mu.UnitOfMeasureId,
                ConversionFactorToBase = mu.ConversionFactorToBase,
                IsBaseUnit             = mu.IsBaseUnit,
                CanPurchase            = mu.CanPurchase,
                CanSell                = mu.CanSell,
                CostPrice              = mu.CostPrice,
                UnitPrice              = mu.UnitPrice,
                Mrp                    = mu.Mrp,
                IsActive               = mu.IsActive,
                UnitOfMeasure = new UnitOfMeasure
                {
                    UnitOfMeasureId = mu.UnitOfMeasure.UnitOfMeasureId,
                    Name            = mu.UnitOfMeasure.Name,
                    Symbol          = mu.UnitOfMeasure.Symbol
                }
            })
            .FirstOrDefaultAsync();
}
