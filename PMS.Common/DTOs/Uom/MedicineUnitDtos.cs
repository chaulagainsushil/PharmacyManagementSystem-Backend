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

    /// <summary>Whether this unit can be used when receiving/purchasing stock.</summary>
    public bool CanPurchase { get; set; }

    /// <summary>Whether this unit can be selected during a sale transaction.</summary>
    public bool CanSell { get; set; }

    /// <summary>Purchase cost per one of this unit (independent of selling price).</summary>
    public decimal CostPrice { get; set; }

    /// <summary>Retail selling price per one of this unit.</summary>
    public decimal UnitPrice { get; set; }

    /// <summary>Maximum Retail Price printed on the pack for this unit.</summary>
    public decimal Mrp { get; set; }

    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Lightweight response for the POS unit-selector dropdown.
/// Sorted with the default unit first, then active units alphabetically.
/// Only includes units where CanSell = true.
/// </summary>
public class MedicineUnitForPosDto
{
    public int MedicineUnitId { get; set; }
    public string UomName { get; set; } = string.Empty;
    public string UomSymbol { get; set; } = string.Empty;

    /// <summary>Retail selling price per one of this unit.</summary>
    public decimal UnitPrice { get; set; }

    /// <summary>MRP for this unit (for display purposes).</summary>
    public decimal Mrp { get; set; }

    public decimal ConversionFactorToBase { get; set; }
    public bool IsDefault { get; set; }
    public bool IsBaseUnit { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Lightweight response for the purchase unit-selector dropdown.
/// Only includes units where CanPurchase = true.
/// </summary>
public class MedicineUnitForPurchaseDto
{
    public int MedicineUnitId { get; set; }
    public string UomName { get; set; } = string.Empty;
    public string UomSymbol { get; set; } = string.Empty;

    /// <summary>Purchase cost per one of this unit.</summary>
    public decimal CostPrice { get; set; }

    public decimal ConversionFactorToBase { get; set; }
    public bool IsBaseUnit { get; set; }
    public DateTime UpdatedAt { get; set; }
}

// ── Commands ──────────────────────────────────────────────────────────────────

/// <summary>Attach a UoM to a medicine with its conversion factor and prices.</summary>
public class AddMedicineUnitCommand
{
    [Required]
    [Range(1, int.MaxValue)]
    public int MedicineId { get; set; }

    [Required]
    [Range(1, int.MaxValue)]
    public int UnitOfMeasureId { get; set; }

    /// <summary>How many base units fit in one of this unit. Must be &gt; 0.</summary>
    [Required]
    [Range(0.000001, double.MaxValue, ErrorMessage = "ConversionFactorToBase must be greater than 0.")]
    public decimal ConversionFactorToBase { get; set; } = 1m;

    /// <summary>Whether this unit may be used on purchase/batch-receive transactions.</summary>
    public bool CanPurchase { get; set; } = false;

    /// <summary>Whether this unit may be selected on sale transactions.</summary>
    public bool CanSell { get; set; } = true;

    /// <summary>Purchase cost per one of this unit.</summary>
    [Range(0, double.MaxValue, ErrorMessage = "CostPrice must be non-negative.")]
    public decimal CostPrice { get; set; } = 0m;

    /// <summary>Retail selling price per one of this unit.</summary>
    [Range(0, double.MaxValue, ErrorMessage = "UnitPrice must be non-negative.")]
    public decimal UnitPrice { get; set; } = 0m;

    /// <summary>Maximum Retail Price for this unit.</summary>
    [Range(0, double.MaxValue, ErrorMessage = "Mrp must be non-negative.")]
    public decimal Mrp { get; set; } = 0m;

    /// <summary>Whether this is the physical stock-storage unit (e.g. Tablet). Only one allowed per medicine.</summary>
    public bool IsBaseUnit { get; set; } = false;

    /// <summary>Whether this should be the POS default selection. Only one allowed per medicine.</summary>
    public bool IsDefault { get; set; } = false;
}

/// <summary>Full update of a MedicineUnit — all mutable fields can be changed in one call.</summary>
public class UpdateMedicineUnitCommand
{
    /// <summary>
    /// How many base units fit in one of this unit.
    /// Cannot be changed on the base unit itself (always 1).
    /// </summary>
    [Range(0.000001, double.MaxValue, ErrorMessage = "ConversionFactorToBase must be greater than 0.")]
    public decimal ConversionFactorToBase { get; set; } = 1m;

    /// <summary>Whether this unit may be used on purchase/batch-receive transactions.</summary>
    public bool CanPurchase { get; set; } = false;

    /// <summary>Whether this unit may be selected on sale transactions.</summary>
    public bool CanSell { get; set; } = true;

    /// <summary>Purchase cost per one of this unit (independent of selling price).</summary>
    [Range(0, double.MaxValue, ErrorMessage = "CostPrice must be non-negative.")]
    public decimal CostPrice { get; set; } = 0m;

    /// <summary>Retail selling price per one of this unit.</summary>
    [Range(0, double.MaxValue, ErrorMessage = "UnitPrice must be non-negative.")]
    public decimal UnitPrice { get; set; } = 0m;

    /// <summary>Maximum Retail Price for this unit.</summary>
    [Range(0, double.MaxValue, ErrorMessage = "Mrp must be non-negative.")]
    public decimal Mrp { get; set; } = 0m;

    /// <summary>Whether this unit should be active.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Optimistic concurrency token — pass the UpdatedAt value from the last GET.</summary>
    public DateTime UpdatedAt { get; set; }
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

    public DateTime? UpdatedAt { get; set; }
}

/// <summary>Update the selling price for an existing MedicineUnit (legacy / quick-price endpoint).</summary>
public class UpdateMedicineUnitPriceCommand
{
    [Required]
    [Range(0, double.MaxValue, ErrorMessage = "UnitPrice must be non-negative.")]
    public decimal UnitPrice { get; set; }

    public DateTime? UpdatedAt { get; set; }
}

// ── Dual-unit endpoint DTOs ────────────────────────────────────────────────────

/// <summary>
/// The base-unit half of the dual-unit add command.
/// ConversionFactorToBase is always 1 for the base unit.
/// </summary>
public class DualUnitBaseEntry
{
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "UnitOfMeasureId must be a positive integer.")]
    public int UnitOfMeasureId { get; set; }

