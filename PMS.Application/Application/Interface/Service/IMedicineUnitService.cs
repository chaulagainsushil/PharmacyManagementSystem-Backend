using PMS.Common.DTOs.Uom;

namespace PMS.Application.Application.Interface.Service;

public interface IMedicineUnitService
{
    /// <summary>List all units attached to a medicine (for management views).</summary>
    Task<IReadOnlyList<MedicineUnitDto>> GetByMedicineIdAsync(int medicineId);

    /// <summary>
    /// List active units where CanSell = true for the POS unit-selector, default unit first.
    /// </summary>
    Task<IReadOnlyList<MedicineUnitForPosDto>> GetForPosAsync(int medicineId);

    /// <summary>
    /// List active units where CanPurchase = true for the purchase/batch-receive unit-selector.
    /// </summary>
    Task<IReadOnlyList<MedicineUnitForPurchaseDto>> GetForPurchaseAsync(int medicineId);

    /// <summary>Attach a new UoM to a medicine.</summary>
    Task<(bool Success, string Message, MedicineUnitDto? Data)> AddUnitAsync(AddMedicineUnitCommand cmd);

    /// <summary>
    /// Full update of all mutable fields on an existing MedicineUnit.
    /// Supports changing CanPurchase, CanSell, CostPrice, UnitPrice, Mrp,
    /// ConversionFactorToBase (non-base units only), and IsActive.
    /// </summary>
    Task<(bool Success, string Message, bool IsConflict, MedicineUnitDto? Data)> UpdateAsync(
        int medicineUnitId, UpdateMedicineUnitCommand cmd);

    /// <summary>Set which unit is the POS default for a medicine.</summary>
    Task<(bool Success, string Message, bool IsConflict)> SetDefaultUnitAsync(SetDefaultUnitCommand cmd);

    /// <summary>Update only the selling price for an existing MedicineUnit (quick-price endpoint).</summary>
    Task<(bool Success, string Message, bool IsConflict, MedicineUnitDto? Data)> UpdatePriceAsync(
        int medicineUnitId, UpdateMedicineUnitPriceCommand cmd);

    /// <summary>Deactivate a unit (soft-delete). Base units cannot be deactivated.</summary>
    Task<(bool Success, string Message)> DeactivateAsync(int medicineUnitId);

    /// <summary>
    /// Convenience endpoint: attach a base unit and an optional sub unit to a medicine in one call.
    /// When <paramref name="cmd"/>.UseAutoCalculatedPrice is true the sub-unit's selling price is
    /// computed server-side as BaseUnit.UnitPrice / SubUnit.ConversionFactorToBase.
    /// </summary>
    Task<(bool Success, string Message, IReadOnlyList<MedicineUnitDto>? Data)> AddDualUnitAsync(
        int medicineId, AddDualUnitCommand cmd);

    /// <summary>
    /// Return all units for a medicine with computed TotalInBaseUnits
    /// (TotalStockInBaseUnit for each unit expressed in that unit's own terms).
    /// </summary>
    Task<(bool Found, MedicineUnitsWithTotalsResponse? Data)> GetUnitsWithTotalsAsync(int medicineId);
}
