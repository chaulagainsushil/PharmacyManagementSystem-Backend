using PMS.Application.Application.Interface.Repository;
using PMS.Application.Application.Interface.Service;
using PMS.Common;
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
        return await _repo.GetAllAsync();
    }

    public async Task<CategoryResponseDto?> GetByIdAsync(int id)
    {
        return await _repo.GetByIdAsync(id);
    }

    public async Task<(bool Success, string Message, CategoryResponseDto? Data)> CreateAsync(
        CreateCategoryDto dto)
    {
        if (await _repo.ExistsByNameAsync(dto.CategoryName))
            return (false, $"Category '{dto.CategoryName}' already exists.", null);

        var category = new Category { CategoryName = dto.CategoryName.Trim() };
        var created  = await _repo.CreateAsync(category);

        var response = await _repo.GetByIdAsync(created.CategoryId);
        return (true, "Category created successfully.", response);
    }

    public async Task<(bool Success, string Message, bool IsConflict, CategoryResponseDto? Data)> UpdateAsync(
        int id, UpdateCategoryDto dto)
    {
        // 1. Read current DTO (includes UpdatedAt)
        var existing = await _repo.GetByIdAsync(id);
        if (existing is null)
            return (false, $"Category with ID {id} not found.", false, null);

        // 2. Optimistic concurrency check
        if (!ConcurrencyHelper.Matches(existing.UpdatedAt, dto.UpdatedAt))
            return (false, "The record was modified by another user. Please refresh and try again.", true, null);

        // 3. Business rule: duplicate name
        if (await _repo.ExistsByNameAsync(dto.CategoryName, excludeId: id))
            return (false, $"Category '{dto.CategoryName}' already exists.", false, null);

        // 4. Fetch tracked entity and mutate
        var entity = await _repo.GetEntityByIdAsync(id);
        if (entity is null)
            return (false, $"Category with ID {id} not found.", false, null);

        entity.CategoryName = dto.CategoryName.Trim();
        await _repo.UpdateAsync(entity);

        var updated = await _repo.GetByIdAsync(id);
        return (true, "Category updated successfully.", false, updated);
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
}
