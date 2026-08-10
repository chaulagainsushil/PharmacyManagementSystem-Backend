namespace PMS.Common.DTOs.Supplier;

/// <summary>Lightweight — GET all.</summary>
public class SupplierListItemDto
{
    public int SupplierId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public int BatchCount { get; set; }
    public DateTime UpdatedAt { get; set; }
}
