using PMS.Common.DTOs.Medicine;

namespace PMS.Application.Application.Interface.Service;

public interface IMedicineService
{
    Task<IEnumerable<MedicineListItemDto>> GetAllAsync();
    Task<IEnumerable<MedicineListItemDto>> SearchAsync(string query);
    Task<MedicineResponseDto?> GetByIdAsync(int id);
    Task<(bool Success, string Message, MedicineResponseDto? Data)> CreateAsync(CreateMedicineDto dto);
    Task<BulkCreateMedicineResultDto> CreateBulkAsync(IReadOnlyList<CreateMedicineDto> dtos);
    Task<(bool Success, string Message, bool IsConflict, MedicineResponseDto? Data)> UpdateAsync(int id, UpdateMedicineDto dto);
    Task<(bool Success, string Message)> DeleteAsync(int id);
}
