using PMS.Models.Models;

namespace PMS.Application.Application.Interface.Repository;

public interface IMedicineRepository
{
    Task<IEnumerable<Medicine>> GetAllAsync();
    Task<IEnumerable<Medicine>> SearchAsync(string query);
    Task<Medicine?> GetByIdAsync(int id);
    Task<bool> ExistsByNameAsync(string name, int? excludeId = null);
    Task<Medicine> CreateAsync(Medicine medicine);
    Task<IReadOnlyList<Medicine>> CreateBulkAsync(IEnumerable<Medicine> medicines);
    Task<Medicine> UpdateAsync(Medicine medicine);
    Task<bool> DeleteAsync(int id);

    /// <summary>
    /// Removes all existing MedicineUnit rows for the medicine and inserts the new ones
    /// in a single transaction.
    /// </summary>
    Task ReplaceUnitsAsync(int medicineId, IEnumerable<MedicineUnit> newUnits);
}
