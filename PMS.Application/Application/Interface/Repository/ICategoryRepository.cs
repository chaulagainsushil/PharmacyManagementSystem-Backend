using PMS.Common.DTOs.Category;
using PMS.Models.Models;

namespace PMS.Application.Application.Interface.Repository;

public interface ICategoryRepository
{
    Task<IEnumerable<CategoryListItemDto>> GetAllAsync();
    Task<CategoryResponseDto?> GetByIdAsync(int id);

    /// <summary>Returns the tracked entity for update/delete operations.</summary>
    Task<Category?> GetEntityByIdAsync(int id);

    Task<bool> ExistsByNameAsync(string name, int? excludeId = null);
    Task<Category> CreateAsync(Category category);
    Task<Category> UpdateAsync(Category category);
    Task<bool> DeleteAsync(int id);
    Task<bool> HasMedicinesAsync(int id);
}
