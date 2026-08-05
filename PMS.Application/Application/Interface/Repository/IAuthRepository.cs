using Microsoft.AspNetCore.Identity;
using PMS.Common.DTOs;
using PMS.Models.Models;

namespace PMS.Application.Application.Interface.Repository;

public interface IAuthRepository
{
    /// <summary>Registers a new user via ASP.NET Identity.</summary>
    Task<IdentityResult> RegisterAsync(SignupDto dto);

    /// <summary>Validates credentials and returns the Identity user if valid, null otherwise.</summary>
    Task<IdentityUser?> ValidateCredentialsAsync(LoginDto dto);

    /// <summary>Returns the roles assigned to the given user.</summary>
    Task<IList<string>> GetUserRolesAsync(IdentityUser user);

    /// <summary>Returns the FullName claim value stored for the given user.</summary>
    Task<string> GetFullNameAsync(IdentityUser user);

    /// <summary>
    /// Gets or creates the AppUser record (integer PK) linked to the given Identity user.
    /// Used to obtain the PharmacistId for sale creation.
    /// </summary>
    Task<User> GetOrCreateAppUserAsync(IdentityUser identityUser, string fullName);

    /// <summary>
    /// Assigns a single role to the given Identity user.
    /// The role must already exist (seeded at startup).
    /// </summary>
    Task<IdentityResult> AssignRoleAsync(IdentityUser user, string role);

    /// <summary>Finds an Identity user by their email address.</summary>
    Task<IdentityUser?> FindByEmailAsync(string email);
}
