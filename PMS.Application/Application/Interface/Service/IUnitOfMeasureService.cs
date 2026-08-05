using PMS.Common.DTOs.Uom;

namespace PMS.Application.Application.Interface.Service;

public interface IUnitOfMeasureService
{
    Task<IReadOnlyList<UomDto>> GetAllAsync(bool includeInactive = false);
    Task<UomDto?> GetByIdAsync(int id);
    Task<(bool Success, string Message, UomDto? Data)> CreateAsync(CreateUomDto dto);
    Task<(bool Success, string Message, UomDto? Data)> UpdateAsync(int id, UpdateUomDto dto);
    Task<(bool Success, string Message)> DeleteAsync(int id);
}
