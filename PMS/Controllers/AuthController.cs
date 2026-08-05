using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PMS.Application.Application.Interface.Service;
using PMS.Common;
using PMS.Common.DTOs;

namespace PMS.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    // ── Public endpoints ──────────────────────────────────────────────────────

    /// <summary>
    /// Register a new user account.
    /// The Role field is ignored here — all self-registered users receive the
    /// Pharmacist role by default. Use POST /create-user to provision other roles.
    /// </summary>
    [HttpPost("signup")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status400BadRequest)]
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
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _authService.LoginAsync(dto);
        return result.IsSuccess ? Ok(result) : Unauthorized(result);
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
