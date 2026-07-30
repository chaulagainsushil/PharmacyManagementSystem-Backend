using PMS.Application.Application.Interface.Repository;
using PMS.Application.Application.Interface.Service;
using PMS.Common.DTOs.Supplier;
using PMS.Models.Models;

namespace PMS.Application.Application.Service;

public class SupplierService : ISupplierService
{
    private readonly ISupplierRepository _repo;

    public SupplierService(ISupplierRepository repo) => _repo = repo;

    public async Task<IEnumerable<SupplierListItemDto>> GetAllAsync()
    {
        var suppliers = await _repo.GetAllAsync();
        return suppliers.Select(s => new SupplierListItemDto
        {
            SupplierId = s.SupplierId,
            Name       = s.Name,
            Phone      = s.Phone,
            Address    = s.Address,
            BatchCount = s.Batches.Count
        });
    }

    public async Task<SupplierResponseDto?> GetByIdAsync(int id)
    {
        var supplier = await _repo.GetByIdAsync(id);
        return supplier is null ? null : MapToResponse(supplier);
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

        var created = await _repo.CreateAsync(supplier);
        return (true, "Supplier created successfully.", MapToResponse(created));
    }

    public async Task<(bool Success, string Message, SupplierResponseDto? Data)> UpdateAsync(
        int id, UpdateSupplierDto dto)
    {
        var supplier = await _repo.GetByIdAsync(id);
        if (supplier is null)
            return (false, $"Supplier with ID {id} not found.", null);

        if (await _repo.ExistsByNameAsync(dto.Name, excludeId: id))
            return (false, $"Supplier '{dto.Name}' already exists.", null);

        supplier.Name    = dto.Name.Trim();
        supplier.Phone   = dto.Phone?.Trim();
        supplier.Address = dto.Address?.Trim();

        var updated = await _repo.UpdateAsync(supplier);
        return (true, "Supplier updated successfully.", MapToResponse(updated));
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

    private static SupplierResponseDto MapToResponse(Supplier s) => new()
    {
        SupplierId = s.SupplierId,
        Name       = s.Name,
        Phone      = s.Phone,
        Address    = s.Address,
        BatchCount = s.Batches?.Count ?? 0
    };
}
