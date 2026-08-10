using PMS.Common.DTOs.Disposal;
using PMS.Models.Models;

namespace PMS.Application.Application.Interface.Repository;

public interface IDisposalRepository
{
    Task<bool> PharmacistExistsAsync(int pharmacistId);

    /// <summary>Returns batch with MedicineId and QuantityInTablets for validation — no Include needed.</summary>
    Task<MedicineBatch?> GetBatchAsync(int batchId);

    Task<ExpiredMedicineDisposal> SaveDisposalAsync(
        ExpiredMedicineDisposal disposal,
        IReadOnlyList<(int BatchId, int TabletsToDeduct)> deductions);

    Task<DisposalResponseDto?> GetDisposalWithDetailsAsync(int disposalId);
    Task<IReadOnlyList<DisposalResponseDto>> GetAllDisposalsAsync();
    Task<string> GenerateDisposalNumberAsync();
}
