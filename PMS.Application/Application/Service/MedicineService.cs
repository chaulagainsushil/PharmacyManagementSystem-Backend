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
        if (await _repo.ExistsByNameAsync(dto.Name))
            return (false, $"Medicine '{dto.Name}' already exists.", null);

        // Check plan medicine limit
        var limitError = await CheckMedicineLimitAsync(1);
        if (limitError is not null)
            return (false, limitError, null);

        // Validate inline units if provided
        if (dto.Units is { Count: > 0 })
        {
            var unitError = ValidateInlineUnits(dto.Units);
            if (unitError is not null)
                return (false, unitError, null);
        }

        var medicine = new Medicine
        {
            Name                 = dto.Name.Trim(),
            GenericName          = dto.GenericName?.Trim(),
            CategoryId           = dto.CategoryId,
            ManufacturerId       = dto.ManufacturerId,
            TabletsPerStrip      = dto.TabletsPerStrip,
            StrapsPerBox         = dto.StrapsPerBox,
            StripPrice           = dto.StripPrice,
            TabletPrice          = dto.TabletPrice,
            ReorderLevel         = dto.ReorderLevel,
            RequiresPrescription = dto.RequiresPrescription,
            IsActive             = dto.IsActive,
            CreatedAt            = DateTime.UtcNow
        };

        var created = await _repo.CreateAsync(medicine);

        // Attach units inline if provided
        if (dto.Units is { Count: > 0 })
        {
            var units = BuildMedicineUnits(created.MedicineId, dto.Units);
            await _repo.ReplaceUnitsAsync(created.MedicineId, units);
        }

        var full = await _repo.GetByIdAsync(created.MedicineId);
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
            // Fail the entire bulk operation
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
                    { Index = i, Success = false, Message = $"Medicine '{name}' already exists." });
                continue;
            }

            if (dto.Units is { Count: > 0 })
            {
                var unitError = ValidateInlineUnits(dto.Units);
                if (unitError is not null)
                {
                    results.Add(new BulkCreateMedicineItemResultDto
                        { Index = i, Success = false, Message = unitError });
                    continue;
                }
            }

            toInsert.Add((i, new Medicine
            {
                Name                 = name,
                GenericName          = dto.GenericName?.Trim(),
                CategoryId           = dto.CategoryId,
                ManufacturerId       = dto.ManufacturerId,
                TabletsPerStrip      = dto.TabletsPerStrip,
                StrapsPerBox         = dto.StrapsPerBox,
                StripPrice           = dto.StripPrice,
                TabletPrice          = dto.TabletPrice,
                ReorderLevel         = dto.ReorderLevel,
                RequiresPrescription = dto.RequiresPrescription,
                IsActive             = dto.IsActive,
                CreatedAt            = DateTime.UtcNow
            }, dto.Units));
        }

        if (toInsert.Count > 0)
        {
            await _repo.CreateBulkAsync(toInsert.Select(t => t.Entity).ToList());

            foreach (var (idx, entity, unitDtos) in toInsert)
            {
                if (unitDtos is { Count: > 0 })
                {
                    var units = BuildMedicineUnits(entity.MedicineId, unitDtos);
                    await _repo.ReplaceUnitsAsync(entity.MedicineId, units);
                }

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
        // 1. Read current DTO (includes UpdatedAt)
        var existing = await _repo.GetByIdAsync(id);
        if (existing is null)
            return (false, $"Medicine with ID {id} not found.", false, null);

        // 2. Optimistic concurrency check
        if (!ConcurrencyHelper.Matches(existing.UpdatedAt, dto.UpdatedAt))
            return (false, "The record was modified by another user. Please refresh and try again.", true, null);

        // 3. Business rule: duplicate name
        if (await _repo.ExistsByNameAsync(dto.Name, excludeId: id))
            return (false, $"Medicine '{dto.Name}' already exists.", false, null);

        // 4. Validate inline units if provided
        if (dto.Units is { Count: > 0 })
        {
            var unitError = ValidateInlineUnits(dto.Units);
            if (unitError is not null)
                return (false, unitError, false, null);
        }

        // 5. Fetch tracked entity and mutate
        var entity = await _repo.GetEntityByIdAsync(id);
        if (entity is null)
            return (false, $"Medicine with ID {id} not found.", false, null);

        entity.Name                 = dto.Name.Trim();
        entity.GenericName          = dto.GenericName?.Trim();
        entity.CategoryId           = dto.CategoryId;
        entity.ManufacturerId       = dto.ManufacturerId;
        entity.TabletsPerStrip      = dto.TabletsPerStrip;
        entity.StrapsPerBox         = dto.StrapsPerBox;
        entity.StripPrice           = dto.StripPrice;
        entity.TabletPrice          = dto.TabletPrice;
        entity.ReorderLevel         = dto.ReorderLevel;
        entity.RequiresPrescription = dto.RequiresPrescription;
        entity.IsActive             = dto.IsActive;

        await _repo.UpdateAsync(entity);

        // Replace units only when explicitly supplied
        if (dto.Units is { Count: > 0 })
        {
            var units = BuildMedicineUnits(id, dto.Units);
            await _repo.ReplaceUnitsAsync(id, units);
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

    private static string? ValidateInlineUnits(List<CreateMedicineUnitDto> units)
    {
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

        return null;
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private static List<MedicineUnit> BuildMedicineUnits(int medicineId, List<CreateMedicineUnitDto> dtos) =>
        dtos.Select(u => new MedicineUnit
        {
            MedicineId             = medicineId,
            UnitOfMeasureId        = u.UnitOfMeasureId,
            ConversionFactorToBase = u.IsBaseUnit ? 1m : u.ConversionFactorToBase,
            IsBaseUnit             = u.IsBaseUnit,
            IsDefault              = u.IsDefault,
            IsActive               = true,
            UnitPrice              = u.UnitPrice
        }).ToList();
}
