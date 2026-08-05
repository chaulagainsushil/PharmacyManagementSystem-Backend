using System.ComponentModel.DataAnnotations;

namespace PMS.Common.DTOs.Uom;

// ── Response ──────────────────────────────────────────────────────────────────

/// <summary>Full detail response for a MedicineUnit row.</summary>
public class MedicineUnitDto
{
    public int MedicineUnitId { get; set; }
    public int MedicineId { get; set; }
    public int UnitOfMeasureId { get; set; }
    public string UomName { get; set; } = string.Empty;
    public string UomSymbol { get; set; } = string.Empty;
    public decimal ConversionFactorToBase { get; set; }
    public bool IsBaseUnit { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; }
    public decimal UnitPrice { get; set; }
}

/// <summary>
/// Lightweight response for the POS unit-selector dropdown.
/// Sorted with the default unit first, then active units alphabetically.
/// </summary>
public class MedicineUnitForPosDto
{
    public int MedicineUnitId { get; set; }
    public string UomName { get; set; } = string.Empty;
    public string UomSymbol { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public decimal ConversionFactorToBase { get; set; }
    public bool IsDefault { get; set; }
    public bool IsBaseUnit { get; set; }
}

// ── Commands ──────────────────────────────────────────────────────────────────

/// <summary>Attach a UoM to a medicine with its conversion factor and price.</summary>
public class AddMedicineUnitCommand
{
    [Required]
    [Range(1, int.MaxValue)]
    public int MedicineId { get; set; }

    [Required]
    [Range(1, int.MaxValue)]
    public int UnitOfMeasureId { get; set; }

    /// <summary>How many base units fit in one of this unit. Must be ≥ 1.</summary>
    [Required]
    [Range(0.000001, double.MaxValue, ErrorMessage = "ConversionFactorToBase must be greater than 0.")]
    public decimal ConversionFactorToBase { get; set; } = 1m;

    [Range(0, double.MaxValue, ErrorMessage = "UnitPrice must be non-negative.")]
    public decimal UnitPrice { get; set; } = 0m;

    /// <summary>Whether this is the physical stock-storage unit (e.g. Tablet). Only one allowed per medicine.</summary>
    public bool IsBaseUnit { get; set; } = false;

    /// <summary>Whether this should be the POS default selection. Only one allowed per medicine.</summary>
    public bool IsDefault { get; set; } = false;
}

/// <summary>Flip the default-unit flag for a medicine.</summary>
public class SetDefaultUnitCommand
{
    [Required]
    [Range(1, int.MaxValue)]
    public int MedicineId { get; set; }

    [Required]
    [Range(1, int.MaxValue)]
    public int MedicineUnitId { get; set; }
}

/// <summary>Update the price for an existing MedicineUnit.</summary>
public class UpdateMedicineUnitPriceCommand
{
    [Required]
    [Range(0, double.MaxValue, ErrorMessage = "UnitPrice must be non-negative.")]
    public decimal UnitPrice { get; set; }
}
