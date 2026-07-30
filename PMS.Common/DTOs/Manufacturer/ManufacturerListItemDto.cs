namespace PMS.Common.DTOs.Manufacturer;

/// <summary>Lightweight — GET all.</summary>
public class ManufacturerListItemDto
{
    public int ManufacturerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int MedicineCount { get; set; }
}
