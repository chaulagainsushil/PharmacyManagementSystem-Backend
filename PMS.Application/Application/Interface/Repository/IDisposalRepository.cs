using PMS.Models.Models;

namespace PMS.Application.Application.Interface.Repository;

public interface IDisposalRepository
{
    Task<bool> PharmacistExistsAsync(int pharmacistId);
    Task<MedicineBatch?> GetBatchWithMedicineAsync(int batchId);
    Task<ExpiredMedicineDisposal> SaveDisposalAsync(
        ExpiredMedicineDisposal disposal,
        IReadOnlyList<(int BatchId, int TabletsToDeduct)> deductions);
    Task<ExpiredMedicineDisposal?> GetDisposalWithDetailsAsync(int disposalId);
    Task<IReadOnlyList<ExpiredMedicineDisposal>> GetAllDisposalsAsync();
    Task<string> GenerateDisposalNumberAsync();
}
