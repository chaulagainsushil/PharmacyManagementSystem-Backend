using PMS.Common.DTOs.MedicineBatch;

namespace PMS.Application.Application.Interface.Service;

public interface IMedicineBatchService
{
    Task<IEnumerable<MedicineBatchListItemDto>> GetAllAsync();
    Task<IEnumerable<MedicineBatchListItemDto>> GetByMedicineIdAsync(int medicineId);
    Task<MedicineBatchResponseDto?> GetByIdAsync(int id);
    Task<(bool Success, string Message, MedicineBatchResponseDto? Data)> CreateAsync(CreateMedicineBatchDto dto);
    Task<(bool Success, string Message, MedicineBatchResponseDto? Data)> UpdateAsync(int id, UpdateMedicineBatchDto dto);
    Task<(bool Success, string Message)> DeleteAsync(int id);
}
