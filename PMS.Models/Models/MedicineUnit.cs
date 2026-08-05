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
/// </summary>
public class MedicineUnit
{
    public int MedicineUnitId { get; set; }
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

    public decimal UnitPrice { get; set; } = 0m;

    // Navigation
    public Medicine Medicine { get; set; } = null!;
    public UnitOfMeasure UnitOfMeasure { get; set; } = null!;
}
