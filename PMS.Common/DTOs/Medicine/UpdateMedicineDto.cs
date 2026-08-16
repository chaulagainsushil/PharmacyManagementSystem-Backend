using System.ComponentModel.DataAnnotations;

namespace PMS.Common.DTOs.Medicine;

public class UpdateMedicineDto
{
    [Required(ErrorMessage = "Medicine name is required.")]
    [MaxLength(300, ErrorMessage = "Name must not exceed 300 characters.")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(300, ErrorMessage = "Generic name must not exceed 300 characters.")]
    public string? GenericName { get; set; }

    public int? CategoryId { get; set; }
    public int? ManufacturerId { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Reorder level must be non-negative.")]
    public int ReorderLevel { get; set; } = 10;

    public bool RequiresPrescription { get; set; }
    public bool IsActive { get; set; }
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// When provided, replaces all existing units for this medicine.
    /// Must include exactly one IsBaseUnit=true, exactly one IsDefault=true,
    /// and all UnitPrice > 0.
    /// When omitted (null), existing units are left untouched.
    /// </summary>
    public List<CreateMedicineUnitDto>? Units { get; set; }
}
