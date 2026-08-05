using PMS.Common.DTOs;

namespace PMS.Application.Application.Interface.Service;

public interface IAuthService
{
    /// <summary>Signs up a new user. Returns token on success.</summary>
    Task<AuthResponseDto> SignupAsync(SignupDto dto);

    /// <summary>Logs in an existing user. Returns token on success.</summary>
    Task<AuthResponseDto> LoginAsync(LoginDto dto);

    /// <summary>
    /// Creates a new user and assigns the specified role.
    /// Only callable by a SystemAdmin — enforced at the controller layer.
    /// </summary>
    Task<AuthResponseDto> CreateUserAsync(SignupDto dto);

    /// <summary>
    /// Assigns (or re-assigns) a role to an existing user identified by email.
    /// Only callable by a SystemAdmin — enforced at the controller layer.
    /// </summary>
    Task<AuthResponseDto> AssignRoleAsync(AssignRoleDto dto);
}
