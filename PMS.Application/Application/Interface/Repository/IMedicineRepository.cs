using PMS.Common.DTOs.Medicine;
using PMS.Models.Models;

namespace PMS.Application.Application.Interface.Repository;

public interface IMedicineRepository
{
    Task<IEnumerable<MedicineListItemDto>> GetAllAsync();
    Task<IEnumerable<MedicineListItemDto>> SearchAsync(string query);
    Task<MedicineResponseDto?> GetByIdAsync(int id);

    /// <summary>Returns the tracked entity (no navigation properties) for update operations.</summary>
    Task<Medicine?> GetEntityByIdAsync(int id);

    Task<bool> ExistsByNameAsync(string name, int? excludeId = null);
    Task<int> CountAsync();
    Task<Medicine> CreateAsync(Medicine medicine);
    Task<IReadOnlyList<Medicine>> CreateBulkAsync(IEnumerable<Medicine> medicines);

    /// <summary>
    /// Persists the medicine together with its pre-populated <see cref="Medicine.Units"/>
    /// collection in a single <see cref="Microsoft.EntityFrameworkCore.DbContext.SaveChangesAsync"/> call.
    /// The caller must populate <c>medicine.Units</c> before calling this method.
    /// </summary>
    Task<Medicine> CreateWithUnitsAsync(Medicine medicine);
    Task<Medicine> UpdateAsync(Medicine medicine);
    Task<bool> DeleteAsync(int id);

    /// <summary>
    /// Removes all existing MedicineUnit rows for the medicine and inserts the new ones
    /// in a single transaction.
    /// </summary>
    Task ReplaceUnitsAsync(int medicineId, IEnumerable<MedicineUnit> newUnits);
}
