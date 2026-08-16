using PMS.Common.DTOs.MedicineBatch;
using PMS.Models.Models;

namespace PMS.Application.Application.Interface.Repository;

public interface IMedicineBatchRepository
{
    Task<IEnumerable<MedicineBatchListItemDto>> GetAllAsync();
    Task<IEnumerable<MedicineBatchListItemDto>> GetByMedicineIdAsync(int medicineId);
    Task<MedicineBatchResponseDto?> GetByIdAsync(int id);

    /// <summary>Returns the tracked entity for update operations.</summary>
    Task<MedicineBatch?> GetEntityByIdAsync(int id);

    Task<bool> ExistsByBatchNumberAsync(string batchNumber, int? excludeId = null);
    Task<MedicineBatch> CreateAsync(MedicineBatch batch);
    Task<MedicineBatch> UpdateAsync(MedicineBatch batch);
    Task<bool> DeleteAsync(int id);
    Task<IEnumerable<MedicineBatchListItemDto>> GetNearExpiryAsync(int days);

    /// <summary>
    /// Returns the active MedicineUnit with its UnitOfMeasure loaded — used during batch creation
    /// to validate CanPurchase and resolve the quantity/cost conversion.
    /// Returns null if the unit is not found or inactive.
    /// </summary>
    Task<MedicineUnit?> GetPurchaseUnitAsync(int medicineId, int medicineUnitId);
}
