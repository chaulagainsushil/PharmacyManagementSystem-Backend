namespace PMS.Common.DTOs.Medicine;

using PMS.Common.DTOs.Uom;

/// <summary>Lightweight response — used for GET all (list).</summary>
public class MedicineListItemDto
{
    public int MedicineId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? GenericName { get; set; }
    public string? CategoryName { get; set; }
    public string? ManufacturerName { get; set; }

    /// <summary>Total stock across all non-expired batches, in the medicine's base unit.</summary>
    public int TotalStockInBaseUnit { get; set; }

    public int ReorderLevel { get; set; }
    public bool IsActive { get; set; }
    public bool RequiresPrescription { get; set; }
    public DateTime UpdatedAt { get; set; }

    /// <summary>Default unit for quick display (name + price). Null if no units attached.</summary>
    public string? DefaultUnitName { get; set; }
    public decimal? DefaultUnitPrice { get; set; }

    /// <summary>All active units configured for this medicine (includes pricing, conversions, etc.).</summary>
    public List<MedicineUnitDto> Units { get; set; } = new();
}
