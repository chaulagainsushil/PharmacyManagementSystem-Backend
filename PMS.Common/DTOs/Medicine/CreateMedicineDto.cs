using System.ComponentModel.DataAnnotations;

namespace PMS.Common.DTOs.Medicine;

/// <summary>
/// Inline unit definition when creating or updating a medicine.
/// Exactly one unit must have IsBaseUnit = true and exactly one must have IsDefault = true
/// (they can be the same unit).
/// </summary>
public class CreateMedicineUnitDto
{
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "UnitOfMeasureId must be a positive integer.")]
    public int UnitOfMeasureId { get; set; }

    /// <summary>How many base units fit in one of this unit (e.g. Strip = 10 tablets). Must be > 0.</summary>
    [Range(0.000001, double.MaxValue, ErrorMessage = "ConversionFactorToBase must be greater than 0.")]
    public decimal ConversionFactorToBase { get; set; } = 1m;

    [Range(0, double.MaxValue, ErrorMessage = "UnitPrice must be non-negative.")]
    public decimal UnitPrice { get; set; } = 0m;

    /// <summary>The physical stock-storage unit. Exactly one per medicine.</summary>
    public bool IsBaseUnit { get; set; } = false;

    /// <summary>The default POS selection. Exactly one per medicine.</summary>
    public bool IsDefault { get; set; } = false;
}

public class CreateMedicineDto
{
    [Required(ErrorMessage = "Medicine name is required.")]
    [MaxLength(300, ErrorMessage = "Name must not exceed 300 characters.")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(300, ErrorMessage = "Generic name must not exceed 300 characters.")]
    public string? GenericName { get; set; }

    public int? CategoryId { get; set; }
    public int? ManufacturerId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "TabletsPerStrip must be at least 1.")]
    public int TabletsPerStrip { get; set; } = 1;

    [Range(1, int.MaxValue, ErrorMessage = "StrapsPerBox must be at least 1.")]
    public int StrapsPerBox { get; set; } = 1;

    [Range(0, double.MaxValue, ErrorMessage = "Strip price must be non-negative.")]
    public decimal StripPrice { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Tablet price must be non-negative.")]
    public decimal TabletPrice { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Reorder level must be non-negative.")]
    public int ReorderLevel { get; set; } = 10;

    public bool RequiresPrescription { get; set; } = false;
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Optional inline units. When provided, must include exactly one IsBaseUnit=true
    /// and exactly one IsDefault=true. If omitted, no units are attached at creation time.
    /// </summary>
    public List<CreateMedicineUnitDto>? Units { get; set; }
}
