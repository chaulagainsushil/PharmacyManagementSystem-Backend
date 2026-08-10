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

    // ── Registration ──────────────────────────────────────────────────────────

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

    // ── Authentication ────────────────────────────────────────────────────────

    public async Task<IdentityUser?> ValidateCredentialsAsync(LoginDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user is null) return null;

        var isValid = await _userManager.CheckPasswordAsync(user, dto.Password);
        return isValid ? user : null;
    }

    // ── Roles & Claims ────────────────────────────────────────────────────────

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

        // AppUsers is NOT tenant-scoped (no TenantId) — use IgnoreQueryFilters just in case
        var appUser = await _db.AppUsers
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Email == email);

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

    public async Task<IdentityUser?> FindByIdAsync(string userId)
        => await _userManager.FindByIdAsync(userId);

    // ── Tenant helpers ────────────────────────────────────────────────────────

    public async Task<PharmacyProfile?> GetPharmacyProfileByTenantIdAsync(Guid tenantId)
        => await _db.PharmacyProfiles
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.TenantId == tenantId);

    public async Task<PharmacyProfile?> GetPharmacyProfileByUserEmailAsync(string email)
    {
        // The "tenantId" claim is stored on the Identity user — look it up
        var identityUser = await _userManager.FindByEmailAsync(email);
        if (identityUser is null) return null;

        var claims = await _userManager.GetClaimsAsync(identityUser);
        var tenantIdStr = claims.FirstOrDefault(c => c.Type == "tenantId")?.Value;

        if (!Guid.TryParse(tenantIdStr, out var tenantId)) return null;

        return await GetPharmacyProfileByTenantIdAsync(tenantId);
    }

    public async Task<PharmacyProfile> CreatePharmacyProfileAsync(string pharmacyName)
    {
        var profile = new PharmacyProfile
        {
            PharmacyName = pharmacyName,
            TenantId     = Guid.NewGuid(),
            CreatedAt    = DateTime.UtcNow,
            UpdatedAt    = DateTime.UtcNow
        };

        _db.PharmacyProfiles.Add(profile);
        await _db.SaveChangesAsync();
        return profile;
    }

    public async Task AddTenantClaimAsync(IdentityUser user, Guid tenantId)
    {
        // Remove any stale tenantId claim first
        var existingClaims = await _userManager.GetClaimsAsync(user);
        var old = existingClaims.FirstOrDefault(c => c.Type == "tenantId");
        if (old is not null)
            await _userManager.RemoveClaimAsync(user, old);

        await _userManager.AddClaimAsync(user, new Claim("tenantId", tenantId.ToString()));
    }

    // ── Password Management ───────────────────────────────────────────────────

    public async Task<IdentityResult> ChangePasswordAsync(
        IdentityUser user, string currentPassword, string newPassword)
        => await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);

    public async Task<string> GeneratePasswordResetTokenAsync(IdentityUser user)
        => await _userManager.GeneratePasswordResetTokenAsync(user);

    public async Task<IdentityResult> ResetPasswordAsync(
        IdentityUser user, string token, string newPassword)
        => await _userManager.ResetPasswordAsync(user, token, newPassword);
}
