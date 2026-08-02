using PMS.Application.Application.Interface.Repository;
using PMS.Application.Application.Interface.Service;
using PMS.Common.DTOs.Medicine;
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
        return medicines.Select(m => new MedicineListItemDto
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
        });
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

        // Reload with navigation properties for the response
        var full = await _repo.GetByIdAsync(created.MedicineId);
        return (true, "Medicine created successfully.", MapToResponse(full!));
    }

    public async Task<BulkCreateMedicineResultDto> CreateBulkAsync(IReadOnlyList<CreateMedicineDto> dtos)
    {
        var results  = new List<BulkCreateMedicineItemResultDto>(dtos.Count);
        var toInsert = new List<(int Index, Medicine Entity)>();

        // ── Per-row validation (duplicate name check) ─────────────────────────
        // Track names already seen within this batch to catch intra-batch dupes.
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
            }));
        }

        // ── Persist all valid rows in one trip ────────────────────────────────
        if (toInsert.Count > 0)
        {
            var saved = await _repo.CreateBulkAsync(toInsert.Select(t => t.Entity).ToList());

            // Reload each saved entity with navigation properties
            foreach (var (idx, entity) in toInsert)
            {
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

        // Sort results back into original request order
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

        // Reload with navigation properties
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

    // ── Mapping helper ─────────────────────────────────────────────────────────
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
        CreatedAt            = m.CreatedAt
    };
}
