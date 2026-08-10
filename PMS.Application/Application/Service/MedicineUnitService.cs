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
        return units.Select(MapToPos).ToList();
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
            UnitPrice               = cmd.UnitPrice
        };

        var created = await _repo.CreateAsync(newUnit);
        var full    = await _repo.GetByIdAsync(created.MedicineUnitId);
        return (true, "Unit attached to medicine.", MapToDto(full!));
    }

    public async Task<(bool Success, string Message, bool IsConflict)> SetDefaultUnitAsync(
        SetDefaultUnitCommand cmd)
    {
        // Load medicine with all its current units
        var medicine = await _repo.GetMedicineWithUnitsAsync(cmd.MedicineId);
        if (medicine is null)
            return (false, $"Medicine with ID {cmd.MedicineId} not found.", false);

        var target = medicine.Units.FirstOrDefault(u => u.MedicineUnitId == cmd.MedicineUnitId);
        if (target is null)
            return (false, $"MedicineUnit {cmd.MedicineUnitId} is not attached to medicine {cmd.MedicineId}.", false);

        // Optimistic concurrency check using the target unit's UpdatedAt
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
        // Read current state (AsNoTracking)
        var unit = await _repo.GetByIdAsync(medicineUnitId);
        if (unit is null)
            return (false, $"MedicineUnit with ID {medicineUnitId} not found.", false, null);

        // Optimistic concurrency check
        if (!ConcurrencyHelper.Matches(unit.UpdatedAt, cmd.UpdatedAt))
            return (false, "The record was modified by another user. Please refresh and try again.", true, null);

        // Build a fresh entity for update (GetByIdAsync is AsNoTracking)
        var updatable = new MedicineUnit
        {
            MedicineUnitId         = unit.MedicineUnitId,
            MedicineId             = unit.MedicineId,
            UnitOfMeasureId        = unit.UnitOfMeasureId,
            ConversionFactorToBase = unit.ConversionFactorToBase,
            IsBaseUnit             = unit.IsBaseUnit,
            IsDefault              = unit.IsDefault,
            IsActive               = unit.IsActive,
            UnitPrice              = cmd.UnitPrice
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
            IsDefault              = false,
            IsActive               = false,
            UnitPrice              = unit.UnitPrice
        };

        await _repo.UpdateAsync(updatable);
        return (true, "Unit deactivated.");
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
        UnitPrice              = mu.UnitPrice,
        UpdatedAt              = mu.UpdatedAt
    };

    private static MedicineUnitForPosDto MapToPos(MedicineUnit mu) => new()
    {
        MedicineUnitId         = mu.MedicineUnitId,
        UomName                = mu.UnitOfMeasure?.Name ?? string.Empty,
        UomSymbol              = mu.UnitOfMeasure?.Symbol ?? string.Empty,
        UnitPrice              = mu.UnitPrice,
        ConversionFactorToBase = mu.ConversionFactorToBase,
        IsDefault              = mu.IsDefault,
        IsBaseUnit             = mu.IsBaseUnit,
        UpdatedAt              = mu.UpdatedAt
    };
}
