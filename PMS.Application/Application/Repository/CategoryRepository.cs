using Microsoft.EntityFrameworkCore;
using PMS.Application.Application.Interface.Repository;
using PMS.Common.DTOs.Category;
using PMS.Models.Interfaces;
using PMS.Models.Models;
using PMS.Repository.Data;

namespace PMS.Application.Application.Repository;

public class CategoryRepository : ICategoryRepository
{
    private readonly ApplicationDbContext _db;
    private readonly ITenantProvider _tenantProvider;

    public CategoryRepository(ApplicationDbContext db, ITenantProvider tenantProvider)
    {
        _db = db;
        _tenantProvider = tenantProvider;
    }

    public async Task<IEnumerable<CategoryListItemDto>> GetAllAsync()
    {
        return await _db.Categories
            .AsNoTracking()
            .OrderBy(c => c.CategoryName)
            .Select(c => new CategoryListItemDto
            {
                CategoryId    = c.CategoryId,
                CategoryName  = c.CategoryName,
                MedicineCount = c.Medicines.Count(),
                UpdatedAt     = c.UpdatedAt
            })
            .ToListAsync();
    }

    public async Task<CategoryResponseDto?> GetByIdAsync(int id)
    {
        return await _db.Categories
            .AsNoTracking()
            .Where(c => c.CategoryId == id)
            .Select(c => new CategoryResponseDto
            {
                CategoryId    = c.CategoryId,
                CategoryName  = c.CategoryName,
                MedicineCount = c.Medicines.Count(),
                UpdatedAt     = c.UpdatedAt
            })
            .FirstOrDefaultAsync();
    }

    public async Task<Category?> GetEntityByIdAsync(int id)
    {
        return await _db.Categories.FindAsync(id);
    }

    public async Task<bool> ExistsByNameAsync(string name, int? excludeId = null)
    {
        return await _db.Categories
            .AnyAsync(c => c.CategoryName.ToLower() == name.ToLower()
                          && (excludeId == null || c.CategoryId != excludeId));
    }

    public async Task<Category> CreateAsync(Category category)
    {
        _db.Categories.Add(category);
        await _db.SaveChangesAsync();
        return category;
    }

    public async Task<Category> UpdateAsync(Category category)
    {
        _db.Categories.Update(category);
        await _db.SaveChangesAsync();
        return category;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var category = await _db.Categories.FindAsync(id);
        if (category is null) return false;

        _db.Categories.Remove(category);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> HasMedicinesAsync(int id)
    {
        return await _db.Medicines.AnyAsync(m => m.CategoryId == id);
    }
}
