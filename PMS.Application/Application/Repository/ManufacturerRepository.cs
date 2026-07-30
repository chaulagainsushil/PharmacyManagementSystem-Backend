using Microsoft.EntityFrameworkCore;
using PMS.Application.Application.Interface.Repository;
using PMS.Models.Models;
using PMS.Repository.Data;

namespace PMS.Application.Application.Repository;

public class ManufacturerRepository : IManufacturerRepository
{
    private readonly ApplicationDbContext _db;

    public ManufacturerRepository(ApplicationDbContext db) => _db = db;

    public async Task<IEnumerable<Manufacturer>> GetAllAsync() =>
        await _db.Manufacturers
            .Include(m => m.Medicines)
            .AsNoTracking()
            .OrderBy(m => m.Name)
            .ToListAsync();

    public async Task<Manufacturer?> GetByIdAsync(int id) =>
        await _db.Manufacturers
            .Include(m => m.Medicines)
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.ManufacturerId == id);

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
