using PMS.Application.Application.Interface.Repository;
using PMS.Application.Application.Interface.Service;
using PMS.Common.DTOs.Category;
using PMS.Models.Models;

namespace PMS.Application.Application.Service;

public class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _repo;

    public CategoryService(ICategoryRepository repo)
    {
        _repo = repo;
    }

    public async Task<IEnumerable<CategoryListItemDto>> GetAllAsync()
    {
        var categories = await _repo.GetAllAsync();
        return categories.Select(c => new CategoryListItemDto
        {
            CategoryId    = c.CategoryId,
            CategoryName  = c.CategoryName,
            MedicineCount = c.Medicines.Count
        });
    }

    public async Task<CategoryResponseDto?> GetByIdAsync(int id)
    {
        var category = await _repo.GetByIdAsync(id);
        if (category is null) return null;

        return MapToResponse(category);
    }

    public async Task<(bool Success, string Message, CategoryResponseDto? Data)> CreateAsync(
        CreateCategoryDto dto)
    {
        if (await _repo.ExistsByNameAsync(dto.CategoryName))
            return (false, $"Category '{dto.CategoryName}' already exists.", null);

        var category = new Category { CategoryName = dto.CategoryName.Trim() };
        var created  = await _repo.CreateAsync(category);

        return (true, "Category created successfully.", MapToResponse(created));
    }

    public async Task<(bool Success, string Message, CategoryResponseDto? Data)> UpdateAsync(
        int id, UpdateCategoryDto dto)
    {
        var category = await _repo.GetByIdAsync(id);
        if (category is null)
            return (false, $"Category with ID {id} not found.", null);

        if (await _repo.ExistsByNameAsync(dto.CategoryName, excludeId: id))
            return (false, $"Category '{dto.CategoryName}' already exists.", null);

        category.CategoryName = dto.CategoryName.Trim();
        var updated = await _repo.UpdateAsync(category);

        return (true, "Category updated successfully.", MapToResponse(updated));
    }

    public async Task<(bool Success, string Message)> DeleteAsync(int id)
    {
        var exists = await _repo.GetByIdAsync(id);
        if (exists is null)
            return (false, $"Category with ID {id} not found.");

        if (await _repo.HasMedicinesAsync(id))
            return (false, "Cannot delete category — it has medicines linked to it.");

        await _repo.DeleteAsync(id);
        return (true, "Category deleted successfully.");
    }

    // ── Mapping helper ─────────────────────────────────────────────────────────
    private static CategoryResponseDto MapToResponse(Category c) => new()
    {
        CategoryId    = c.CategoryId,
        CategoryName  = c.CategoryName,
        MedicineCount = c.Medicines?.Count ?? 0
    };
}
