using PMS.Common.DTOs.Manufacturer;

namespace PMS.Application.Application.Interface.Service;

public interface IManufacturerService
{
    Task<IEnumerable<ManufacturerListItemDto>> GetAllAsync();
    Task<ManufacturerResponseDto?> GetByIdAsync(int id);
    Task<(bool Success, string Message, ManufacturerResponseDto? Data)> CreateAsync(CreateManufacturerDto dto);
    Task<(bool Success, string Message, ManufacturerResponseDto? Data)> UpdateAsync(int id, UpdateManufacturerDto dto);
    Task<(bool Success, string Message)> DeleteAsync(int id);
}
