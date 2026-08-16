using PMS.Common.DTOs.Uom;

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

    public int ReorderLevel { get; set; }
    public bool RequiresPrescription { get; set; }
    public bool IsActive { get; set; }

    /// <summary>Total stock across all non-expired batches, expressed in the medicine's base unit.</summary>
    public int TotalStockInBaseUnit { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    /// <summary>All units attached to this medicine (active and inactive), default unit first.</summary>
    public List<MedicineUnitDto> Units { get; set; } = new();
}
