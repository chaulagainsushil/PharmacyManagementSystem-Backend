namespace PMS.Common.DTOs.Supplier;

/// <summary>Full detail — GET by ID.</summary>
public class SupplierResponseDto
{
    public int SupplierId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public int BatchCount { get; set; }
}
