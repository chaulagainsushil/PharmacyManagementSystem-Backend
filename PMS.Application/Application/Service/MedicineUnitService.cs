using PMS.Application.Application.Interface.Repository;
using PMS.Application.Application.Interface.Service;
using PMS.Common;
using PMS.Common.DTOs.Uom;
using PMS.Models.Models;

namespace PMS.Application.Application.Service;

public class MedicineUnitService : IMedicineUnitService
{
    private readonly IMedicineUnitRepository _repo;
    private readonly IUnitOfMeasureRepository _uomRepo;

    public MedicineUnitService(
        IMedicineUnitRepository repo,
        IUnitOfMeasureRepository uomRepo)
    {
        _repo    = repo;
        _uomRepo = uomRepo;
    }

    // ── Queries ───────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<MedicineUnitDto>> GetByMedicineIdAsync(int medicineId)
    {
        var units = await _repo.GetByMedicineIdAsync(medicineId);
        return units.Select(MapToDto).ToList();
    }

    public async Task<IReadOnlyList<MedicineUnitForPosDto>> GetForPosAsync(int medicineId)
    {
        var units = await _repo.GetActiveForPosAsync(medicineId);
        // Only return units that can be sold
        return units.Where(u => u.CanSell).Select(MapToPos).ToList();
    }

    public async Task<IReadOnlyList<MedicineUnitForPurchaseDto>> GetForPurchaseAsync(int medicineId)
    {
        var units = await _repo.GetActiveForPosAsync(medicineId);
        // Only return units that can be used for purchasing
        return units.Where(u => u.CanPurchase).Select(MapToPurchase).ToList();
    }

    // ── Commands ──────────────────────────────────────────────────────────────

    public async Task<(bool Success, string Message, MedicineUnitDto? Data)> AddUnitAsync(
        AddMedicineUnitCommand cmd)
    {
        // Validate the UoM exists
        var uom = await _uomRepo.GetByIdAsync(cmd.UnitOfMeasureId);
        if (uom is null)
            return (false, $"Unit of measure with ID {cmd.UnitOfMeasureId} not found.", null);

        // Prevent duplicate (Medicine, UoM) pairs
        if (await _repo.ExistsAsync(cmd.MedicineId, cmd.UnitOfMeasureId))
            return (false, $"This medicine already has a '{uom.Name}' unit attached.", null);

        // Validate conversion factor
        if (cmd.ConversionFactorToBase <= 0)
            return (false, "ConversionFactorToBase must be greater than 0.", null);

        if (cmd.IsBaseUnit && cmd.ConversionFactorToBase != 1m)
            return (false, "The base unit must have ConversionFactorToBase = 1.", null);

        // If marking as base unit, load medicine and clear existing base unit flag first
        if (cmd.IsBaseUnit)
        {
            var medicine = await _repo.GetMedicineWithUnitsAsync(cmd.MedicineId);
            if (medicine is null)
                return (false, $"Medicine with ID {cmd.MedicineId} not found.", null);

            foreach (var existingUnit in medicine.Units.Where(u => u.IsBaseUnit))
            {
                existingUnit.IsBaseUnit = false;
                await _repo.UpdateAsync(existingUnit);
            }
        }

        // If marking as default, clear existing default flag
        if (cmd.IsDefault)
        {
            var medicine = await _repo.GetMedicineWithUnitsAsync(cmd.MedicineId);
            if (medicine is null)
                return (false, $"Medicine with ID {cmd.MedicineId} not found.", null);

            foreach (var existingUnit in medicine.Units.Where(u => u.IsDefault))
            {
                existingUnit.IsDefault = false;
                await _repo.UpdateAsync(existingUnit);
            }
        }

        var newUnit = new MedicineUnit
        {
            MedicineId              = cmd.MedicineId,
            UnitOfMeasureId         = cmd.UnitOfMeasureId,
            ConversionFactorToBase  = cmd.IsBaseUnit ? 1m : cmd.ConversionFactorToBase,
            IsBaseUnit              = cmd.IsBaseUnit,
            IsDefault               = cmd.IsDefault,
            IsActive                = true,
            CanPurchase             = cmd.CanPurchase,
            CanSell                 = cmd.CanSell,
            CostPrice               = cmd.CostPrice,
            UnitPrice               = cmd.UnitPrice,
            Mrp                     = cmd.Mrp
        };

        var created = await _repo.CreateAsync(newUnit);
        var full    = await _repo.GetByIdAsync(created.MedicineUnitId);
        return (true, "Unit attached to medicine.", MapToDto(full!));
    }

