namespace PMS.Common.DTOs;

public class UserResponseDto
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int TotalSales { get; set; }
    public DateTime UpdatedAt { get; set; }
}
