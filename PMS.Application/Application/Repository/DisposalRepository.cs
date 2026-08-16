using Microsoft.EntityFrameworkCore;
using PMS.Application.Application.Interface.Repository;
using PMS.Common.DTOs.Disposal;
using PMS.Models.Models;
using PMS.Repository.Data;

namespace PMS.Application.Application.Repository;

public class DisposalRepository : IDisposalRepository
{
    private readonly ApplicationDbContext _db;

    public DisposalRepository(ApplicationDbContext db) => _db = db;

    public async Task<bool> PharmacistExistsAsync(int pharmacistId) =>
        await _db.AppUsers.AnyAsync(u => u.UserId == pharmacistId);

    public async Task<MedicineBatch?> GetBatchAsync(int batchId) =>
        await _db.MedicineBatches
            .Where(b => b.BatchId == batchId)
            .Select(b => new MedicineBatch
            {
                BatchId            = b.BatchId,
                MedicineId         = b.MedicineId,
                BatchNumber        = b.BatchNumber,
                QuantityInBaseUnit = b.QuantityInBaseUnit,
                ExpiryDate         = b.ExpiryDate
            })
            .FirstOrDefaultAsync();

    public async Task<ExpiredMedicineDisposal> SaveDisposalAsync(
        ExpiredMedicineDisposal disposal,
        IReadOnlyList<(int BatchId, int TabletsToDeduct)> deductions)
    {
        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            foreach (var (batchId, tablets) in deductions)
            {
                var batch = await _db.MedicineBatches.FindAsync(batchId)
                    ?? throw new InvalidOperationException($"Batch {batchId} not found.");

                batch.QuantityInBaseUnit -= tablets;

                if (batch.QuantityInBaseUnit < 0)
                    throw new InvalidOperationException(
                        $"Insufficient stock in batch {batch.BatchNumber} (ID {batchId}). " +
                        "The disposal cannot be completed because stock would go negative. " +
                        "This may be caused by a concurrent request.");
            }

            _db.Set<ExpiredMedicineDisposal>().Add(disposal);
            await _db.SaveChangesAsync();
            await tx.CommitAsync();
            return disposal;
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task<DisposalResponseDto?> GetDisposalWithDetailsAsync(int disposalId) =>
        await _db.Set<ExpiredMedicineDisposal>()
            .Where(d => d.DisposalId == disposalId)
            .AsNoTracking()
            .Select(d => new DisposalResponseDto
            {
                DisposalId     = d.DisposalId,
                DisposalNumber = d.DisposalNumber,
                PharmacistId   = d.PharmacistId,
                PharmacistName = d.Pharmacist.FullName,
                DisposalDate   = d.DisposalDate,
                UpdatedAt      = d.UpdatedAt,
                Items = d.Items
                    .Select(i => new DisposalItemResponseDto
                    {
                        DisposalItemId    = i.DisposalItemId,
                        MedicineId        = i.MedicineId,
                        MedicineName      = i.Medicine.Name,
                        BatchId           = i.BatchId,
                        BatchNumber       = i.Batch.BatchNumber,
                        QuantityInTablets = i.QuantityInTablets,
                        Reason            = i.Reason
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync();

    public async Task<IReadOnlyList<DisposalResponseDto>> GetAllDisposalsAsync() =>
        await _db.Set<ExpiredMedicineDisposal>()
            .AsNoTracking()
            .OrderByDescending(d => d.DisposalDate)
            .Select(d => new DisposalResponseDto
            {
                DisposalId     = d.DisposalId,
                DisposalNumber = d.DisposalNumber,
                PharmacistId   = d.PharmacistId,
                PharmacistName = d.Pharmacist.FullName,
                DisposalDate   = d.DisposalDate,
                UpdatedAt      = d.UpdatedAt,
                Items = d.Items
                    .Select(i => new DisposalItemResponseDto
                    {
                        DisposalItemId    = i.DisposalItemId,
                        MedicineId        = i.MedicineId,
                        MedicineName      = i.Medicine.Name,
                        BatchId           = i.BatchId,
                        BatchNumber       = i.Batch.BatchNumber,
                        QuantityInTablets = i.QuantityInTablets,
                        Reason            = i.Reason
                    })
                    .ToList()
            })
            .ToListAsync();

    public async Task<string> GenerateDisposalNumberAsync()
    {
        var today  = DateTime.UtcNow.Date;
        var prefix = $"DSP-{today:yyyyMMdd}-";
        var count  = await _db.Set<ExpiredMedicineDisposal>()
            .CountAsync(d => d.DisposalNumber.StartsWith(prefix));
        return $"{prefix}{(count + 1):D4}";
    }
}
