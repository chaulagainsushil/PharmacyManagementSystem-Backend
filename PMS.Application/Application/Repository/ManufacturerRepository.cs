using Microsoft.EntityFrameworkCore;
using PMS.Application.Application.Interface.Repository;
using PMS.Common.DTOs.Manufacturer;
using PMS.Models.Models;
using PMS.Repository.Data;

namespace PMS.Application.Application.Repository;

public class ManufacturerRepository : IManufacturerRepository
{
    private readonly ApplicationDbContext _db;

    public ManufacturerRepository(ApplicationDbContext db) => _db = db;

    public async Task<IEnumerable<ManufacturerListItemDto>> GetAllAsync() =>
        await _db.Manufacturers
            .AsNoTracking()
            .OrderBy(m => m.Name)
            .Select(m => new ManufacturerListItemDto
            {
                ManufacturerId = m.ManufacturerId,
                Name           = m.Name,
                MedicineCount  = m.Medicines.Count(),
                UpdatedAt      = m.UpdatedAt
            })
            .ToListAsync();

    public async Task<ManufacturerResponseDto?> GetByIdAsync(int id) =>
        await _db.Manufacturers
            .AsNoTracking()
            .Where(m => m.ManufacturerId == id)
            .Select(m => new ManufacturerResponseDto
            {
                ManufacturerId = m.ManufacturerId,
                Name           = m.Name,
                MedicineCount  = m.Medicines.Count(),
                UpdatedAt      = m.UpdatedAt
            })
            .FirstOrDefaultAsync();

    public async Task<Manufacturer?> GetEntityByIdAsync(int id) =>
        await _db.Manufacturers.FindAsync(id);

    public async Task<bool> ExistsByNameAsync(string name, int? excludeId = null) =>
        await _db.Manufacturers
            .AnyAsync(m => m.Name.ToLower() == name.ToLower()
                          && (excludeId == null || m.ManufacturerId != excludeId));

    public async Task<Manufacturer> CreateAsync(Manufacturer manufacturer)
    {
        _db.Manufacturers.Add(manufacturer);
        await _db.SaveChangesAsync();
        return manufacturer;
    }

    public async Task<Manufacturer> UpdateAsync(Manufacturer manufacturer)
    {
        _db.Manufacturers.Update(manufacturer);
        await _db.SaveChangesAsync();
        return manufacturer;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var manufacturer = await _db.Manufacturers.FindAsync(id);
        if (manufacturer is null) return false;
        _db.Manufacturers.Remove(manufacturer);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> HasMedicinesAsync(int id) =>
        await _db.Medicines.AnyAsync(m => m.ManufacturerId == id);
}
