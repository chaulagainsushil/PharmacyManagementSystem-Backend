using Microsoft.EntityFrameworkCore;
using PMS.Application.Application.Interface.Repository;
using PMS.Models.Models;
using PMS.Repository.Data;

namespace PMS.Application.Application.Repository;

public class DisposalRepository : IDisposalRepository
{
    private readonly ApplicationDbContext _db;

    public DisposalRepository(ApplicationDbContext db) => _db = db;

    public async Task<bool> PharmacistExistsAsync(int pharmacistId) =>
        await _db.AppUsers.AnyAsync(u => u.UserId == pharmacistId);

    public async Task<MedicineBatch?> GetBatchWithMedicineAsync(int batchId) =>
        await _db.MedicineBatches
            .Include(b => b.Medicine)
            .FirstOrDefaultAsync(b => b.BatchId == batchId);

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
                batch.QuantityInTablets -= tablets;
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

    public async Task<ExpiredMedicineDisposal?> GetDisposalWithDetailsAsync(int disposalId) =>
        await _db.Set<ExpiredMedicineDisposal>()
            .Include(d => d.Pharmacist)
            .Include(d => d.Items).ThenInclude(i => i.Medicine)
            .Include(d => d.Items).ThenInclude(i => i.Batch)
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.DisposalId == disposalId);

    public async Task<IReadOnlyList<ExpiredMedicineDisposal>> GetAllDisposalsAsync() =>
        await _db.Set<ExpiredMedicineDisposal>()
            .Include(d => d.Pharmacist)
            .Include(d => d.Items).ThenInclude(i => i.Medicine)
            .Include(d => d.Items).ThenInclude(i => i.Batch)
            .AsNoTracking()
            .OrderByDescending(d => d.DisposalDate)
            .ToListAsync();

    public async Task<string> GenerateDisposalNumberAsync()
    {
        var today = DateTime.UtcNow.Date;
        var prefix = $"DSP-{today:yyyyMMdd}-";
        var countToday = await _db.Set<ExpiredMedicineDisposal>()
            .CountAsync(d => d.DisposalNumber.StartsWith(prefix));
        return $"{prefix}{(countToday + 1):D4}";
    }
}
