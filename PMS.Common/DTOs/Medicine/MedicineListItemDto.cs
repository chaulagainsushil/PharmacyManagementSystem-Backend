namespace PMS.Common.DTOs.Medicine;

/// <summary>Lightweight response — used for GET all (list).</summary>
public class MedicineListItemDto
{
    public int MedicineId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? GenericName { get; set; }
    public string? CategoryName { get; set; }
    public string? ManufacturerName { get; set; }
    public decimal StripPrice { get; set; }
    public decimal TabletPrice { get; set; }
    public int TotalStockInTablets { get; set; }
    public bool IsActive { get; set; }
    public bool RequiresPrescription { get; set; }
}
