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
    public async Task<Medicine?> GetMedicineWithFefoStockAsync(int medicineId)
    {
        // Load medicine with only non-expired, in-stock batches, ordered FEFO
        // (earliest ExpiryDate first so the service drains them in order).
        var medicine = await _db.Medicines
            .Where(m => m.MedicineId == medicineId && m.IsActive)
            .Include(m => m.Batches
                .Where(b => b.ExpiryDate.Date > DateTime.UtcNow.Date && b.QuantityInTablets > 0)
                .OrderBy(b => b.ExpiryDate))
            .FirstOrDefaultAsync();

        return medicine;
    }

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
        IReadOnlyList<(int BatchId, int TabletsToDeduct)> batchDeductions)
    {
        // Begin an explicit transaction so that any mid-write failure rolls back
        // both the batch quantity updates and the new Sale/SaleItem rows.
        await using var transaction = await _db.Database.BeginTransactionAsync();
        try
        {
            // 1. Deduct stock from each batch
            foreach (var (batchId, tabletsToDeduct) in batchDeductions)
            {
                // Use tracked entity to benefit from EF change tracking
                var batch = await _db.MedicineBatches.FindAsync(batchId)
                    ?? throw new InvalidOperationException(
                        $"Batch {batchId} disappeared during transaction.");

                batch.QuantityInTablets -= tabletsToDeduct;
            }

            // 2. Insert the Sale (its SaleItems are already populated via navigation property)
            _db.Sales.Add(sale);

            // 3. Flush everything in one round-trip, then commit
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
            .AsNoTracking()
            .OrderByDescending(s => s.SaleDate)
            .ToListAsync();

    // ── Invoice number generation ──────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task<string> GenerateInvoiceNumberAsync()
    {
        // Format: INV-YYYYMMDD-NNNN  (sequence resets per day)
        var today = DateTime.UtcNow.Date;
        var prefix = $"INV-{today:yyyyMMdd}-";

        // Count how many invoices exist for today to get the next sequence
        var countToday = await _db.Sales
            .CountAsync(s => s.InvoiceNumber.StartsWith(prefix));

        var sequence = (countToday + 1).ToString("D4");
        return $"{prefix}{sequence}";
    }
}
