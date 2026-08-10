using Microsoft.EntityFrameworkCore;
using PMS.Application.Application.Interface.Repository;
using PMS.Common.DTOs.Sale;
using PMS.Models.Models;
using PMS.Repository.Data;

namespace PMS.Application.Application.Repository;

/// <summary>
/// Data-access only — no business rules live here.
/// All stock deduction and the Sale insert are wrapped in a single DB transaction.
/// </summary>
public class SaleRepository : ISaleRepository
{
    private readonly ApplicationDbContext _db;

    public SaleRepository(ApplicationDbContext db) => _db = db;

    // ── Lookup helpers ─────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task<Medicine?> GetMedicineWithUnitsAndFefoStockAsync(int medicineId) =>
        await _db.Medicines
            .Where(m => m.MedicineId == medicineId && m.IsActive)
            .Select(m => new Medicine
            {
                MedicineId      = m.MedicineId,
                Name            = m.Name,
                TabletsPerStrip = m.TabletsPerStrip,
                StrapsPerBox    = m.StrapsPerBox,
                IsActive        = m.IsActive,
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

    /// <inheritdoc/>
    public async Task<bool> CustomerExistsAsync(int customerId) =>
        await _db.Customers.AnyAsync(c => c.CustomerId == customerId);

    /// <inheritdoc/>
    public async Task<bool> PharmacistExistsAsync(int pharmacistId) =>
        await _db.AppUsers.AnyAsync(u => u.UserId == pharmacistId);

    // ── Transactional save ─────────────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task<Sale> SaveSaleAsync(
        Sale sale,
        IReadOnlyList<(int BatchId, int BaseUnitsToDeduct)> batchDeductions)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync();
        try
        {
            // 1. Deduct base-unit stock from each batch
            foreach (var (batchId, baseUnitsToDeduct) in batchDeductions)
            {
                var batch = await _db.MedicineBatches.FindAsync(batchId)
                    ?? throw new InvalidOperationException(
                        $"Batch {batchId} disappeared during transaction.");

                batch.QuantityInTablets -= baseUnitsToDeduct;

                if (batch.QuantityInTablets < 0)
                    throw new InvalidOperationException(
                        $"Insufficient stock in batch {batch.BatchNumber} (ID {batchId}). " +
                        "The sale cannot be completed because stock would go negative. " +
                        "This may be caused by a concurrent request.");
            }

            // 2. Insert the Sale (SaleItems already in navigation property)
            _db.Sales.Add(sale);

            // 3. Flush and commit
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            return sale;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    // ── Read-back for response DTO ─────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task<SaleResponseDto?> GetSaleWithDetailsAsync(int saleId) =>
        await _db.Sales
            .Where(s => s.SaleId == saleId)
            .AsNoTracking()
            .Select(s => new SaleResponseDto
            {
                SaleId          = s.SaleId,
                InvoiceNumber   = s.InvoiceNumber,
                CustomerId      = s.CustomerId,
                CustomerName    = s.Customer.FullName,
                PharmacistId    = s.PharmacistId,
                PharmacistName  = s.Pharmacist.FullName,
                SaleDate        = s.SaleDate,
                PaymentMode     = s.PaymentMode,
                Subtotal        = s.Subtotal,
                DiscountPercent = s.DiscountPercent,
                DiscountAmount  = s.DiscountAmount,
                TotalAmount     = s.TotalAmount,
                UpdatedAt       = s.UpdatedAt,
                Items = s.SaleItems
                    .Select(si => new SaleItemResponseDto
                    {
                        SaleItemId           = si.SaleItemId,
                        MedicineId           = si.MedicineId,
                        MedicineName         = si.Medicine.Name,
                        BatchId              = si.BatchId,
                        BatchNumber          = si.Batch.BatchNumber,
                        MedicineUnitId       = si.MedicineUnitId,
                        UomName              = si.MedicineUnit.UnitOfMeasure.Name,
                        Quantity             = si.Quantity,
                        BaseQuantityDeducted = si.BaseQuantityDeducted,
                        UnitPrice            = si.UnitPrice,
                        DiscountPercent      = si.DiscountPercent,
                        DiscountAmount       = si.DiscountAmount,
                        LineTotal            = si.LineTotal
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync();

    /// <inheritdoc/>
    public async Task<IReadOnlyList<SaleResponseDto>> GetAllSalesAsync() =>
        await _db.Sales
            .AsNoTracking()
            .OrderByDescending(s => s.SaleDate)
            .Select(s => new SaleResponseDto
            {
                SaleId          = s.SaleId,
                InvoiceNumber   = s.InvoiceNumber,
                CustomerId      = s.CustomerId,
                CustomerName    = s.Customer.FullName,
                PharmacistId    = s.PharmacistId,
                PharmacistName  = s.Pharmacist.FullName,
                SaleDate        = s.SaleDate,
                PaymentMode     = s.PaymentMode,
                Subtotal        = s.Subtotal,
                DiscountPercent = s.DiscountPercent,
                DiscountAmount  = s.DiscountAmount,
                TotalAmount     = s.TotalAmount,
                UpdatedAt       = s.UpdatedAt,
                Items = s.SaleItems
                    .Select(si => new SaleItemResponseDto
                    {
                        SaleItemId           = si.SaleItemId,
                        MedicineId           = si.MedicineId,
                        MedicineName         = si.Medicine.Name,
                        BatchId              = si.BatchId,
                        BatchNumber          = si.Batch.BatchNumber,
                        MedicineUnitId       = si.MedicineUnitId,
                        UomName              = si.MedicineUnit.UnitOfMeasure.Name,
                        Quantity             = si.Quantity,
                        BaseQuantityDeducted = si.BaseQuantityDeducted,
                        UnitPrice            = si.UnitPrice,
                        DiscountPercent      = si.DiscountPercent,
                        DiscountAmount       = si.DiscountAmount,
                        LineTotal            = si.LineTotal
                    })
                    .ToList()
            })
            .ToListAsync();

    // ── Invoice number generation ──────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task<string> GenerateInvoiceNumberAsync()
    {
        var today  = DateTime.UtcNow.Date;
        var prefix = $"INV-{today:yyyyMMdd}-";

        var countToday = await _db.Sales
            .CountAsync(s => s.InvoiceNumber.StartsWith(prefix));

        return $"{prefix}{(countToday + 1):D4}";
    }
}
