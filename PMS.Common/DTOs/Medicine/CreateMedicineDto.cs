using System.ComponentModel.DataAnnotations;

namespace PMS.Common.DTOs.Medicine;

/// <summary>
/// Inline unit definition when creating or updating a medicine.
/// <para>Rules enforced at the service layer:</para>
/// <list type="bullet">
///   <item>Exactly one unit must have <see cref="IsBaseUnit"/> = true.</item>
///   <item>Exactly one unit must have <see cref="IsDefault"/> = true (may be the same unit).</item>
///   <item>The base unit must have <see cref="ConversionFactorToBase"/> = 1.</item>
///   <item>No duplicate <see cref="UnitOfMeasureId"/> values within the same request.</item>
///   <item>Prices are independent per unit — do NOT calculate one from another.</item>
/// </list>
/// </summary>
public class CreateMedicineUnitDto
{
    /// <summary>Foreign key to <c>UnitsOfMeasure</c>.</summary>
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "UnitOfMeasureId must be a positive integer.")]
    public int UnitOfMeasureId { get; set; }

    /// <summary>
    /// How many base-unit items are contained in one of this unit.
    /// Example: 1 Strip = 10 Tablets → ConversionFactorToBase = 10.
    /// The base unit itself must always be 1.
    /// </summary>
    [Range(0.000001, double.MaxValue, ErrorMessage = "ConversionFactorToBase must be greater than 0.")]
    public decimal ConversionFactorToBase { get; set; } = 1m;

    /// <summary>Whether this unit may be used on purchase/batch-receive transactions.</summary>
    public bool CanPurchase { get; set; } = false;

    /// <summary>Whether this unit may be selected on sale transactions.</summary>
    public bool CanSell { get; set; } = true;

    /// <summary>Purchase cost per one of this unit. Independent of selling price.</summary>
    [Range(0, double.MaxValue, ErrorMessage = "CostPrice must be non-negative.")]
    public decimal CostPrice { get; set; } = 0m;

    /// <summary>Retail selling price per one of this unit. Independent of cost price.</summary>
    [Range(0, double.MaxValue, ErrorMessage = "UnitPrice must be non-negative.")]
    public decimal UnitPrice { get; set; } = 0m;

    /// <summary>Maximum Retail Price for this unit.</summary>
    [Range(0, double.MaxValue, ErrorMessage = "Mrp must be non-negative.")]
    public decimal Mrp { get; set; } = 0m;

    /// <summary>
    /// True for the single unit in which stock is physically stored (e.g. Tablet).
    /// Exactly one unit per medicine must have this set to true.
    /// </summary>
    public bool IsBaseUnit { get; set; } = false;

    /// <summary>
    /// True for the unit shown as the default in the POS sale screen.
    /// Exactly one unit per medicine must have this set to true.
    /// </summary>
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

    [Range(0, int.MaxValue, ErrorMessage = "Reorder level must be non-negative.")]
    public int ReorderLevel { get; set; } = 10;

    public bool RequiresPrescription { get; set; } = false;
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Units to attach to this medicine. Must contain at least one entry.
    /// Exactly one must have <see cref="CreateMedicineUnitDto.IsBaseUnit"/> = true,
    /// exactly one must have <see cref="CreateMedicineUnitDto.IsDefault"/> = true,
    /// and every entry must have <see cref="CreateMedicineUnitDto.UnitPrice"/> &gt; 0.
    /// </summary>
    [Required(ErrorMessage = "At least one unit is required.")]
    [MinLength(1, ErrorMessage = "At least one unit is required.")]
    public List<CreateMedicineUnitDto> Units { get; set; } = new();
}
