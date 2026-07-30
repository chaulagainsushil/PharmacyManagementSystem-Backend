using PMS.Common.DTOs.Category;

namespace PMS.Application.Application.Interface.Service;

public interface ICategoryService
{
    Task<IEnumerable<CategoryListItemDto>> GetAllAsync();
    Task<CategoryResponseDto?> GetByIdAsync(int id);
    Task<(bool Success, string Message, CategoryResponseDto? Data)> CreateAsync(CreateCategoryDto dto);
    Task<(bool Success, string Message, CategoryResponseDto? Data)> UpdateAsync(int id, UpdateCategoryDto dto);
    Task<(bool Success, string Message)> DeleteAsync(int id);
}
