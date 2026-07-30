namespace PMS.Common.DTOs;

public class AuthResponseDto
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Token { get; set; }

    /// <summary>UTC timestamp when the JWT token expires. Null when IsSuccess is false.</summary>
    public DateTime? Expiration { get; set; }

    public string? Email { get; set; }
    public string? FullName { get; set; }
    public IList<string> Roles { get; set; } = new List<string>();

    /// <summary>Integer PK from the AppUsers table — used as PharmacistId when creating sales.</summary>
    public int? UserId { get; set; }
}
