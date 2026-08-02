using PMS.Models.Models;

namespace PMS.Application.Application.Interface.Repository;

public interface IMedicineBatchRepository
{
    Task<IEnumerable<MedicineBatch>> GetAllAsync();
    Task<IEnumerable<MedicineBatch>> GetByMedicineIdAsync(int medicineId);
    Task<MedicineBatch?> GetByIdAsync(int id);
    Task<bool> ExistsByBatchNumberAsync(string batchNumber, int? excludeId = null);
    Task<MedicineBatch> CreateAsync(MedicineBatch batch);
    Task<MedicineBatch> UpdateAsync(MedicineBatch batch);
    Task<bool> DeleteAsync(int id);
    Task<IEnumerable<MedicineBatch>> GetNearExpiryAsync(int days);
}
