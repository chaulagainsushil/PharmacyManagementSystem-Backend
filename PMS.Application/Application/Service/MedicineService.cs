using PMS.Application.Application.Interface.Repository;
using PMS.Application.Application.Interface.Service;
using PMS.Common.DTOs.Medicine;
using PMS.Common.DTOs.Uom;
using PMS.Models.Models;

namespace PMS.Application.Application.Service;

public class MedicineService : IMedicineService
{
    private readonly IMedicineRepository _repo;

    public MedicineService(IMedicineRepository repo)
    {
        _repo = repo;
    }

    public async Task<IEnumerable<MedicineListItemDto>> GetAllAsync()
    {
        var medicines = await _repo.GetAllAsync();
        return medicines.Select(MapToListItem);
    }

    public async Task<IEnumerable<MedicineListItemDto>> SearchAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return await GetAllAsync();

        var medicines = await _repo.SearchAsync(query);
        return medicines.Select(MapToListItem);
    }

    public async Task<MedicineResponseDto?> GetByIdAsync(int id)
    {
        var medicine = await _repo.GetByIdAsync(id);
        if (medicine is null) return null;

        return MapToResponse(medicine);
    }

    public async Task<(bool Success, string Message, MedicineResponseDto? Data)> CreateAsync(
        CreateMedicineDto dto)
    {
        if (await _repo.ExistsByNameAsync(dto.Name))
            return (false, $"Medicine '{dto.Name}' already exists.", null);

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
        return (true, "Medicine created successfully.", MapToResponse(full!));
    }

    public async Task<BulkCreateMedicineResultDto> CreateBulkAsync(IReadOnlyList<CreateMedicineDto> dtos)
    {
        var results  = new List<BulkCreateMedicineItemResultDto>(dtos.Count);
        var toInsert = new List<(int Index, Medicine Entity, List<CreateMedicineUnitDto>? Units)>();

        var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

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
            var saved = await _repo.CreateBulkAsync(toInsert.Select(t => t.Entity).ToList());

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
                    Data    = MapToResponse(full!)
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

    public async Task<(bool Success, string Message, MedicineResponseDto? Data)> UpdateAsync(
        int id, UpdateMedicineDto dto)
    {
        var medicine = await _repo.GetByIdAsync(id);
        if (medicine is null)
            return (false, $"Medicine with ID {id} not found.", null);

        if (await _repo.ExistsByNameAsync(dto.Name, excludeId: id))
            return (false, $"Medicine '{dto.Name}' already exists.", null);

        // Validate inline units if provided
        if (dto.Units is { Count: > 0 })
        {
            var unitError = ValidateInlineUnits(dto.Units);
            if (unitError is not null)
                return (false, unitError, null);
        }

        medicine.Name                 = dto.Name.Trim();
        medicine.GenericName          = dto.GenericName?.Trim();
        medicine.CategoryId           = dto.CategoryId;
        medicine.ManufacturerId       = dto.ManufacturerId;
        medicine.TabletsPerStrip      = dto.TabletsPerStrip;
        medicine.StrapsPerBox         = dto.StrapsPerBox;
        medicine.StripPrice           = dto.StripPrice;
        medicine.TabletPrice          = dto.TabletPrice;
        medicine.ReorderLevel         = dto.ReorderLevel;
        medicine.RequiresPrescription = dto.RequiresPrescription;
        medicine.IsActive             = dto.IsActive;

        await _repo.UpdateAsync(medicine);

        // Replace units only when explicitly supplied
        if (dto.Units is { Count: > 0 })
        {
            var units = BuildMedicineUnits(id, dto.Units);
            await _repo.ReplaceUnitsAsync(id, units);
        }

        var full = await _repo.GetByIdAsync(id);
        return (true, "Medicine updated successfully.", MapToResponse(full!));
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
    /// Returns an error message if the inline units list is invalid, or null if valid.
    /// Rules:
    ///   - Exactly one IsBaseUnit = true
    ///   - Exactly one IsDefault = true
    ///   - No duplicate UnitOfMeasureId values
    ///   - The base unit must have ConversionFactorToBase = 1
    /// </summary>
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

    // ── Mapping ────────────────────────────────────────────────────────────────

    private static MedicineListItemDto MapToListItem(Medicine m) => new()
    {
        MedicineId           = m.MedicineId,
        Name                 = m.Name,
        GenericName          = m.GenericName,
        CategoryName         = m.Category?.CategoryName,
        ManufacturerName     = m.Manufacturer?.Name,
        TabletsPerStrip      = m.TabletsPerStrip,
        StrapsPerBox         = m.StrapsPerBox,
        TotalTabletsPerBox   = m.TotalTabletsPerBox,
        StripPrice           = m.StripPrice,
        TabletPrice          = m.TabletPrice,
        TotalStockInTablets  = m.TotalStockInTablets,
        IsActive             = m.IsActive,
        RequiresPrescription = m.RequiresPrescription
    };

    private static MedicineResponseDto MapToResponse(Medicine m) => new()
    {
        MedicineId           = m.MedicineId,
        Name                 = m.Name,
        GenericName          = m.GenericName,
        CategoryId           = m.CategoryId,
        CategoryName         = m.Category?.CategoryName,
        ManufacturerId       = m.ManufacturerId,
        ManufacturerName     = m.Manufacturer?.Name,
        TabletsPerStrip      = m.TabletsPerStrip,
        StrapsPerBox         = m.StrapsPerBox,
        TotalTabletsPerBox   = m.TotalTabletsPerBox,
        StripPrice           = m.StripPrice,
        TabletPrice          = m.TabletPrice,
        ReorderLevel         = m.ReorderLevel,
        RequiresPrescription = m.RequiresPrescription,
        IsActive             = m.IsActive,
        TotalStockInTablets  = m.TotalStockInTablets,
        CreatedAt            = m.CreatedAt,
        Units                = m.Units.Select(mu => new MedicineUnitDto
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
            UnitPrice              = mu.UnitPrice
        }).ToList()
    };
}
