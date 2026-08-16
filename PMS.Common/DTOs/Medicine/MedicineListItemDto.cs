namespace PMS.Common.DTOs.Medicine;

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
}