    /// <summary>Purchase cost per one base unit.</summary>
    [Range(0, double.MaxValue, ErrorMessage = "CostPrice must be non-negative.")]
    public decimal CostPrice { get; set; } = 0m;

    /// <summary>Retail selling price per one base unit.</summary>
    [Range(0, double.MaxValue, ErrorMessage = "UnitPrice must be non-negative.")]
    public decimal UnitPrice { get; set; } = 0m;

    /// <summary>Maximum Retail Price for the base unit.</summary>
    [Range(0, double.MaxValue, ErrorMessage = "Mrp must be non-negative.")]
    public decimal Mrp { get; set; } = 0m;

    /// <summary>Whether this base unit can be purchased (batch-receive).</summary>
    public bool CanPurchase { get; set; } = true;

    /// <summary>Whether this base unit can be sold at POS.</summary>
    public bool CanSell { get; set; } = true;

    /// <summary>Whether this base unit should be the POS default.</summary>
    public bool IsDefault { get; set; } = true;
}

/// <summary>
/// The sub-unit half of the dual-unit add command.
/// ConversionFactorToBase indicates how many base units are in one sub unit (e.g. 10 tablets per strip).
/// </summary>
public class DualUnitSubEntry
{
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "UnitOfMeasureId must be a positive integer.")]
    public int UnitOfMeasureId { get; set; }

    /// <summary>How many base units fit in one sub unit. Must be &gt; 0.</summary>
    [Required]
    [Range(0.000001, double.MaxValue, ErrorMessage = "ConversionFactorToBase must be greater than 0.")]
    public decimal ConversionFactorToBase { get; set; } = 1m;

    /// <summary>Purchase cost per one sub unit.</summary>
    [Range(0, double.MaxValue, ErrorMessage = "CostPrice must be non-negative.")]
    public decimal CostPrice { get; set; } = 0m;

    /// <summary>
    /// Retail selling price per one sub unit.
    /// Ignored when the parent command's <see cref="AddDualUnitCommand.UseAutoCalculatedPrice"/> is true;
    /// in that case the server computes UnitPrice = BaseUnit.UnitPrice / ConversionFactorToBase.
    /// </summary>
    [Range(0, double.MaxValue, ErrorMessage = "UnitPrice must be non-negative.")]
    public decimal UnitPrice { get; set; } = 0m;

    /// <summary>Maximum Retail Price for the sub unit.</summary>
    [Range(0, double.MaxValue, ErrorMessage = "Mrp must be non-negative.")]
    public decimal Mrp { get; set; } = 0m;

    /// <summary>Whether this sub unit can be purchased (batch-receive).</summary>
    public bool CanPurchase { get; set; } = true;

    /// <summary>Whether this sub unit can be sold at POS.</summary>
    public bool CanSell { get; set; } = true;
}

/// <summary>
/// Command for the convenience endpoint POST /api/medicines/{id}/units.
/// Attaches a base unit and optionally a sub unit (e.g. Tablet + Strip) in a single request.
/// </summary>
public class AddDualUnitCommand
{
    [Required]
    public DualUnitBaseEntry BaseUnit { get; set; } = null!;

    /// <summary>
    /// Optional sub unit (e.g. Strip, Box).
    /// When null only the base unit is created.
    /// </summary>
    public DualUnitSubEntry? SubUnit { get; set; }

    /// <summary>
    /// When true and <see cref="SubUnit"/> is present, the server computes
    /// SubUnit.UnitPrice = BaseUnit.UnitPrice / SubUnit.ConversionFactorToBase,
    /// ignoring the submitted SubUnit.UnitPrice.
    /// Set to false to trust the submitted price.
    /// </summary>
    public bool UseAutoCalculatedPrice { get; set; } = false;
}

// ── Response with computed totals ─────────────────────────────────────────────

/// <summary>A single unit row augmented with TotalInBaseUnits for reporting.</summary>
public class MedicineUnitWithTotalsDto : MedicineUnitDto
{
    /// <summary>
    /// Quantity of this unit expressed in base units.
    /// Calculated as Quantity × ConversionFactorToBase.
    /// Note: Quantity here refers to the medicine's total stock expressed in this unit's own
    /// terms — this is provided by the caller if known; otherwise 0 when called from pure
    /// catalogue endpoints.
    /// </summary>
    public decimal TotalInBaseUnits { get; set; }
}

/// <summary>Response wrapper for GET /api/medicines/{id}/units.</summary>
public class MedicineUnitsWithTotalsResponse
{
    public int MedicineId { get; set; }
    public string MedicineName { get; set; } = string.Empty;
    public IReadOnlyList<MedicineUnitWithTotalsDto> Units { get; set; } =
        Array.Empty<MedicineUnitWithTotalsDto>();
}