    /// <summary>
    /// Full update of all mutable fields on a MedicineUnit in one call.
    /// Cannot change the base unit's ConversionFactorToBase (always 1).
    /// </summary>
    public async Task<(bool Success, string Message, bool IsConflict, MedicineUnitDto? Data)> UpdateAsync(
        int medicineUnitId, UpdateMedicineUnitCommand cmd)
    {
        var unit = await _repo.GetByIdAsync(medicineUnitId);
        if (unit is null)
            return (false, $"MedicineUnit with ID {medicineUnitId} not found.", false, null);

        // Optimistic concurrency
        if (!ConcurrencyHelper.Matches(unit.UpdatedAt, cmd.UpdatedAt))
            return (false, "The record was modified by another user. Please refresh and try again.", true, null);

        // Base unit conversion factor is always 1 — disallow changes
        if (unit.IsBaseUnit && cmd.ConversionFactorToBase != 1m)
            return (false, "The base unit's ConversionFactorToBase cannot be changed (must remain 1).", false, null);

        // Cannot deactivate the base unit
        if (unit.IsBaseUnit && !cmd.IsActive)
            return (false, "Cannot deactivate the base unit of a medicine.", false, null);

        // Build updated entity
        var updatable = new MedicineUnit
        {
            MedicineUnitId         = unit.MedicineUnitId,
            MedicineId             = unit.MedicineId,
            UnitOfMeasureId        = unit.UnitOfMeasureId,
            ConversionFactorToBase = unit.IsBaseUnit ? 1m : cmd.ConversionFactorToBase,
            IsBaseUnit             = unit.IsBaseUnit,
            IsDefault              = unit.IsDefault,
            IsActive               = cmd.IsActive,
            CanPurchase            = cmd.CanPurchase,
            CanSell                = cmd.CanSell,
            CostPrice              = cmd.CostPrice,
            UnitPrice              = cmd.UnitPrice,
            Mrp                    = cmd.Mrp
        };

        // If deactivating a default unit, clear the IsDefault flag
        if (!cmd.IsActive && unit.IsDefault)
            updatable.IsDefault = false;

        var updated = await _repo.UpdateAsync(updatable);
        var full    = await _repo.GetByIdAsync(updated.MedicineUnitId);
        return (true, "Medicine unit updated.", false, MapToDto(full!));
    }

    public async Task<(bool Success, string Message, bool IsConflict)> SetDefaultUnitAsync(
        SetDefaultUnitCommand cmd)
    {
        var medicine = await _repo.GetMedicineWithUnitsAsync(cmd.MedicineId);
        if (medicine is null)
            return (false, $"Medicine with ID {cmd.MedicineId} not found.", false);

        var target = medicine.Units.FirstOrDefault(u => u.MedicineUnitId == cmd.MedicineUnitId);
        if (target is null)
            return (false, $"MedicineUnit {cmd.MedicineUnitId} is not attached to medicine {cmd.MedicineId}.", false);

        if (!ConcurrencyHelper.Matches(target.UpdatedAt, cmd.UpdatedAt))
            return (false, "The record was modified by another user. Please refresh and try again.", true);

        if (!target.IsActive)
            return (false, "Cannot set an inactive unit as the default.", false);

        // Unset any current default
        foreach (var u in medicine.Units.Where(u => u.IsDefault && u.MedicineUnitId != cmd.MedicineUnitId))
        {
            u.IsDefault = false;
            await _repo.UpdateAsync(u);
        }

        target.IsDefault = true;
        await _repo.UpdateAsync(target);

        return (true, $"Default unit updated to '{target.UnitOfMeasure?.Name ?? target.MedicineUnitId.ToString()}'.", false);
    }

