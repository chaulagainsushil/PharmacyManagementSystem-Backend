using Microsoft.EntityFrameworkCore;
using PMS.Application.Application.Interface.Repository;
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
            // Load active MedicineUnits with their UoM master so the service can
            // call Medicine.ConvertToBaseQuantity and Medicine.GetUnitPrice.
            .Include(m => m.Units.Where(u => u.IsActive))
                .ThenInclude(u => u.UnitOfMeasure)
            // FEFO batches: non-expired, in-stock, sorted earliest expiry first
            .Include(m => m.Batches
                .Where(b => b.ExpiryDate.Date > DateTime.UtcNow.Date && b.QuantityInTablets > 0)
                .OrderBy(b => b.ExpiryDate))
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
    public async Task<Sale?> GetSaleWithDetailsAsync(int saleId) =>
        await _db.Sales
            .Include(s => s.Customer)
            .Include(s => s.Pharmacist)
            .Include(s => s.SaleItems)
                .ThenInclude(si => si.Medicine)
            .Include(s => s.SaleItems)
                .ThenInclude(si => si.Batch)
            .Include(s => s.SaleItems)
                .ThenInclude(si => si.MedicineUnit)
                    .ThenInclude(mu => mu.UnitOfMeasure)
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.SaleId == saleId);

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Sale>> GetAllSalesAsync() =>
        await _db.Sales
            .Include(s => s.Customer)
            .Include(s => s.Pharmacist)
            .Include(s => s.SaleItems)
                .ThenInclude(si => si.Medicine)
            .Include(s => s.SaleItems)
                .ThenInclude(si => si.Batch)
            .Include(s => s.SaleItems)
                .ThenInclude(si => si.MedicineUnit)
                    .ThenInclude(mu => mu.UnitOfMeasure)
            .AsNoTracking()
            .OrderByDescending(s => s.SaleDate)
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
