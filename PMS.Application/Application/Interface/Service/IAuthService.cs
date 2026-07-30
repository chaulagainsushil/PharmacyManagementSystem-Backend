using PMS.Common.DTOs;

namespace PMS.Application.Application.Interface.Service;

public interface IAuthService
{
    /// <summary>Signs up a new user. Returns token on success.</summary>
    Task<AuthResponseDto> SignupAsync(SignupDto dto);

    /// <summary>Logs in an existing user. Returns token on success.</summary>
    Task<AuthResponseDto> LoginAsync(LoginDto dto);
}
