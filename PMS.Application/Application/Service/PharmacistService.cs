using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PMS.Application.Application.Interface.Repository;
using PMS.Application.Application.Interface.Service;
using PMS.Common;
using PMS.Common.DTOs.Pharmacist;
using PMS.Models.Interfaces;
using PMS.Models.Models;
using PMS.Repository.Data;

namespace PMS.Application.Application.Service;

public class PharmacistService : IPharmacistService
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly ApplicationDbContext _db;
    private readonly ITenantProvider _tenantProvider;
    private readonly IAuthRepository _authRepository;

    public PharmacistService(
        UserManager<IdentityUser> userManager,
        ApplicationDbContext db,
        ITenantProvider tenantProvider,
        IAuthRepository authRepository)
    {
        _userManager    = userManager;
        _db             = db;
        _tenantProvider = tenantProvider;
        _authRepository = authRepository;
    }

    public async Task<IEnumerable<PharmacistListItemDto>> GetAllPharmacistsAsync()
    {
        var tenantId = _tenantProvider.TenantId;
        if (tenantId == Guid.Empty)
            return Enumerable.Empty<PharmacistListItemDto>();

        // Find all identity users belonging to this tenant
        // A user belongs to this tenant if they have a "tenantId" claim matching this tenantId
        var userClaims = await _db.UserClaims
            .Where(c => c.ClaimType == "tenantId" && c.ClaimValue == tenantId.ToString())
            .ToListAsync();

        var userIds = userClaims.Select(c => c.UserId).Distinct().ToList();
        if (userIds.Count == 0)
            return Enumerable.Empty<PharmacistListItemDto>();

        var identityUsers = await _userManager.Users
            .Where(u => userIds.Contains(u.Id))
            .ToListAsync();

        var emails = identityUsers.Select(u => u.Email).Where(e => !string.IsNullOrEmpty(e)).ToList();

        // Get app users for total sales count
        var appUsers = await _db.AppUsers
            .IgnoreQueryFilters()
            .Include(u => u.Sales)
            .Where(u => emails.Contains(u.Email))
            .ToListAsync();

        var appUserMap = appUsers.ToDictionary(u => u.Email, StringComparer.OrdinalIgnoreCase);

        var list = new List<PharmacistListItemDto>();

        foreach (var user in identityUsers)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var claims = await _userManager.GetClaimsAsync(user);
            var fullName = claims.FirstOrDefault(c => c.Type == "FullName")?.Value ?? user.Email ?? "Unknown";

            appUserMap.TryGetValue(user.Email ?? "", out var appUser);

            list.Add(new PharmacistListItemDto
            {
                Id              = user.Id,
                FullName        = fullName,
                Email           = user.Email ?? "",
                Role            = roles.FirstOrDefault() ?? AppRoles.Pharmacist,
                AppUserId       = appUser?.UserId,
                TotalSalesCount = appUser?.Sales?.Count ?? 0,
                CreatedAt       = appUser != null ? appUser.UpdatedAt : DateTime.UtcNow
            });
        }

        return list.OrderBy(u => u.Role == AppRoles.PharmacyAdmin ? 0 : 1)
                   .ThenBy(u => u.FullName);
    }

    public async Task<PharmacistListItemDto?> GetByIdAsync(string id)
    {
        var tenantId = _tenantProvider.TenantId;
        if (tenantId == Guid.Empty) return null;

        var user = await _userManager.FindByIdAsync(id);
        if (user is null) return null;

        var claims = await _userManager.GetClaimsAsync(user);
        var userTenantIdStr = claims.FirstOrDefault(c => c.Type == "tenantId")?.Value;
        if (userTenantIdStr != tenantId.ToString()) return null;

        var roles = await _userManager.GetRolesAsync(user);
        var fullName = claims.FirstOrDefault(c => c.Type == "FullName")?.Value ?? user.Email ?? "Unknown";

        var appUser = await _db.AppUsers
            .IgnoreQueryFilters()
            .Include(u => u.Sales)
            .FirstOrDefaultAsync(u => u.Email == user.Email);

        return new PharmacistListItemDto
        {
            Id              = user.Id,
            FullName        = fullName,
            Email           = user.Email ?? "",
            Role            = roles.FirstOrDefault() ?? AppRoles.Pharmacist,
            AppUserId       = appUser?.UserId,
            TotalSalesCount = appUser?.Sales?.Count ?? 0,
            CreatedAt       = appUser?.UpdatedAt ?? DateTime.UtcNow
        };
    }

    public async Task<(bool Success, string Message, PharmacistListItemDto? Data)> CreatePharmacistAsync(CreatePharmacistRequestDto dto)
    {
        var tenantId = _tenantProvider.TenantId;
        if (tenantId == Guid.Empty)
            return (false, "Invalid tenant context.", null);

        var existing = await _userManager.FindByEmailAsync(dto.Email.Trim());
        if (existing is not null)
            return (false, $"A user with email '{dto.Email}' already exists.", null);

        var user = new IdentityUser
        {
            UserName = dto.Email.Trim(),
            Email    = dto.Email.Trim()
        };

        var result = await _userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
        {
            return (false, string.Join("; ", result.Errors.Select(e => e.Description)), null);
        }

        // Store FullName and tenantId claim
        await _userManager.AddClaimAsync(user, new Claim("FullName", dto.FullName.Trim()));
        await _userManager.AddClaimAsync(user, new Claim("tenantId", tenantId.ToString()));

        // Assign role
        var role = !string.IsNullOrWhiteSpace(dto.Role) && AppRoles.All.Contains(dto.Role)
            ? dto.Role
            : AppRoles.Pharmacist;

        await _userManager.AddToRoleAsync(user, role);

        // Ensure AppUser record exists for Sales linking
        var appUser = await _authRepository.GetOrCreateAppUserAsync(user, dto.FullName.Trim());

        var dtoResult = new PharmacistListItemDto
        {
            Id              = user.Id,
            FullName        = dto.FullName.Trim(),
            Email           = user.Email,
            Role            = role,
            AppUserId       = appUser.UserId,
            TotalSalesCount = 0,
            CreatedAt       = DateTime.UtcNow
        };

        return (true, "Pharmacist created successfully.", dtoResult);
    }

    public async Task<(bool Success, string Message, PharmacistListItemDto? Data)> UpdatePharmacistAsync(string id, UpdatePharmacistRequestDto dto)
    {
        var tenantId = _tenantProvider.TenantId;
        if (tenantId == Guid.Empty)
            return (false, "Invalid tenant context.", null);

        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
            return (false, "User not found.", null);

        var claims = await _userManager.GetClaimsAsync(user);
        var userTenantIdStr = claims.FirstOrDefault(c => c.Type == "tenantId")?.Value;
        if (userTenantIdStr != tenantId.ToString())
            return (false, "User does not belong to this pharmacy.", null);

        // Update FullName claim
        var existingNameClaim = claims.FirstOrDefault(c => c.Type == "FullName");
        if (existingNameClaim is not null)
            await _userManager.RemoveClaimAsync(user, existingNameClaim);

        await _userManager.AddClaimAsync(user, new Claim("FullName", dto.FullName.Trim()));

        // Update AppUser entity
        var appUser = await _db.AppUsers
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Email == user.Email);

        if (appUser is not null)
        {
            appUser.FullName = dto.FullName.Trim();
            appUser.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        // Update Role if requested and valid
        if (!string.IsNullOrWhiteSpace(dto.Role) && AppRoles.All.Contains(dto.Role))
        {
            var currentRoles = await _userManager.GetRolesAsync(user);
            if (!currentRoles.Contains(dto.Role))
            {
                await _userManager.RemoveFromRolesAsync(user, currentRoles);
                await _userManager.AddToRoleAsync(user, dto.Role);
            }
        }

        // Update password if provided
        if (!string.IsNullOrWhiteSpace(dto.NewPassword))
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var resetResult = await _userManager.ResetPasswordAsync(user, token, dto.NewPassword);
            if (!resetResult.Succeeded)
            {
                return (false, string.Join("; ", resetResult.Errors.Select(e => e.Description)), null);
            }
        }

        var updatedRoles = await _userManager.GetRolesAsync(user);

        return (true, "Pharmacist updated successfully.", new PharmacistListItemDto
        {
            Id              = user.Id,
            FullName        = dto.FullName.Trim(),
            Email           = user.Email ?? "",
            Role            = updatedRoles.FirstOrDefault() ?? AppRoles.Pharmacist,
            AppUserId       = appUser?.UserId,
            TotalSalesCount = appUser != null ? await _db.Sales.CountAsync(s => s.PharmacistId == appUser.UserId) : 0,
            CreatedAt       = appUser?.UpdatedAt ?? DateTime.UtcNow
        });
    }

    public async Task<(bool Success, string Message)> DeletePharmacistAsync(string id, string currentUserId)
    {
        var tenantId = _tenantProvider.TenantId;
        if (tenantId == Guid.Empty)
            return (false, "Invalid tenant context.");

        if (string.Equals(id, currentUserId, StringComparison.OrdinalIgnoreCase))
            return (false, "You cannot delete your own account.");

        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
            return (false, "User not found.");

        var claims = await _userManager.GetClaimsAsync(user);
        var userTenantIdStr = claims.FirstOrDefault(c => c.Type == "tenantId")?.Value;
        if (userTenantIdStr != tenantId.ToString())
            return (false, "User does not belong to this pharmacy.");

        // Check if user has associated sales
        var appUser = await _db.AppUsers
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Email == user.Email);

        if (appUser is not null)
        {
            var salesCount = await _db.Sales.CountAsync(s => s.PharmacistId == appUser.UserId);
            if (salesCount > 0)
            {
                return (false, $"Cannot delete '{user.Email}' because they have processed {salesCount} sales records. You can change their password or update their role instead.");
            }
            _db.AppUsers.Remove(appUser);
            await _db.SaveChangesAsync();
        }

        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            return (false, string.Join("; ", result.Errors.Select(e => e.Description)));
        }

        return (true, "Pharmacist deleted successfully.");
    }
}
