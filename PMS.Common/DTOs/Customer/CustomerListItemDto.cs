namespace PMS.Common.DTOs.Customer;

/// <summary>Lightweight — GET all.</summary>
public class CustomerListItemDto
{
    public int CustomerId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Address { get; set; }
    public int TotalSales { get; set; }
    public DateTime CreatedAt { get; set; }
}
