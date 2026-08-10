using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PMS.Application.Application.Interface.Service;
using PMS.Common;
using PMS.Common.DTOs;
using PMS.DependencyHandler;
using System.Security.Claims;

namespace PMS.Controllers;

[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting(RateLimitPolicies.Auth)]   // strict IP limit on all auth endpoints
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    // ── Tenant self-registration ──────────────────────────────────────────────

    /// <summary>
    /// Register a brand-new tenant (pharmacy) and its first PharmacyAdmin user.
    /// Creates PharmacyProfile + initial Subscription + Identity user in one transaction.
    /// Requires PharmacyName in the request body; returns a JWT with tenantId claim on success.
    /// </summary>
    [HttpPost("tenant-signup")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> TenantSignup([FromBody] SignupDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _authService.TenantSignupAsync(dto);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    // ── Public endpoints ──────────────────────────────────────────────────────

    /// <summary>
    /// Register a new user inside an existing tenant.
    /// All self-registered users receive the Pharmacist role.
    /// Use POST /create-user to provision other roles.
    /// Password must be at least 8 characters and contain uppercase, lowercase,
    /// digit, and special character. Common/weak passwords are rejected.
    /// </summary>
    [HttpPost("signup")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Signup([FromBody] SignupDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        // Force role to Pharmacist on self-registration — prevents privilege escalation
        dto.Role = AppRoles.Pharmacist;

        var result = await _authService.SignupAsync(dto);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    /// <summary>Login with email and password.</summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _authService.LoginAsync(dto);
        return result.IsSuccess ? Ok(result) : Unauthorized(result);
    }

    // ── Password management (authenticated) ───────────────────────────────────

    /// <summary>
    /// Changes the password for the currently authenticated user.
    /// Requires the current password for verification.
    /// </summary>
    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(new AuthResponseDto { IsSuccess = false, Message = "Could not identify the current user." });

        var result = await _authService.ChangePasswordAsync(userId, dto);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    // ── Password reset ────────────────────────────────────────────────────────

    /// <summary>
    /// Initiates a password reset for the given email.
    /// Always returns 200 OK to prevent email enumeration attacks.
    /// </summary>
    [HttpPost("forgot-password")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _authService.ForgotPasswordAsync(dto);
        return Ok(result);
    }

    /// <summary>
    /// Resets the user's password using the token returned by POST /forgot-password.
    /// </summary>
    [HttpPost("reset-password")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _authService.ResetPasswordAsync(dto);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    // ── Admin-only endpoints ──────────────────────────────────────────────────

    /// <summary>
    /// Creates a new user and assigns any valid role.
    /// Restricted to SystemAdmin only.
    /// </summary>
    [HttpPost("create-user")]
    [Authorize(Roles = AppRoles.SystemAdmin)]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> CreateUser([FromBody] SignupDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _authService.CreateUserAsync(dto);
        return result.IsSuccess ? StatusCode(StatusCodes.Status201Created, result) : BadRequest(result);
    }

    /// <summary>
    /// Assigns (or re-assigns) a role to an existing user by email.
    /// Restricted to SystemAdmin only.
    /// </summary>
    [HttpPost("assign-role")]
    [Authorize(Roles = AppRoles.SystemAdmin)]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> AssignRole([FromBody] AssignRoleDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _authService.AssignRoleAsync(dto);

        if (!result.IsSuccess && result.Message.Contains("No user found"))
            return NotFound(result);

        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }
}
