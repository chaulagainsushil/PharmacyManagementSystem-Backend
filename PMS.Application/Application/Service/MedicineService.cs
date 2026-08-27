using PMS.Application.Application.Interface.Repository;
using PMS.Application.Application.Interface.Service;
using PMS.Common;
using PMS.Common.DTOs.Medicine;
using PMS.Common.DTOs.Uom;
using PMS.Models.Interfaces;
using PMS.Models.Models;

namespace PMS.Application.Application.Service;

public class MedicineService : IMedicineService
{
    private readonly IMedicineRepository    _repo;
    private readonly ISubscriptionRepository _subscriptionRepo;
    private readonly ITenantProvider        _tenantProvider;

    public MedicineService(
        IMedicineRepository    repo,
        ISubscriptionRepository subscriptionRepo,
        ITenantProvider        tenantProvider)
    {
        _repo             = repo;
        _subscriptionRepo = subscriptionRepo;
        _tenantProvider   = tenantProvider;
    }

    public async Task<IEnumerable<MedicineListItemDto>> GetAllAsync()
    {
        return await _repo.GetAllAsync();
    }

    public async Task<IEnumerable<MedicineListItemDto>> SearchAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return await GetAllAsync();

        return await _repo.SearchAsync(query);
    }

    public async Task<MedicineResponseDto?> GetByIdAsync(int id)
    {
        return await _repo.GetByIdAsync(id);
    }

    public async Task<(bool Success, string Message, MedicineResponseDto? Data)> CreateAsync(
        CreateMedicineDto dto)
    {
        var tenantId = _tenantProvider.TenantId;
        Console.WriteLine($"[DEBUG] CreateAsync - TenantId: {tenantId}, Medicine: {dto.Name}");
        
        // Trim and normalize the name
        var normalizedName = dto.Name?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedName))
            return (false, "Medicine name cannot be empty.", null);

        var exists = await _repo.ExistsByNameAsync(normalizedName);
        Console.WriteLine($"[DEBUG] ExistsByNameAsync returned: {exists}");
        
        if (exists)
            return (false, $"A medicine named '{normalizedName}' already exists in your inventory. Please use a different name or search for the existing medicine to update it.", null);

        // Check plan medicine limit
        var limitError = await CheckMedicineLimitAsync(1);
        if (limitError is not null)
            return (false, limitError, null);

        // Auto-fix single unit if needed before validation
        if (dto.Units != null && dto.Units.Count == 1)
        {
            var u = dto.Units[0];
            u.IsBaseUnit = true;
            u.IsDefault = true;
            u.ConversionFactorToBase = 1m;
        }

        // Units are required — validate them
        var unitError = ValidateInlineUnits(dto.Units);
        if (unitError is not null)
            return (false, unitError, null);

        var medicine = new Medicine
        {
            Name                 = normalizedName,
            GenericName          = dto.GenericName?.Trim(),
            CategoryId           = dto.CategoryId,
            ManufacturerId       = dto.ManufacturerId,
            ReorderLevel         = dto.ReorderLevel,
            RequiresPrescription = dto.RequiresPrescription,
            IsActive             = dto.IsActive,
            CreatedAt            = DateTime.UtcNow
        };

        // Build units and add to the navigation collection so EF persists
        // the Medicine + all MedicineUnit rows in a single SaveChangesAsync.
        foreach (var unitDto in dto.Units)
        {
            medicine.Units.Add(new MedicineUnit
            {
                UnitOfMeasureId        = unitDto.UnitOfMeasureId,
                ConversionFactorToBase = unitDto.IsBaseUnit ? 1m : unitDto.ConversionFactorToBase,
                IsBaseUnit             = unitDto.IsBaseUnit,
                IsDefault              = unitDto.IsDefault,
                IsActive               = true,
                CanPurchase            = unitDto.CanPurchase,
                CanSell                = unitDto.CanSell,
                CostPrice              = unitDto.CostPrice,
                UnitPrice              = unitDto.UnitPrice,
                Mrp                    = unitDto.Mrp
            });
        }

        var created = await _repo.CreateWithUnitsAsync(medicine);
        var full    = await _repo.GetByIdAsync(created.MedicineId);
        return (true, "Medicine created successfully.", full);
    }

    public async Task<BulkCreateMedicineResultDto> CreateBulkAsync(IReadOnlyList<CreateMedicineDto> dtos)
    {
        var results  = new List<BulkCreateMedicineItemResultDto>(dtos.Count);
        var toInsert = new List<(int Index, Medicine Entity, List<CreateMedicineUnitDto>? Units)>();

        var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Check plan medicine limit upfront for the whole batch
        var limitError = await CheckMedicineLimitAsync(dtos.Count);
        if (limitError is not null)
        {
            for (int i = 0; i < dtos.Count; i++)
                results.Add(new BulkCreateMedicineItemResultDto { Index = i, Success = false, Message = limitError });

            return new BulkCreateMedicineResultDto
            {
                TotalRequested = dtos.Count,
                TotalCreated   = 0,
                TotalFailed    = dtos.Count,
                Results        = results
            };
        }

        for (int i = 0; i < dtos.Count; i++)
        {
            var dto  = dtos[i];
            var name = dto.Name?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(name))
            {
                results.Add(new BulkCreateMedicineItemResultDto
                    { Index = i, Success = false, Message = "Medicine name is required." });
                continue;
            }

            if (!seenNames.Add(name))
            {
                results.Add(new BulkCreateMedicineItemResultDto
                    { Index = i, Success = false, Message = $"Duplicate name '{name}' within this batch." });
                continue;
            }

            if (await _repo.ExistsByNameAsync(name))
            {
                results.Add(new BulkCreateMedicineItemResultDto
                    { Index = i, Success = false, Message = $"Medicine '{name}' already exists in your inventory." });
                continue;
            }

            // Units are required
            if (dto.Units is not { Count: > 0 })
            {
                results.Add(new BulkCreateMedicineItemResultDto
                    { Index = i, Success = false, Message = "At least one unit is required." });
                continue;
            }

            var unitError = ValidateInlineUnits(dto.Units);
            if (unitError is not null)
            {
                results.Add(new BulkCreateMedicineItemResultDto
                    { Index = i, Success = false, Message = unitError });
                continue;
            }

            var entity = new Medicine
            {
                Name                 = name,
                GenericName          = dto.GenericName?.Trim(),
                CategoryId           = dto.CategoryId,
                ManufacturerId       = dto.ManufacturerId,
                ReorderLevel         = dto.ReorderLevel,
                RequiresPrescription = dto.RequiresPrescription,
                IsActive             = dto.IsActive,
                CreatedAt            = DateTime.UtcNow
            };

            foreach (var unitDto in dto.Units)
            {
                entity.Units.Add(new MedicineUnit
                {
                    UnitOfMeasureId        = unitDto.UnitOfMeasureId,
                    ConversionFactorToBase = unitDto.IsBaseUnit ? 1m : unitDto.ConversionFactorToBase,
                    IsBaseUnit             = unitDto.IsBaseUnit,
                    IsDefault              = unitDto.IsDefault,
                    IsActive               = true,
                    CanPurchase            = unitDto.CanPurchase,
                    CanSell                = unitDto.CanSell,
                    CostPrice              = unitDto.CostPrice,
                    UnitPrice              = unitDto.UnitPrice,
                    Mrp                    = unitDto.Mrp
                });
            }

            toInsert.Add((i, entity, dto.Units));
        }

        if (toInsert.Count > 0)
        {
            await _repo.CreateBulkAsync(toInsert.Select(t => t.Entity).ToList());

            foreach (var (idx, entity, _) in toInsert)
            {
                var full = await _repo.GetByIdAsync(entity.MedicineId);
                results.Add(new BulkCreateMedicineItemResultDto
                {
                    Index   = idx,
                    Success = true,
                    Message = "Created successfully.",
                    Data    = full
                });
            }
        }

        results.Sort((a, b) => a.Index.CompareTo(b.Index));

        return new BulkCreateMedicineResultDto
        {
            TotalRequested = dtos.Count,
            TotalCreated   = results.Count(r => r.Success),
            TotalFailed    = results.Count(r => !r.Success),
            Results        = results
        };
    }

    public async Task<(bool Success, string Message, bool IsConflict, MedicineResponseDto? Data)> UpdateAsync(
        int id, UpdateMedicineDto dto)
    {
        var existing = await _repo.GetByIdAsync(id);
        if (existing is null)
            return (false, $"Medicine with ID {id} not found.", false, null);

        if (!ConcurrencyHelper.Matches(existing.UpdatedAt, dto.UpdatedAt))
            return (false, "The record was modified by another user. Please refresh and try again.", true, null);

        // Trim and normalize the name
        var normalizedName = dto.Name?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedName))
            return (false, "Medicine name cannot be empty.", false, null);

        if (await _repo.ExistsByNameAsync(normalizedName, excludeId: id))
            return (false, $"A medicine named '{normalizedName}' already exists in your inventory. Please use a different name.", false, null);

        if (dto.Units is { Count: > 0 })
        {
            var unitError = ValidateInlineUnits(dto.Units);
            if (unitError is not null)
                return (false, unitError, false, null);
        }

        var entity = await _repo.GetEntityByIdAsync(id);
        if (entity is null)
            return (false, $"Medicine with ID {id} not found.", false, null);

        entity.Name                 = normalizedName;
        entity.GenericName          = dto.GenericName?.Trim();
        entity.CategoryId           = dto.CategoryId;
        entity.ManufacturerId       = dto.ManufacturerId;
        entity.ReorderLevel         = dto.ReorderLevel;
        entity.RequiresPrescription = dto.RequiresPrescription;
        entity.IsActive             = dto.IsActive;

        await _repo.UpdateAsync(entity);

        if (dto.TotalStockInBaseUnit.HasValue)
        {
            await _repo.UpdateStockQuantityAsync(id, dto.TotalStockInBaseUnit.Value);
        }

        if (dto.Units is { Count: > 0 })
        {
            var newUnits = dto.Units.Select(u => new MedicineUnit
            {
                MedicineId             = id,
                UnitOfMeasureId        = u.UnitOfMeasureId,
                ConversionFactorToBase = u.IsBaseUnit ? 1m : u.ConversionFactorToBase,
                IsBaseUnit             = u.IsBaseUnit,
                IsDefault              = u.IsDefault,
                IsActive               = true,
                CanPurchase            = u.CanPurchase,
                CanSell                = u.CanSell,
                CostPrice              = u.CostPrice,
                UnitPrice              = u.UnitPrice,
                Mrp                    = u.Mrp
            });
            await _repo.ReplaceUnitsAsync(id, newUnits);
        }

        var full = await _repo.GetByIdAsync(id);
        return (true, "Medicine updated successfully.", false, full);
    }

    public async Task<(bool Success, string Message)> DeleteAsync(int id)
    {
        var exists = await _repo.GetByIdAsync(id);
        if (exists is null)
            return (false, $"Medicine with ID {id} not found.");

        await _repo.DeleteAsync(id);
        return (true, "Medicine deleted successfully.");
    }

    // ── Validation ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns an error message if adding <paramref name="addCount"/> medicines would exceed
    /// the plan's MedicineLimit. Returns null if the operation is allowed.
    /// </summary>
    private async Task<string?> CheckMedicineLimitAsync(int addCount)
    {
        if (!_tenantProvider.HasTenant) return null;

        var plan = await _subscriptionRepo.GetPlanDefinitionByTenantAsync(_tenantProvider.TenantId);
        if (plan?.MedicineLimit is null) return null; // unlimited

        var current = await _repo.CountAsync();
        if (current + addCount > plan.MedicineLimit)
        {
            return $"Your {plan.PlanType} plan allows a maximum of {plan.MedicineLimit} medicines. " +
                   $"You currently have {current}. Please upgrade your plan to add more medicines.";
        }

        return null;
    }

    private static string? ValidateInlineUnits(List<CreateMedicineUnitDto>? units)
    {
        if (units == null || units.Count == 0)
            return "At least one unit is required.";

        var baseCount    = units.Count(u => u.IsBaseUnit);
        var defaultCount = units.Count(u => u.IsDefault);

        if (baseCount != 1)
            return $"Exactly one unit must be marked as the base unit (IsBaseUnit=true). Found {baseCount}.";

        if (defaultCount != 1)
            return $"Exactly one unit must be marked as the default (IsDefault=true). Found {defaultCount}.";

        var duplicateUom = units
            .GroupBy(u => u.UnitOfMeasureId)
            .FirstOrDefault(g => g.Count() > 1);

        if (duplicateUom is not null)
            return $"Duplicate UnitOfMeasureId {duplicateUom.Key} in the units list.";

        var baseUnit = units.Single(u => u.IsBaseUnit);
        if (baseUnit.ConversionFactorToBase != 1m)
            return "The base unit must have ConversionFactorToBase = 1.";

        // Validate: every unit must have a positive ConversionFactorToBase
        var badFactor = units.FirstOrDefault(u => u.ConversionFactorToBase <= 0);
        if (badFactor is not null)
            return $"ConversionFactorToBase must be greater than 0 for all units. " +
                   $"UnitOfMeasureId {badFactor.UnitOfMeasureId} has factor {badFactor.ConversionFactorToBase}.";

        return null;
    }
}
