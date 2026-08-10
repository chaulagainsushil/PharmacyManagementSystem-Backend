using PMS.Application.Application.Interface.Repository;
using PMS.Application.Application.Interface.Service;
using PMS.Common;
using PMS.Common.DTOs.Supplier;
using PMS.Models.Models;

namespace PMS.Application.Application.Service;

public class SupplierService : ISupplierService
{
    private readonly ISupplierRepository _repo;

    public SupplierService(ISupplierRepository repo) => _repo = repo;

    public async Task<IEnumerable<SupplierListItemDto>> GetAllAsync()
    {
        return await _repo.GetAllAsync();
    }

    public async Task<SupplierResponseDto?> GetByIdAsync(int id)
    {
        return await _repo.GetByIdAsync(id);
    }

    public async Task<(bool Success, string Message, SupplierResponseDto? Data)> CreateAsync(
        CreateSupplierDto dto)
    {
        if (await _repo.ExistsByNameAsync(dto.Name))
            return (false, $"Supplier '{dto.Name}' already exists.", null);

        var supplier = new Supplier
        {
            Name    = dto.Name.Trim(),
            Phone   = dto.Phone?.Trim(),
            Address = dto.Address?.Trim()
        };

        var created  = await _repo.CreateAsync(supplier);
        var response = await _repo.GetByIdAsync(created.SupplierId);
        return (true, "Supplier created successfully.", response);
    }

    public async Task<(bool Success, string Message, bool IsConflict, SupplierResponseDto? Data)> UpdateAsync(
        int id, UpdateSupplierDto dto)
    {
        // 1. Read current DTO (includes UpdatedAt)
        var existing = await _repo.GetByIdAsync(id);
        if (existing is null)
            return (false, $"Supplier with ID {id} not found.", false, null);

        // 2. Optimistic concurrency check
        if (!ConcurrencyHelper.Matches(existing.UpdatedAt, dto.UpdatedAt))
            return (false, "The record was modified by another user. Please refresh and try again.", true, null);

        // 3. Business rule: duplicate name
        if (await _repo.ExistsByNameAsync(dto.Name, excludeId: id))
            return (false, $"Supplier '{dto.Name}' already exists.", false, null);

        // 4. Fetch tracked entity and mutate
        var entity = await _repo.GetEntityByIdAsync(id);
        if (entity is null)
            return (false, $"Supplier with ID {id} not found.", false, null);

        entity.Name    = dto.Name.Trim();
        entity.Phone   = dto.Phone?.Trim();
        entity.Address = dto.Address?.Trim();

        await _repo.UpdateAsync(entity);

        var updated = await _repo.GetByIdAsync(id);
        return (true, "Supplier updated successfully.", false, updated);
    }

    public async Task<(bool Success, string Message)> DeleteAsync(int id)
    {
        var supplier = await _repo.GetByIdAsync(id);
        if (supplier is null)
            return (false, $"Supplier with ID {id} not found.");

        if (await _repo.HasBatchesAsync(id))
            return (false, "Cannot delete supplier — it has medicine batches linked to it.");

        await _repo.DeleteAsync(id);
        return (true, "Supplier deleted successfully.");
    }
}
