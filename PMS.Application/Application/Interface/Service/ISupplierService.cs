using PMS.Common.DTOs.Supplier;

namespace PMS.Application.Application.Interface.Service;

public interface ISupplierService
{
    Task<IEnumerable<SupplierListItemDto>> GetAllAsync();
    Task<SupplierResponseDto?> GetByIdAsync(int id);
    Task<(bool Success, string Message, SupplierResponseDto? Data)> CreateAsync(CreateSupplierDto dto);
    Task<(bool Success, string Message, bool IsConflict, SupplierResponseDto? Data)> UpdateAsync(int id, UpdateSupplierDto dto);
    Task<(bool Success, string Message)> DeleteAsync(int id);
}
