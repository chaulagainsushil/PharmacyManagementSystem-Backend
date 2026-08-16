namespace PMS.Models.Models;

/// <summary>
/// Links a Product to one of its Units of Measure with conversion and pricing.
/// 
/// Base unit: user specifies TotalUnitQty (e.g., 10 tablets), 
/// ConversionFactorToBase (e.g., 20 tablets per carton), 
/// and pricing for the full base unit.
///
/// Sub unit (if attached): auto-calculates TotalUnitQty = Base.TotalUnitQty × Base.ConversionFactorToBase,
/// and derives pricing as: UnitPrice = Base.UnitPrice / SubUnit.TotalUnitQty.
/// Both pricing fields are editable even if auto-calculated.
///
/// Exactly one ProductUnit per product must have IsBaseUnit = true.
/// ConversionFactorToBase and TotalUnitQty must both be > 0.
/// </summary>
public class ProductUnit : IHasTenant
{
    public int ProductUnitId { get; set; }

    /// <inheritdoc/>
    public Guid TenantId { get; set; }

    public int ProductId { get; set; }
    public int UnitOfMeasureId { get; set; }

    /// <summary>
    /// True for the base unit of this product (e.g., individual Tablet).
    /// Only one ProductUnit per product can have this flag.
    /// </summary>
    public bool IsBaseUnit { get; set; } = false;

    /// <summary>
    /// Quantity of this unit contained in the next "larger" unit.
    /// For base unit: how many base items per "carton" or "pack".
    /// Example: Tablet is base (factor=1), Strip contains 10 Tablets (factor=10).
    /// Must be > 0.
    /// </summary>
    public decimal ConversionFactorToBase { get; set; } = 1m;

    /// <summary>
    /// Total quantity of this unit when acting as a container/measurement.
    /// For base unit: actual count (e.g., 10 tablets).
    /// For sub unit: auto-calculated as Base.TotalUnitQty × Base.ConversionFactorToBase.
    /// Must be > 0.
    /// </summary>
    public decimal TotalUnitQty { get; set; } = 1m;

    /// <summary>
    /// Purchase price for the quantity specified by TotalUnitQty.
    /// For base unit: cost to purchase TotalUnitQty items at this unit (e.g., cost for 10 tablets).
    /// For sub unit: default is Base.PurchasePrice / SubUnit.TotalUnitQty (editable).
    /// Must be >= 0.
    /// </summary>
    public decimal PurchasePrice { get; set; } = 0m;

    /// <summary>
    /// Sale/retail price for the quantity specified by TotalUnitQty.
    /// For base unit: selling price for TotalUnitQty items at this unit.
    /// For sub unit: default is Base.SalePrice / SubUnit.TotalUnitQty (editable).
    /// Must be >= 0.
    /// </summary>
    public decimal SalePrice { get; set; } = 0m;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // ── Navigation ─────────────────────────────────────────────────────────────
    public Product Product { get; set; } = null!;
    public UnitOfMeasure UnitOfMeasure { get; set; } = null!;
}
