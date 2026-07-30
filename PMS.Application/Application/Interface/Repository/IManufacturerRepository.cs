using PMS.Models.Models;

namespace PMS.Application.Application.Interface.Repository;

public interface IManufacturerRepository
{
    Task<IEnumerable<Manufacturer>> GetAllAsync();
    Task<Manufacturer?> GetByIdAsync(int id);
    Task<bool> ExistsByNameAsync(string name, int? excludeId = null);
    Task<Manufacturer> CreateAsync(Manufacturer manufacturer);
    Task<Manufacturer> UpdateAsync(Manufacturer manufacturer);
    Task<bool> DeleteAsync(int id);
    Task<bool> HasMedicinesAsync(int id);
}
