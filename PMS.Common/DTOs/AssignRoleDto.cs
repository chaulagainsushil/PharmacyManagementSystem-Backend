using System.ComponentModel.DataAnnotations;

namespace PMS.Common.DTOs;

public class AssignRoleDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// The role to assign. Must be one of the values defined in <see cref="AppRoles"/>.
    /// </summary>
    [Required]
    public string Role { get; set; } = string.Empty;
}
