using PMS.Application.Application.Interface.Repository;
using PMS.Application.Application.Interface.Service;
using PMS.Common;
using PMS.Common.DTOs.Uom;
using PMS.Models.Models;

namespace PMS.Application.Application.Service;

public class UnitOfMeasureService : IUnitOfMeasureService
{
    private readonly IUnitOfMeasureRepository _repo;

    public UnitOfMeasureService(IUnitOfMeasureRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<UomDto>> GetAllAsync(bool includeInactive = false)
    {
        var items = await _repo.GetAllAsync(includeInactive);
        return items.Select(Map).ToList();
    }

    public async Task<UomDto?> GetByIdAsync(int id)
    {
        var uom = await _repo.GetByIdAsync(id);
        return uom is null ? null : Map(uom);
    }

    public async Task<(bool Success, string Message, UomDto? Data)> CreateAsync(CreateUomDto dto)
    {
        if (await _repo.ExistsByNameAsync(dto.Name))
            return (false, $"A unit of measure named '{dto.Name}' already exists.", null);

        var uom = new UnitOfMeasure
        {
            Name     = dto.Name.Trim(),
            Symbol   = dto.Symbol.Trim(),
            IsActive = true
        };

        var created = await _repo.CreateAsync(uom);
        return (true, "Unit of measure created.", Map(created));
    }

    public async Task<(bool Success, string Message, bool IsConflict, UomDto? Data)> UpdateAsync(
        int id, UpdateUomDto dto)
    {
        // 1. Read current entity (UoM repo returns entity directly — AsNoTracking)
        var existing = await _repo.GetByIdAsync(id);
        if (existing is null)
            return (false, $"Unit of measure with ID {id} not found.", false, null);

        // 2. Optimistic concurrency check
        if (!ConcurrencyHelper.Matches(existing.UpdatedAt, dto.UpdatedAt))
            return (false, "The record was modified by another user. Please refresh and try again.", true, null);

        // 3. Business rule: duplicate name
        if (await _repo.ExistsByNameAsync(dto.Name, excludeId: id))
            return (false, $"A unit of measure named '{dto.Name}' already exists.", false, null);

        // 4. Build a new entity instance for update (since GetByIdAsync returns AsNoTracking)
        var entity = new UnitOfMeasure
        {
            UnitOfMeasureId = id,
            Name            = dto.Name.Trim(),
            Symbol          = dto.Symbol.Trim(),
            IsActive        = dto.IsActive
        };

        var updated = await _repo.UpdateAsync(entity);
        return (true, "Unit of measure updated.", false, Map(updated));
    }

    public async Task<(bool Success, string Message)> DeleteAsync(int id)
    {
        var uom = await _repo.GetByIdAsync(id);
        if (uom is null)
            return (false, $"Unit of measure with ID {id} not found.");

        var deleted = await _repo.DeleteAsync(id);
        return deleted
            ? (true, "Unit of measure deleted.")
            : (false, "Delete failed.");
    }

    // ── Mapping ───────────────────────────────────────────────────────────────

    private static UomDto Map(UnitOfMeasure u) => new()
    {
        UnitOfMeasureId = u.UnitOfMeasureId,
        Name            = u.Name,
        Symbol          = u.Symbol,
        IsActive        = u.IsActive,
        UpdatedAt       = u.UpdatedAt
    };
}