    public async Task<(bool Success, string Message, bool IsConflict, MedicineUnitDto? Data)> UpdatePriceAsync(
        int medicineUnitId, UpdateMedicineUnitPriceCommand cmd)
    {
        var unit = await _repo.GetByIdAsync(medicineUnitId);
        if (unit is null)
            return (false, $"MedicineUnit with ID {medicineUnitId} not found.", false, null);

        if (cmd.UpdatedAt.HasValue && !ConcurrencyHelper.Matches(unit.UpdatedAt, cmd.UpdatedAt))
            return (false, "The record was modified by another user. Please refresh and try again.", true, null);

        var updatable = new MedicineUnit
        {
            MedicineUnitId         = unit.MedicineUnitId,
            MedicineId             = unit.MedicineId,
            UnitOfMeasureId        = unit.UnitOfMeasureId,
            ConversionFactorToBase = unit.ConversionFactorToBase,
            IsBaseUnit             = unit.IsBaseUnit,
            IsDefault              = unit.IsDefault,
            IsActive               = unit.IsActive,
            CanPurchase            = unit.CanPurchase,
            CanSell                = unit.CanSell,
            CostPrice              = unit.CostPrice,
            UnitPrice              = cmd.UnitPrice,   // only the selling price is updated
            Mrp                    = unit.Mrp
        };

        var updated = await _repo.UpdateAsync(updatable);
        var full    = await _repo.GetByIdAsync(updated.MedicineUnitId);
        return (true, "Unit price updated.", false, MapToDto(full!));
    }

    public async Task<(bool Success, string Message)> DeactivateAsync(int medicineUnitId)
    {
        var unit = await _repo.GetByIdAsync(medicineUnitId);
        if (unit is null)
            return (false, $"MedicineUnit with ID {medicineUnitId} not found.");

        if (unit.IsBaseUnit)
            return (false, "Cannot deactivate the base unit of a medicine.");

        var updatable = new MedicineUnit
        {
            MedicineUnitId         = unit.MedicineUnitId,
            MedicineId             = unit.MedicineId,
            UnitOfMeasureId        = unit.UnitOfMeasureId,
            ConversionFactorToBase = unit.ConversionFactorToBase,
            IsBaseUnit             = unit.IsBaseUnit,
            IsDefault              = false,   // deactivated units cannot be default
            IsActive               = false,
            CanPurchase            = unit.CanPurchase,
            CanSell                = unit.CanSell,
            CostPrice              = unit.CostPrice,
            UnitPrice              = unit.UnitPrice,
            Mrp                    = unit.Mrp
        };

        await _repo.UpdateAsync(updatable);
        return (true, "Unit deactivated.");
    }

