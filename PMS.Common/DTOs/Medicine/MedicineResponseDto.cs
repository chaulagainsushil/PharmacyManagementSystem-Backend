namespace PMS.Common.DTOs.Medicine;

/// <summary>Full detail response — used for GET by ID.</summary>
public class MedicineResponseDto
{
    public int MedicineId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? GenericName { get; set; }

    public int? CategoryId { get; set; }
    public string? CategoryName { get; set; }

    public int? ManufacturerId { get; set; }
    public string? ManufacturerName { get; set; }

    public int TabletsPerStrip { get; set; }
    public decimal StripPrice { get; set; }
    public decimal TabletPrice { get; set; }

    public int ReorderLevel { get; set; }
    public bool RequiresPrescription { get; set; }
    public bool IsActive { get; set; }

    public int TotalStockInTablets { get; set; }
    public DateTime CreatedAt { get; set; }
}
