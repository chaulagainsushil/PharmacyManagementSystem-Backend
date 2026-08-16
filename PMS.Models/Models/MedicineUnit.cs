namespace PMS.Models.Models;

/// <summary>
/// Links a Medicine to one of its allowable Units of Measure.
/// <para>
/// <b>ConversionFactorToBase</b>: how many base-unit quantities fit in one of this unit.
/// Example: if Tablet is the base unit (factor = 1) and a Strip contains 10 tablets,
/// then Strip.ConversionFactorToBase = 10.
/// </para>
/// <para>
/// Exactly one MedicineUnit per medicine must have <b>IsBaseUnit = true</b> (conversion = 1).
/// Exactly one MedicineUnit per medicine must have <b>IsDefault = true</b> (shown first in POS).
/// Both invariants are also enforced by partial unique indexes in the database.
/// </para>
/// <para>
/// <b>CanPurchase</b>: when true this unit may be used on purchase/batch-receive transactions.
/// <b>CanSell</b>: when true this unit may be selected on a sale transaction.
/// At least one unit per medicine should have <b>CanSell = true</b> for the medicine to be saleable.
/// </para>
/// <para>
/// Pricing is fully independent per unit — do NOT derive one unit's price from another.
/// <b>CostPrice</b> is the purchase cost per this unit.
/// <b>UnitPrice</b> (selling price) and <b>Mrp</b> are the retail prices for this unit.
/// Example: 1 Strip may cost Rs 15 to purchase and sell for Rs 18 MRP,
///          while 1 Tablet costs Rs 1.5 to purchase and sells for Rs 2 MRP.
/// </para>
/// </summary>
public class MedicineUnit : IHasTenant
{
    public int MedicineUnitId { get; set; }

    /// <inheritdoc/>
    public Guid TenantId { get; set; }

    public int MedicineId { get; set; }
    public int UnitOfMeasureId { get; set; }

    /// <summary>
    /// How many base-unit items are in one of this unit.
    /// The base unit itself always has ConversionFactorToBase = 1.
    /// </summary>
    public decimal ConversionFactorToBase { get; set; } = 1m;

    /// <summary>True for the single unit in which stock is physically stored (e.g. Tablet).</summary>
    public bool IsBaseUnit { get; set; } = false;

    /// <summary>True for the unit shown as the default selection in the POS sale screen.</summary>
    public bool IsDefault { get; set; } = false;

    public bool IsActive { get; set; } = true;

    /// <summary>
    /// When true, this unit may be used when receiving stock (purchase / batch-receive).
    /// A purchase quantity in this unit is converted to base-unit stock using ConversionFactorToBase.
    /// </summary>
    public bool CanPurchase { get; set; } = false;

    /// <summary>
    /// When true, this unit may be selected on sale transactions.
    /// If false the unit is purchase-only (internal costing use) and cannot be sold.
    /// </summary>
    public bool CanSell { get; set; } = true;

    /// <summary>
    /// Purchase cost per one of this unit.
    /// Kept independent from <see cref="UnitPrice"/> — do NOT calculate cost from selling price.
    /// </summary>
    public decimal CostPrice { get; set; } = 0m;

    /// <summary>
    /// Retail selling price per one of this unit (what the customer pays).
    /// Independent from <see cref="CostPrice"/> and <see cref="Mrp"/>.
    /// </summary>
    public decimal UnitPrice { get; set; } = 0m;

    /// <summary>
    /// Maximum Retail Price (MRP) printed on the pack for this unit.
    /// Independent from <see cref="UnitPrice"/> — the pharmacy may sell below MRP.
    /// </summary>
    public decimal Mrp { get; set; } = 0m;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Medicine Medicine { get; set; } = null!;
    public UnitOfMeasure UnitOfMeasure { get; set; } = null!;
    public ICollection<MedicineBatch> PurchasedBatches { get; set; } = new List<MedicineBatch>();
}
