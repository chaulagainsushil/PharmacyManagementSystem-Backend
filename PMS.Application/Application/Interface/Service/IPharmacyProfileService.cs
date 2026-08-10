using PMS.Common.DTOs.PharmacyProfile;

namespace PMS.Application.Application.Interface.Service;

public interface IPharmacyProfileService
{
    Task<IEnumerable<PharmacyProfileListItemDto>> GetAllAsync();
    Task<PharmacyProfileResponseDto?> GetByIdAsync(int id);

    Task<(bool Success, string Message, PharmacyProfileResponseDto? Data)> CreateAsync(
        CreatePharmacyProfileDto dto);

    Task<(bool Success, string Message, bool IsConflict, PharmacyProfileResponseDto? Data)> UpdateAsync(
        int id, UpdatePharmacyProfileDto dto);

    /// <summary>Updates only the logo URL (called after a successful file upload).</summary>
    Task<(bool Success, string Message, PharmacyProfileResponseDto? Data)> UpdateLogoAsync(
        int id, string logoUrl);

    /// <summary>Updates only the signature URL (called after a successful file upload).</summary>
    Task<(bool Success, string Message, PharmacyProfileResponseDto? Data)> UpdateSignatureAsync(
        int id, string signatureUrl);

    Task<(bool Success, string Message)> DeleteAsync(int id);
}
