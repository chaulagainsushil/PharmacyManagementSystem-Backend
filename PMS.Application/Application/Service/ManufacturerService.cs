using PMS.Application.Application.Interface.Repository;
using PMS.Application.Application.Interface.Service;
using PMS.Common;
using PMS.Common.DTOs.Manufacturer;
using PMS.Models.Models;

namespace PMS.Application.Application.Service;

public class ManufacturerService : IManufacturerService
{
    private readonly IManufacturerRepository _repo;

    public ManufacturerService(IManufacturerRepository repo) => _repo = repo;

    public async Task<IEnumerable<ManufacturerListItemDto>> GetAllAsync()
    {
        return await _repo.GetAllAsync();
    }

    public async Task<ManufacturerResponseDto?> GetByIdAsync(int id)
    {
        return await _repo.GetByIdAsync(id);
    }

    public async Task<(bool Success, string Message, ManufacturerResponseDto? Data)> CreateAsync(
        CreateManufacturerDto dto)
    {
        if (await _repo.ExistsByNameAsync(dto.Name))
            return (false, $"Manufacturer '{dto.Name}' already exists.", null);

        var manufacturer = new Manufacturer { Name = dto.Name.Trim() };
        var created      = await _repo.CreateAsync(manufacturer);
        var response     = await _repo.GetByIdAsync(created.ManufacturerId);
        return (true, "Manufacturer created successfully.", response);
    }

    public async Task<(bool Success, string Message, bool IsConflict, ManufacturerResponseDto? Data)> UpdateAsync(
        int id, UpdateManufacturerDto dto)
    {
        // 1. Read current DTO (includes UpdatedAt)
        var existing = await _repo.GetByIdAsync(id);
        if (existing is null)
            return (false, $"Manufacturer with ID {id} not found.", false, null);

        // 2. Optimistic concurrency check
        if (!ConcurrencyHelper.Matches(existing.UpdatedAt, dto.UpdatedAt))
            return (false, "The record was modified by another user. Please refresh and try again.", true, null);

        // 3. Business rule: duplicate name
        if (await _repo.ExistsByNameAsync(dto.Name, excludeId: id))
            return (false, $"Manufacturer '{dto.Name}' already exists.", false, null);

        // 4. Fetch tracked entity and mutate
        var entity = await _repo.GetEntityByIdAsync(id);
        if (entity is null)
            return (false, $"Manufacturer with ID {id} not found.", false, null);

        entity.Name = dto.Name.Trim();
        await _repo.UpdateAsync(entity);

        var updated = await _repo.GetByIdAsync(id);
        return (true, "Manufacturer updated successfully.", false, updated);
    }

    public async Task<(bool Success, string Message)> DeleteAsync(int id)
    {
        var manufacturer = await _repo.GetByIdAsync(id);
        if (manufacturer is null)
            return (false, $"Manufacturer with ID {id} not found.");

        if (await _repo.HasMedicinesAsync(id))
            return (false, "Cannot delete manufacturer — it has medicines linked to it.");

        await _repo.DeleteAsync(id);
        return (true, "Manufacturer deleted successfully.");
    }
}
