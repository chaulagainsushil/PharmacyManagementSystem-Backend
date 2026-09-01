using PMS.Common.DTOs;
using PMS.Common.DTOs.Pharmacist;

namespace PMS.Application.Application.Interface.Service;

public interface IPharmacistService
{
    Task<IEnumerable<PharmacistListItemDto>> GetAllPharmacistsAsync();
    Task<PharmacistListItemDto?> GetByIdAsync(string id);
    Task<(bool Success, string Message, PharmacistListItemDto? Data)> CreatePharmacistAsync(CreatePharmacistRequestDto dto);
    Task<(bool Success, string Message, PharmacistListItemDto? Data)> UpdatePharmacistAsync(string id, UpdatePharmacistRequestDto dto);
    Task<(bool Success, string Message)> DeletePharmacistAsync(string id, string currentUserId);
}
