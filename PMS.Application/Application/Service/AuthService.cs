using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using PMS.Application.Application.Interface.Repository;
using PMS.Application.Application.Interface.Service;
using PMS.Common;
using PMS.Common.DTOs;

namespace PMS.Application.Application.Service;

public class AuthService : IAuthService
{
    private readonly IAuthRepository      _authRepository;
    private readonly ISubscriptionService _subscriptionService;
    private readonly IConfiguration       _configuration;

    public AuthService(
        IAuthRepository      authRepository,
        ISubscriptionService subscriptionService,
        IConfiguration       configuration)
    {
        _authRepository      = authRepository;
        _subscriptionService = subscriptionService;
        _configuration       = configuration;
    }

    // ── Tenant Signup (creates Tenant + Subscription + User in one flow) ──────

    public async Task<AuthResponseDto> TenantSignupAsync(SignupDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.PharmacyName))
        {
            return new AuthResponseDto
            {
                IsSuccess = false,
                Message   = "PharmacyName is required for tenant signup."
            };
        }

        // Step 1 – Create the PharmacyProfile (tenant root)
        var profile = await _authRepository.CreatePharmacyProfileAsync(dto.PharmacyName);

        // Step 2 – Register the Identity user
        var registerResult = await _authRepository.RegisterAsync(dto);
        if (!registerResult.Succeeded)
        {
            return new AuthResponseDto
            {
                IsSuccess = false,
                Message   = string.Join("; ", registerResult.Errors.Select(e => e.Description))
            };
        }

        var identityUser = await _authRepository.FindByEmailAsync(dto.Email);
        if (identityUser is null)
        {
            return new AuthResponseDto { IsSuccess = false, Message = "User registration failed." };
        }

        // Step 3 – Assign PharmacyAdmin role (first user of a tenant is always the admin)
        await _authRepository.AssignRoleAsync(identityUser, AppRoles.PharmacyAdmin);

        // Step 4 – Link the user to the tenant via a persistent claim
        await _authRepository.AddTenantClaimAsync(identityUser, profile.TenantId);

        // Step 5 – Create the initial subscription
        await _subscriptionService.CreateInitialAsync(profile.TenantId, dto.InitialPlan);

        // Step 6 – Auto-login (this also creates the AppUser record)
        return await LoginAsync(new LoginDto { Email = dto.Email, Password = dto.Password });
    }

    // ── Signup (add user to an existing tenant) ───────────────────────────────

    public async Task<AuthResponseDto> SignupAsync(SignupDto dto)
    {
        var result = await _authRepository.RegisterAsync(dto);

        if (!result.Succeeded)
        {
            return new AuthResponseDto
            {
                IsSuccess = false,
                Message   = string.Join("; ", result.Errors.Select(e => e.Description))
            };
        }

        // Assign role — default to Pharmacist when none is supplied
        var role = dto.Role is not null && AppRoles.All.Contains(dto.Role)
            ? dto.Role
            : AppRoles.Pharmacist;

        var identityUser = await _authRepository.FindByEmailAsync(dto.Email);
        if (identityUser is not null)
            await _authRepository.AssignRoleAsync(identityUser, role);

        // Auto-login after successful signup
        return await LoginAsync(new LoginDto { Email = dto.Email, Password = dto.Password });
    }

    // ── CreateUser (admin-initiated) ──────────────────────────────────────────

    public async Task<AuthResponseDto> CreateUserAsync(SignupDto dto)
    {
        var role = dto.Role is not null && AppRoles.All.Contains(dto.Role)
            ? dto.Role
            : AppRoles.Pharmacist;

        var registerResult = await _authRepository.RegisterAsync(dto);

        if (!registerResult.Succeeded)
        {
            return new AuthResponseDto
            {
                IsSuccess = false,
                Message   = string.Join("; ", registerResult.Errors.Select(e => e.Description))
            };
        }

        var identityUser = await _authRepository.FindByEmailAsync(dto.Email);
        if (identityUser is not null)
        {
            var roleResult = await _authRepository.AssignRoleAsync(identityUser, role);
            if (!roleResult.Succeeded)
            {
                return new AuthResponseDto
                {
                    IsSuccess = false,
                    Message   = string.Join("; ", roleResult.Errors.Select(e => e.Description))
                };
            }
        }

        return new AuthResponseDto
        {
            IsSuccess = true,
            Message   = $"User created and assigned role '{role}'.",
            Email     = dto.Email,
            FullName  = dto.FullName,
            Roles     = [role]
        };
    }

    // ── AssignRole (re-assign existing user) ──────────────────────────────────

    public async Task<AuthResponseDto> AssignRoleAsync(AssignRoleDto dto)
    {
        if (!AppRoles.All.Contains(dto.Role))
        {
            return new AuthResponseDto
            {
                IsSuccess = false,
                Message   = $"Unknown role '{dto.Role}'. Valid roles: {string.Join(", ", AppRoles.All)}."
            };
        }

        var identityUser = await _authRepository.FindByEmailAsync(dto.Email);
        if (identityUser is null)
        {
            return new AuthResponseDto
            {
                IsSuccess = false,
                Message   = $"No user found with email '{dto.Email}'."
            };
        }

        var result = await _authRepository.AssignRoleAsync(identityUser, dto.Role);

        return result.Succeeded
            ? new AuthResponseDto
            {
                IsSuccess = true,
                Message   = $"Role '{dto.Role}' assigned to '{dto.Email}'.",
                Email     = dto.Email,
                Roles     = [dto.Role]
            }
            : new AuthResponseDto
            {
                IsSuccess = false,
                Message   = string.Join("; ", result.Errors.Select(e => e.Description))
            };
    }

    // ── Login ─────────────────────────────────────────────────────────────────

    public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
    {
        var user = await _authRepository.ValidateCredentialsAsync(dto);

        if (user is null)
        {
            return new AuthResponseDto
            {
                IsSuccess = false,
                Message   = "Invalid email or password."
            };
        }

        var roles    = await _authRepository.GetUserRolesAsync(user);
        var fullName = await _authRepository.GetFullNameAsync(user);

        // Resolve the tenant associated with this user
        var profile  = await _authRepository.GetPharmacyProfileByUserEmailAsync(dto.Email);
        var tenantId = profile?.TenantId ?? Guid.Empty;

        // Ensure the AppUser (integer-PK) record exists for PharmacistId use in sales
        var appUser = await _authRepository.GetOrCreateAppUserAsync(user, fullName);

        var (token, expiration) = GenerateJwtToken(user, roles, fullName, tenantId);

        return new AuthResponseDto
        {
            IsSuccess  = true,
            Message    = "Login successful.",
            Token      = token,
            Expiration = expiration,
            Email      = user.Email,
            FullName   = fullName,
            Roles      = roles,
            UserId     = appUser.UserId,
            TenantId   = tenantId == Guid.Empty ? null : tenantId
        };
    }

    // ── Change Password ───────────────────────────────────────────────────────

    public async Task<AuthResponseDto> ChangePasswordAsync(string userId, ChangePasswordDto dto)
    {
        var user = await _authRepository.FindByEmailAsync(userId)
                   ?? await FindByIdInternalAsync(userId);

        if (user is null)
        {
            return new AuthResponseDto
            {
                IsSuccess = false,
                Message   = "User not found."
            };
        }

        var result = await _authRepository.ChangePasswordAsync(
            user, dto.CurrentPassword, dto.NewPassword);

        return result.Succeeded
            ? new AuthResponseDto
            {
                IsSuccess = true,
                Message   = "Password changed successfully.",
                Email     = user.Email
            }
            : new AuthResponseDto
            {
                IsSuccess = false,
                Message   = string.Join("; ", result.Errors.Select(e => e.Description))
            };
    }

    // ── Forgot Password ───────────────────────────────────────────────────────

    public async Task<AuthResponseDto> ForgotPasswordAsync(ForgotPasswordDto dto)
    {
        var user = await _authRepository.FindByEmailAsync(dto.Email);

        if (user is null)
        {
            return new AuthResponseDto
            {
                IsSuccess = true,
                Message   = "If that email address is registered, a reset link has been sent."
            };
        }

        var token = await _authRepository.GeneratePasswordResetTokenAsync(user);

        return new AuthResponseDto
        {
            IsSuccess = true,
            Message   = "Password reset token generated. In production, this would be emailed to you.",
            Token     = token,
            Email     = user.Email
        };
    }

    // ── Reset Password ────────────────────────────────────────────────────────

    public async Task<AuthResponseDto> ResetPasswordAsync(ResetPasswordDto dto)
    {
        var user = await _authRepository.FindByEmailAsync(dto.Email);

        if (user is null)
        {
            return new AuthResponseDto
            {
                IsSuccess = false,
                Message   = "Password reset failed. The token may be invalid or expired."
            };
        }

        var result = await _authRepository.ResetPasswordAsync(user, dto.Token, dto.NewPassword);

        return result.Succeeded
            ? new AuthResponseDto
            {
                IsSuccess = true,
                Message   = "Password has been reset successfully. You can now log in.",
                Email     = user.Email
            }
            : new AuthResponseDto
            {
                IsSuccess = false,
                Message   = string.Join("; ", result.Errors.Select(e => e.Description))
            };
    }

    // ── JWT helpers ───────────────────────────────────────────────────────────

    private (string Token, DateTime Expiration) GenerateJwtToken(
        IdentityUser user, IList<string> roles, string fullName, Guid tenantId)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var secretKey   = jwtSettings["SecretKey"]
            ?? throw new InvalidOperationException("JwtSettings:SecretKey is not configured.");

        var key         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub,   user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString()),
            new(ClaimTypes.NameIdentifier,     user.Id),
            new("FullName",                    fullName)
        };

        // Embed tenantId so every request knows which tenant the user belongs to
        if (tenantId != Guid.Empty)
            claims.Add(new Claim("tenantId", tenantId.ToString()));

        // Add each role as a claim
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var expireMinutes = int.TryParse(jwtSettings["ExpiryInMinutes"], out var mins) ? mins : 60;
        var expiration    = DateTime.UtcNow.AddMinutes(expireMinutes);

        var token = new JwtSecurityToken(
            issuer:             jwtSettings["Issuer"],
            audience:           jwtSettings["Audience"],
            claims:             claims,
            expires:            expiration,
            signingCredentials: credentials
        );

        return (new JwtSecurityTokenHandler().WriteToken(token), expiration);
    }

    private async Task<IdentityUser?> FindByIdInternalAsync(string id)
        => await _authRepository.FindByIdAsync(id);
}
