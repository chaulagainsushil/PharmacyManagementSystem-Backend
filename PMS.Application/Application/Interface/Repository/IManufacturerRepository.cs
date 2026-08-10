using PMS.Common.DTOs.Manufacturer;
using PMS.Models.Models;

namespace PMS.Application.Application.Interface.Repository;

public interface IManufacturerRepository
{
    Task<IEnumerable<ManufacturerListItemDto>> GetAllAsync();
    Task<ManufacturerResponseDto?> GetByIdAsync(int id);

    /// <summary>Returns the tracked entity for update/delete operations.</summary>
    Task<Manufacturer?> GetEntityByIdAsync(int id);

    Task<bool> ExistsByNameAsync(string name, int? excludeId = null);
    Task<Manufacturer> CreateAsync(Manufacturer manufacturer);
    Task<Manufacturer> UpdateAsync(Manufacturer manufacturer);
    Task<bool> DeleteAsync(int id);
    Task<bool> HasMedicinesAsync(int id);
}
