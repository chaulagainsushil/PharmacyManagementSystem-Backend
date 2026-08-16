using System.ComponentModel.DataAnnotations;

namespace PMS.Common.DTOs.Product;

// ── Base Unit Entry ────────────────────────────────────────────────────────

/// <summary>
/// First unit (base unit) in a dual-unit attach request.
/// User provides all values.
/// </summary>
public class DualUnitFirstEntry
{
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "UnitOfMeasureId must be a positive integer.")]
    public int UnitOfMeasureId { get; set; }

    /// <summary>Total quantity of this base unit (e.g., 10 tablets per pack).</summary>
    [Required]
    [Range(0.000001, double.MaxValue, ErrorMessage = "TotalUnitQty must be greater than 0.")]
    public decimal TotalUnitQty { get; set; } = 1m;

    /// <summary>How many of this unit fit in one "container" (e.g., 20 tablets per strip).</summary>
    [Required]
    [Range(0.000001, double.MaxValue, ErrorMessage = "ConversionFactorToBase must be greater than 0.")]
    public decimal ConversionFactorToBase { get; set; } = 1m;

    /// <summary>Purchase price for TotalUnitQty items.</summary>
    [Range(0, double.MaxValue, ErrorMessage = "PurchasePrice must be non-negative.")]
    public decimal PurchasePrice { get; set; } = 0m;

    /// <summary>Sale price for TotalUnitQty items.</summary>
    [Range(0, double.MaxValue, ErrorMessage = "SalePrice must be non-negative.")]
    public decimal SalePrice { get; set; } = 0m;
}

// ── Sub Unit Entry ─────────────────────────────────────────────────────────

/// <summary>
/// Second unit (sub unit) in a dual-unit attach request.
/// User provides UnitOfMeasureId; other fields are auto-calculated with option to override.
/// </summary>
public class DualUnitSecondEntry
{
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "UnitOfMeasureId must be a positive integer.")]
    public int UnitOfMeasureId { get; set; }

    /// <summary>
    /// User-provided purchase price (optional; if not provided, auto-calculated).
    /// When auto-calculated: FirstUnit.PurchasePrice / SecondUnit.TotalUnitQty.
    /// </summary>
    [Range(0, double.MaxValue, ErrorMessage = "PurchasePrice must be non-negative.")]
    public decimal? PurchasePrice { get; set; }

    /// <summary>
    /// User-provided sale price (optional; if not provided, auto-calculated).
    /// When auto-calculated: FirstUnit.SalePrice / SecondUnit.TotalUnitQty.
    /// </summary>
    [Range(0, double.MaxValue, ErrorMessage = "SalePrice must be non-negative.")]
    public decimal? SalePrice { get; set; }
}

// ── Dual-Unit Command ──────────────────────────────────────────────────────

/// <summary>
/// Command for POST /api/products/{id}/units.
/// Attaches a base unit and optionally a sub unit in one call.
/// </summary>
public class AddDualUnitCommand
{
    [Required]
    public DualUnitFirstEntry FirstUnit { get; set; } = null!;

    /// <summary>
    /// Optional second unit. When null, only the first unit is created.
    /// </summary>
    public DualUnitSecondEntry? SecondUnit { get; set; }
}

// ── Response DTOs ──────────────────────────────────────────────────────────

/// <summary>
/// Response DTO for a single ProductUnit with all computed values.
/// </summary>
public class ProductUnitDto
{
    public int ProductUnitId { get; set; }
    public int ProductId { get; set; }
    public int UnitOfMeasureId { get; set; }
    public string UomName { get; set; } = string.Empty;
    public string UomSymbol { get; set; } = string.Empty;
    public bool IsBaseUnit { get; set; }
    public decimal ConversionFactorToBase { get; set; }
    public decimal TotalUnitQty { get; set; }
    public decimal PurchasePrice { get; set; }
    public decimal SalePrice { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Response wrapper for GET /api/products/{id}/units.
/// </summary>
public class ProductUnitsResponse
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public IReadOnlyList<ProductUnitDto> Units { get; set; } = Array.Empty<ProductUnitDto>();
}
