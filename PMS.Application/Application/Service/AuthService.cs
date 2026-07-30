using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using PMS.Application.Application.Interface.Repository;
using PMS.Application.Application.Interface.Service;
using PMS.Common.DTOs;

namespace PMS.Application.Application.Service;

public class AuthService : IAuthService
{
    private readonly IAuthRepository _authRepository;
    private readonly IConfiguration _configuration;

    public AuthService(IAuthRepository authRepository, IConfiguration configuration)
    {
        _authRepository = authRepository;
        _configuration = configuration;
    }

    public async Task<AuthResponseDto> SignupAsync(SignupDto dto)
    {
        var result = await _authRepository.RegisterAsync(dto);

        if (!result.Succeeded)
        {
            return new AuthResponseDto
            {
                IsSuccess = false,
                Message = string.Join("; ", result.Errors.Select(e => e.Description))
            };
        }

        // Auto-login after successful signup
        var loginDto = new LoginDto { Email = dto.Email, Password = dto.Password };
        return await LoginAsync(loginDto);
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
    {
        var user = await _authRepository.ValidateCredentialsAsync(dto);

        if (user is null)
        {
            return new AuthResponseDto
            {
                IsSuccess = false,
                Message = "Invalid email or password."
            };
        }

        var roles    = await _authRepository.GetUserRolesAsync(user);
        var fullName = await _authRepository.GetFullNameAsync(user);

        // Ensure the AppUser (integer-PK) record exists for PharmacistId use in sales
        var appUser = await _authRepository.GetOrCreateAppUserAsync(user, fullName);

        var (token, expiration) = GenerateJwtToken(user, roles, fullName);

        return new AuthResponseDto
        {
            IsSuccess  = true,
            Message    = "Login successful.",
            Token      = token,
            Expiration = expiration,
            Email      = user.Email,
            FullName   = fullName,
            Roles      = roles,
            UserId     = appUser.UserId
        };
    }

    // ── JWT helpers ───────────────────────────────────────────────────────────

    private (string Token, DateTime Expiration) GenerateJwtToken(
        IdentityUser user, IList<string> roles, string fullName)
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
}
