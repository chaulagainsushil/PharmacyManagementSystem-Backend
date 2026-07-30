namespace PMS.Common.DTOs.Manufacturer;

/// <summary>Full detail — GET by ID.</summary>
public class ManufacturerResponseDto
{
    public int ManufacturerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int MedicineCount { get; set; }
}
