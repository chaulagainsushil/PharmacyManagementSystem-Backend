using PMS.Common.DTOs.Medicine;

namespace PMS.Application.Application.Interface.Service;

public interface IMedicineService
{
    Task<IEnumerable<MedicineListItemDto>> GetAllAsync();
    Task<MedicineResponseDto?> GetByIdAsync(int id);
    Task<(bool Success, string Message, MedicineResponseDto? Data)> CreateAsync(CreateMedicineDto dto);
    Task<(bool Success, string Message, MedicineResponseDto? Data)> UpdateAsync(int id, UpdateMedicineDto dto);
    Task<(bool Success, string Message)> DeleteAsync(int id);
}
