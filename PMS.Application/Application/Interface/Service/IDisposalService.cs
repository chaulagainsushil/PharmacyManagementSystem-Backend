using PMS.Common.DTOs.Disposal;

namespace PMS.Application.Application.Interface.Service;

public interface IDisposalService
{
    Task<(bool Success, string Message, DisposalResponseDto? Data)> CreateDisposalAsync(CreateDisposalRequestDto dto);
    Task<IReadOnlyList<DisposalResponseDto>> GetAllAsync();
    Task<DisposalResponseDto?> GetByIdAsync(int id);
}