    /// <summary>
    /// Convenience endpoint: attach a base unit and an optional sub unit in one call.
    /// Enforces exactly one base unit, validates conversion factors, prevents duplicates.
    /// When UseAutoCalculatedPrice is true, the sub-unit price is computed server-side.
    /// </summary>
    public async Task<(bool Success, string Message, IReadOnlyList<MedicineUnitDto>? Data)> AddDualUnitAsync(
        int medicineId, AddDualUnitCommand cmd)
    {
        // Validate base unit UoM exists
        var baseUom = await _uomRepo.GetByIdAsync(cmd.BaseUnit.UnitOfMeasureId);
        if (baseUom is null)
            return (false, $"Base unit UoM with ID {cmd.BaseUnit.UnitOfMeasureId} not found.", null);

        // If sub unit provided, validate it too
        UnitOfMeasure? subUom = null;
        if (cmd.SubUnit is not null)
        {
            subUom = await _uomRepo.GetByIdAsync(cmd.SubUnit.UnitOfMeasureId);
            if (subUom is null)
                return (false, $"Sub unit UoM with ID {cmd.SubUnit.UnitOfMeasureId} not found.", null);

            // Base and sub cannot be the same
            if (cmd.BaseUnit.UnitOfMeasureId == cmd.SubUnit.UnitOfMeasureId)
                return (false, "Base unit and sub unit cannot be the same UoM.", null);

            // Validate sub unit's conversion factor
            if (cmd.SubUnit.ConversionFactorToBase <= 0)
                return (false, "Sub unit ConversionFactorToBase must be greater than 0.", null);
        }

        // Check for duplicates
        if (await _repo.ExistsAsync(medicineId, cmd.BaseUnit.UnitOfMeasureId))
            return (false, $"This medicine already has '{baseUom.Name}' attached as a unit.", null);

        if (cmd.SubUnit is not null && await _repo.ExistsAsync(medicineId, cmd.SubUnit.UnitOfMeasureId))
            return (false, $"This medicine already has '{subUom?.Name ?? "this unit"}' attached as a unit.", null);

        // Load medicine to manage existing base unit flag
        var medicine = await _repo.GetMedicineWithUnitsAsync(medicineId);
        if (medicine is null)
            return (false, $"Medicine with ID {medicineId} not found.", null);

        // Clear any existing base unit flag (only one base unit allowed)
        foreach (var existingUnit in medicine.Units.Where(u => u.IsBaseUnit))
        {
            existingUnit.IsBaseUnit = false;
            await _repo.UpdateAsync(existingUnit);
        }

        // Create and insert base unit
        var baseUnit = new MedicineUnit
        {
            MedicineId              = medicineId,
            UnitOfMeasureId         = cmd.BaseUnit.UnitOfMeasureId,
            ConversionFactorToBase  = 1m,
            IsBaseUnit              = true,
            IsDefault               = cmd.BaseUnit.IsDefault,
            IsActive                = true,
            CanPurchase             = cmd.BaseUnit.CanPurchase,
            CanSell                 = cmd.BaseUnit.CanSell,
            CostPrice               = cmd.BaseUnit.CostPrice,
            UnitPrice               = cmd.BaseUnit.UnitPrice,
            Mrp                     = cmd.BaseUnit.Mrp
        };

        // If base is default, clear any existing default
        if (cmd.BaseUnit.IsDefault)
        {
            foreach (var existingUnit in medicine.Units.Where(u => u.IsDefault))
            {
                existingUnit.IsDefault = false;
                await _repo.UpdateAsync(existingUnit);
            }
        }

        await _repo.CreateAsync(baseUnit);

        // Create and insert sub unit if provided
        if (cmd.SubUnit is not null)
        {
            decimal subUnitPrice = cmd.UseAutoCalculatedPrice
                ? cmd.BaseUnit.UnitPrice / cmd.SubUnit.ConversionFactorToBase
                : cmd.SubUnit.UnitPrice;

            var subUnit = new MedicineUnit
            {
                MedicineId              = medicineId,
                UnitOfMeasureId         = cmd.SubUnit.UnitOfMeasureId,
                ConversionFactorToBase  = cmd.SubUnit.ConversionFactorToBase,
                IsBaseUnit              = false,
                IsDefault               = false,
                IsActive                = true,
                CanPurchase             = cmd.SubUnit.CanPurchase,
                CanSell                 = cmd.SubUnit.CanSell,
                CostPrice               = cmd.SubUnit.CostPrice,
                UnitPrice               = subUnitPrice,
                Mrp                     = cmd.SubUnit.Mrp
            };

            await _repo.CreateAsync(subUnit);
        }

        // Return all units for this medicine
        var allUnits = await _repo.GetByMedicineIdAsync(medicineId);
        var dtos = allUnits.Select(MapToDto).ToList();

        return (true, "Units attached successfully.", dtos);
    }

