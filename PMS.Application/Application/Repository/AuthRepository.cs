using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PMS.Application.Application.Interface.Repository;
using PMS.Common;
using PMS.Common.DTOs;
using PMS.Models.Models;
using PMS.Repository.Data;

namespace PMS.Application.Application.Repository;

public class AuthRepository : IAuthRepository
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly ApplicationDbContext _db;

    public AuthRepository(UserManager<IdentityUser> userManager, ApplicationDbContext db)
    {
        _userManager = userManager;
        _db = db;
    }

    public async Task<IdentityResult> RegisterAsync(SignupDto dto)
    {
        var user = new IdentityUser
        {
            UserName = dto.Email,
            Email    = dto.Email
        };

        var result = await _userManager.CreateAsync(user, dto.Password);

        if (result.Succeeded)
        {
            // Store FullName as a persistent claim so it is available on every login
            await _userManager.AddClaimAsync(user, new Claim("FullName", dto.FullName));
        }

        return result;
    }

    public async Task<IdentityUser?> ValidateCredentialsAsync(LoginDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user is null) return null;

        var isValid = await _userManager.CheckPasswordAsync(user, dto.Password);
        return isValid ? user : null;
    }

    public async Task<IList<string>> GetUserRolesAsync(IdentityUser user)
        => await _userManager.GetRolesAsync(user);

    public async Task<string> GetFullNameAsync(IdentityUser user)
    {
        var claims = await _userManager.GetClaimsAsync(user);
        return claims.FirstOrDefault(c => c.Type == "FullName")?.Value
               ?? user.Email
               ?? string.Empty;
    }

    public async Task<User> GetOrCreateAppUserAsync(IdentityUser identityUser, string fullName)
    {
        var email = identityUser.Email ?? string.Empty;

        var appUser = await _db.AppUsers.FirstOrDefaultAsync(u => u.Email == email);
        if (appUser is not null) return appUser;

        appUser = new User { FullName = fullName, Email = email };
        _db.AppUsers.Add(appUser);
        await _db.SaveChangesAsync();
        return appUser;
    }

    public async Task<IdentityResult> AssignRoleAsync(IdentityUser user, string role)
    {
        // Remove any previously assigned roles first (one role per user model)
        var currentRoles = await _userManager.GetRolesAsync(user);
        if (currentRoles.Any())
            await _userManager.RemoveFromRolesAsync(user, currentRoles);

        return await _userManager.AddToRoleAsync(user, role);
    }

    public async Task<IdentityUser?> FindByEmailAsync(string email)
        => await _userManager.FindByEmailAsync(email);
}
