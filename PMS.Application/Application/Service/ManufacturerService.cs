using PMS.Application.Application.Interface.Repository;
using PMS.Application.Application.Interface.Service;
using PMS.Common.DTOs.Manufacturer;
using PMS.Models.Models;

namespace PMS.Application.Application.Service;

public class ManufacturerService : IManufacturerService
{
    private readonly IManufacturerRepository _repo;

    public ManufacturerService(IManufacturerRepository repo) => _repo = repo;

    public async Task<IEnumerable<ManufacturerListItemDto>> GetAllAsync()
    {
        var manufacturers = await _repo.GetAllAsync();
        return manufacturers.Select(m => new ManufacturerListItemDto
        {
            ManufacturerId = m.ManufacturerId,
            Name           = m.Name,
            MedicineCount  = m.Medicines.Count
        });
    }

    public async Task<ManufacturerResponseDto?> GetByIdAsync(int id)
    {
        var manufacturer = await _repo.GetByIdAsync(id);
        return manufacturer is null ? null : MapToResponse(manufacturer);
    }

    public async Task<(bool Success, string Message, ManufacturerResponseDto? Data)> CreateAsync(
        CreateManufacturerDto dto)
    {
        if (await _repo.ExistsByNameAsync(dto.Name))
            return (false, $"Manufacturer '{dto.Name}' already exists.", null);

        var manufacturer = new Manufacturer { Name = dto.Name.Trim() };
        var created      = await _repo.CreateAsync(manufacturer);
        return (true, "Manufacturer created successfully.", MapToResponse(created));
    }

    public async Task<(bool Success, string Message, ManufacturerResponseDto? Data)> UpdateAsync(
        int id, UpdateManufacturerDto dto)
    {
        var manufacturer = await _repo.GetByIdAsync(id);
        if (manufacturer is null)
            return (false, $"Manufacturer with ID {id} not found.", null);

        if (await _repo.ExistsByNameAsync(dto.Name, excludeId: id))
            return (false, $"Manufacturer '{dto.Name}' already exists.", null);

        manufacturer.Name = dto.Name.Trim();
        var updated       = await _repo.UpdateAsync(manufacturer);
        return (true, "Manufacturer updated successfully.", MapToResponse(updated));
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

    private static ManufacturerResponseDto MapToResponse(Manufacturer m) => new()
    {
        ManufacturerId = m.ManufacturerId,
        Name           = m.Name,
        MedicineCount  = m.Medicines?.Count ?? 0
    };
}
