using PMS.Common.DTOs.Uom;

namespace PMS.Application.Application.Interface.Service;

public interface IMedicineUnitService
{
    /// <summary>List all units attached to a medicine (for management views).</summary>
    Task<IReadOnlyList<MedicineUnitDto>> GetByMedicineIdAsync(int medicineId);

    /// <summary>List active units for the POS unit-selector, default unit first.</summary>
    Task<IReadOnlyList<MedicineUnitForPosDto>> GetForPosAsync(int medicineId);

    /// <summary>Attach a new UoM to a medicine.</summary>
    Task<(bool Success, string Message, MedicineUnitDto? Data)> AddUnitAsync(AddMedicineUnitCommand cmd);

    /// <summary>Set which unit is the POS default for a medicine.</summary>
    Task<(bool Success, string Message, bool IsConflict)> SetDefaultUnitAsync(SetDefaultUnitCommand cmd);

    /// <summary>Update the price for an existing MedicineUnit.</summary>
    Task<(bool Success, string Message, bool IsConflict, MedicineUnitDto? Data)> UpdatePriceAsync(
        int medicineUnitId, UpdateMedicineUnitPriceCommand cmd);

    /// <summary>Deactivate a unit (soft-delete). Base units cannot be deactivated.</summary>
    Task<(bool Success, string Message)> DeactivateAsync(int medicineUnitId);
}