    /// <summary>
    /// Returns all units for a medicine with computed TotalInBaseUnits.
    /// TotalInBaseUnits = medicine's total stock × unit's conversion factor.
    /// </summary>
    public async Task<(bool Found, MedicineUnitsWithTotalsResponse? Data)> GetUnitsWithTotalsAsync(int medicineId)
    {
        var (medicine, totalStockInBaseUnit) = await _repo.GetMedicineWithStockTotalsAsync(medicineId);

        if (medicine is null)
            return (false, null);

        var unitDtos = medicine.Units.Select(mu => new MedicineUnitWithTotalsDto
        {
            MedicineUnitId         = mu.MedicineUnitId,
            MedicineId             = mu.MedicineId,
            UnitOfMeasureId        = mu.UnitOfMeasureId,
            UomName                = mu.UnitOfMeasure?.Name ?? string.Empty,
            UomSymbol              = mu.UnitOfMeasure?.Symbol ?? string.Empty,
            ConversionFactorToBase = mu.ConversionFactorToBase,
            IsBaseUnit             = mu.IsBaseUnit,
            IsDefault              = mu.IsDefault,
            IsActive               = mu.IsActive,
            CanPurchase            = mu.CanPurchase,
            CanSell                = mu.CanSell,
            CostPrice              = mu.CostPrice,
            UnitPrice              = mu.UnitPrice,
            Mrp                    = mu.Mrp,
            UpdatedAt              = mu.UpdatedAt,
            // TotalInBaseUnits = total stock expressed in this unit's terms
            TotalInBaseUnits = totalStockInBaseUnit / (decimal)mu.ConversionFactorToBase
        }).ToList();

        var response = new MedicineUnitsWithTotalsResponse
        {
            MedicineId   = medicine.MedicineId,
            MedicineName = medicine.Name,
            Units        = unitDtos
        };

        return (true, response);
    }

    // ── Mapping ───────────────────────────────────────────────────────────────

    private static MedicineUnitDto MapToDto(MedicineUnit mu) => new()
    {
        MedicineUnitId         = mu.MedicineUnitId,
        MedicineId             = mu.MedicineId,
        UnitOfMeasureId        = mu.UnitOfMeasureId,
        UomName                = mu.UnitOfMeasure?.Name ?? string.Empty,
        UomSymbol              = mu.UnitOfMeasure?.Symbol ?? string.Empty,
        ConversionFactorToBase = mu.ConversionFactorToBase,
        IsBaseUnit             = mu.IsBaseUnit,
        IsDefault              = mu.IsDefault,
        IsActive               = mu.IsActive,
        CanPurchase            = mu.CanPurchase,
        CanSell                = mu.CanSell,
        CostPrice              = mu.CostPrice,
        UnitPrice              = mu.UnitPrice,
        Mrp                    = mu.Mrp,
        UpdatedAt              = mu.UpdatedAt
    };

    private static MedicineUnitForPosDto MapToPos(MedicineUnit mu) => new()
    {
        MedicineUnitId         = mu.MedicineUnitId,
        UomName                = mu.UnitOfMeasure?.Name ?? string.Empty,
        UomSymbol              = mu.UnitOfMeasure?.Symbol ?? string.Empty,
        UnitPrice              = mu.UnitPrice,
        Mrp                    = mu.Mrp,
        ConversionFactorToBase = mu.ConversionFactorToBase,
        IsDefault              = mu.IsDefault,
        IsBaseUnit             = mu.IsBaseUnit,
        UpdatedAt              = mu.UpdatedAt
    };

    private static MedicineUnitForPurchaseDto MapToPurchase(MedicineUnit mu) => new()
    {
        MedicineUnitId         = mu.MedicineUnitId,
        UomName                = mu.UnitOfMeasure?.Name ?? string.Empty,
        UomSymbol              = mu.UnitOfMeasure?.Symbol ?? string.Empty,
        CostPrice              = mu.CostPrice,
        ConversionFactorToBase = mu.ConversionFactorToBase,
        IsBaseUnit             = mu.IsBaseUnit,
        UpdatedAt              = mu.UpdatedAt
    };
}
