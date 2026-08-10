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
            .OrderByDescending(mu => mu.IsDefault)
            .ThenBy(mu => mu.UnitOfMeasure.Name)
            .AsNoTracking()
            .Select(mu => new MedicineUnit
            {
                MedicineUnitId         = mu.MedicineUnitId,
                MedicineId             = mu.MedicineId,
                UnitOfMeasureId        = mu.UnitOfMeasureId,
                ConversionFactorToBase = mu.ConversionFactorToBase,
                IsBaseUnit             = mu.IsBaseUnit,
                IsDefault              = mu.IsDefault,
                IsActive               = mu.IsActive,
                UnitPrice              = mu.UnitPrice,
                UpdatedAt              = mu.UpdatedAt,
                UnitOfMeasure = new UnitOfMeasure
                {
                    UnitOfMeasureId = mu.UnitOfMeasure.UnitOfMeasureId,
                    Name            = mu.UnitOfMeasure.Name,
                    Symbol          = mu.UnitOfMeasure.Symbol,
                    IsActive        = mu.UnitOfMeasure.IsActive
                }
            })
            .ToListAsync();

    public async Task<IReadOnlyList<MedicineUnit>> GetActiveForPosAsync(int medicineId) =>
        await _db.MedicineUnits
            .Where(mu => mu.MedicineId == medicineId && mu.IsActive)
            .OrderByDescending(mu => mu.IsDefault)
            .ThenBy(mu => mu.UnitOfMeasure.Name)
            .AsNoTracking()
            .Select(mu => new MedicineUnit
            {
                MedicineUnitId         = mu.MedicineUnitId,
                MedicineId             = mu.MedicineId,
                UnitOfMeasureId        = mu.UnitOfMeasureId,
                ConversionFactorToBase = mu.ConversionFactorToBase,
                IsBaseUnit             = mu.IsBaseUnit,
                IsDefault              = mu.IsDefault,
                IsActive               = mu.IsActive,
                UnitPrice              = mu.UnitPrice,
                UpdatedAt              = mu.UpdatedAt,
                UnitOfMeasure = new UnitOfMeasure
                {
                    UnitOfMeasureId = mu.UnitOfMeasure.UnitOfMeasureId,
                    Name            = mu.UnitOfMeasure.Name,
                    Symbol          = mu.UnitOfMeasure.Symbol,
                    IsActive        = mu.UnitOfMeasure.IsActive
                }
            })
            .ToListAsync();

    public async Task<MedicineUnit?> GetByIdAsync(int medicineUnitId) =>
        await _db.MedicineUnits
            .Where(mu => mu.MedicineUnitId == medicineUnitId)
            .AsNoTracking()
            .Select(mu => new MedicineUnit
            {
                MedicineUnitId         = mu.MedicineUnitId,
                MedicineId             = mu.MedicineId,
                UnitOfMeasureId        = mu.UnitOfMeasureId,
                ConversionFactorToBase = mu.ConversionFactorToBase,
                IsBaseUnit             = mu.IsBaseUnit,
                IsDefault              = mu.IsDefault,
                IsActive               = mu.IsActive,
                UnitPrice              = mu.UnitPrice,
                UpdatedAt              = mu.UpdatedAt,
                UnitOfMeasure = new UnitOfMeasure
                {
                    UnitOfMeasureId = mu.UnitOfMeasure.UnitOfMeasureId,
                    Name            = mu.UnitOfMeasure.Name,
                    Symbol          = mu.UnitOfMeasure.Symbol,
                    IsActive        = mu.UnitOfMeasure.IsActive
                }
            })
            .FirstOrDefaultAsync();

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

    /// <summary>
    /// Returns the medicine with its active Units collection loaded — needed by service domain
    /// methods that call Medicine.ConvertToBaseQuantity, SetDefaultUnit, etc.
    /// Uses Select to avoid Include.
    /// </summary>
    public async Task<Medicine?> GetMedicineWithUnitsAsync(int medicineId) =>
        await _db.Medicines
            .Where(m => m.MedicineId == medicineId && m.IsActive)
            .Select(m => new Medicine
            {
                MedicineId           = m.MedicineId,
                Name                 = m.Name,
                TabletsPerStrip      = m.TabletsPerStrip,
                StrapsPerBox         = m.StrapsPerBox,
                IsActive             = m.IsActive,
                Units = m.Units
                    .Where(u => u.IsActive)
                    .Select(u => new MedicineUnit
                    {
                        MedicineUnitId         = u.MedicineUnitId,
                        MedicineId             = u.MedicineId,
                        UnitOfMeasureId        = u.UnitOfMeasureId,
                        ConversionFactorToBase = u.ConversionFactorToBase,
                        IsBaseUnit             = u.IsBaseUnit,
                        IsDefault              = u.IsDefault,
                        IsActive               = u.IsActive,
                        UnitPrice              = u.UnitPrice,
                        UpdatedAt              = u.UpdatedAt,
                        UnitOfMeasure = new UnitOfMeasure
                        {
                            UnitOfMeasureId = u.UnitOfMeasure.UnitOfMeasureId,
                            Name            = u.UnitOfMeasure.Name,
                            Symbol          = u.UnitOfMeasure.Symbol
                        }
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync();

    /// <summary>
    /// Returns medicine with active Units AND non-expired, in-stock FEFO batches.
    /// Used by the sale service for stock allocation.
    /// </summary>
    public async Task<Medicine?> GetMedicineWithUnitsAndFefoStockAsync(int medicineId) =>
        await _db.Medicines
            .Where(m => m.MedicineId == medicineId && m.IsActive)
            .Select(m => new Medicine
            {
                MedicineId           = m.MedicineId,
                Name                 = m.Name,
                TabletsPerStrip      = m.TabletsPerStrip,
                StrapsPerBox         = m.StrapsPerBox,
                IsActive             = m.IsActive,
                Units = m.Units
                    .Where(u => u.IsActive)
                    .Select(u => new MedicineUnit
                    {
                        MedicineUnitId         = u.MedicineUnitId,
                        MedicineId             = u.MedicineId,
                        UnitOfMeasureId        = u.UnitOfMeasureId,
                        ConversionFactorToBase = u.ConversionFactorToBase,
                        IsBaseUnit             = u.IsBaseUnit,
                        IsDefault              = u.IsDefault,
                        IsActive               = u.IsActive,
                        UnitPrice              = u.UnitPrice,
                        UpdatedAt              = u.UpdatedAt,
                        UnitOfMeasure = new UnitOfMeasure
                        {
                            UnitOfMeasureId = u.UnitOfMeasure.UnitOfMeasureId,
                            Name            = u.UnitOfMeasure.Name,
                            Symbol          = u.UnitOfMeasure.Symbol
                        }
                    })
                    .ToList(),
                Batches = m.Batches
                    .Where(b => b.ExpiryDate.Date > DateTime.UtcNow.Date && b.QuantityInTablets > 0)
                    .OrderBy(b => b.ExpiryDate)
                    .Select(b => new MedicineBatch
                    {
                        BatchId                = b.BatchId,
                        MedicineId             = b.MedicineId,
                        BatchNumber            = b.BatchNumber,
                        ExpiryDate             = b.ExpiryDate,
                        QuantityInTablets      = b.QuantityInTablets,
                        PurchasePricePerTablet = b.PurchasePricePerTablet
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync();
}
