using PMS.Models.Models;

namespace PMS.Application.Application.Interface.Repository;

public interface IMedicineUnitRepository
{
    /// <summary>Returns all units for a medicine, active or not.</summary>
    Task<IReadOnlyList<MedicineUnit>> GetByMedicineIdAsync(int medicineId);

    /// <summary>Returns active units only, default unit first then alphabetical by name.</summary>
    Task<IReadOnlyList<MedicineUnit>> GetActiveForPosAsync(int medicineId);

    Task<MedicineUnit?> GetByIdAsync(int medicineUnitId);

    /// <summary>Returns true if this (MedicineId, UomId) combination already exists.</summary>
    Task<bool> ExistsAsync(int medicineId, int unitOfMeasureId, int? excludeId = null);

    Task<MedicineUnit> CreateAsync(MedicineUnit unit);
    Task<MedicineUnit> UpdateAsync(MedicineUnit unit);

    /// <summary>
    /// Load a medicine with its Units collection fully populated.
    /// Used before calling Medicine domain methods that require Units to be loaded.
    /// </summary>
    Task<Medicine?> GetMedicineWithUnitsAsync(int medicineId);

    /// <summary>
    /// Load a medicine with Units AND FEFO batches — used by the sale service.
    /// </summary>
    Task<Medicine?> GetMedicineWithUnitsAndFefoStockAsync(int medicineId);

    /// <summary>
    /// Returns the medicine with its units and the total stock per unit
    /// expressed in base units.  Used by GET /api/medicines/{id}/units.
    /// Returns null when the medicine does not exist for the tenant.
    /// </summary>
    Task<(Medicine? Medicine, int TotalStockInBaseUnit)> GetMedicineWithStockTotalsAsync(int medicineId);
}
