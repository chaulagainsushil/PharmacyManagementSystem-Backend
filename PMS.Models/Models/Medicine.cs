namespace PMS.Models.Models;

public class Medicine : IHasTenant
{
    public int MedicineId { get; set; }

    /// <inheritdoc/>
    public Guid TenantId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? GenericName { get; set; }
    public int? CategoryId { get; set; }
    public int? ManufacturerId { get; set; }

    // ── Legacy fields kept for backwards-compatibility with existing batch data ──
    // Stock is always stored in base-unit (tablet/piece) quantities in MedicineBatch.
    // These fields now drive the default MedicineUnit seeding during migration.
    public int TabletsPerStrip { get; set; } = 1;

    /// <summary>How many strips/straps are packed in one box.</summary>
    public int StrapsPerBox { get; set; } = 1;

    /// <summary>Computed: total base-units that fit in one box.</summary>
    public int TotalTabletsPerBox => TabletsPerStrip * StrapsPerBox;

    // ── Legacy pricing — still used by MedicineUnit.UnitPrice seeding ──────────
    public decimal StripPrice { get; set; }
    public decimal TabletPrice { get; set; }

    public int ReorderLevel { get; set; } = 10;
    public bool RequiresPrescription { get; set; } = false;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // ── Navigation ─────────────────────────────────────────────────────────────
    public Category? Category { get; set; }
    public Manufacturer? Manufacturer { get; set; }
    public ICollection<MedicineBatch> Batches { get; set; } = new List<MedicineBatch>();
    public ICollection<SaleItem> SaleItems { get; set; } = new List<SaleItem>();
    public ICollection<MedicineUnit> Units { get; set; } = new List<MedicineUnit>();

    // ── Domain helpers ─────────────────────────────────────────────────────────

    /// <summary>Total stock across all batches, in base units.</summary>
    public int TotalStockInTablets => Batches?.Sum(b => b.QuantityInTablets) ?? 0;

    /// <summary>
    /// Convert a quantity in the given <paramref name="medicineUnitId"/> to base-unit quantity.
    /// Throws <see cref="InvalidOperationException"/> if the unit is not found on this medicine
    /// or if <see cref="Units"/> has not been loaded.
    /// </summary>
    public int ConvertToBaseQuantity(int medicineUnitId, int quantity)
    {
        var unit = Units.FirstOrDefault(u => u.MedicineUnitId == medicineUnitId)
            ?? throw new InvalidOperationException(
                $"MedicineUnit {medicineUnitId} not found on Medicine {MedicineId}. " +
                "Ensure Units collection is loaded before calling ConvertToBaseQuantity.");

        return (int)Math.Ceiling(quantity * unit.ConversionFactorToBase);
    }

    /// <summary>
    /// Returns the price for the given MedicineUnit, falling back to the unit's stored price.
    /// </summary>
    public decimal GetUnitPrice(int medicineUnitId)
    {
        var unit = Units.FirstOrDefault(u => u.MedicineUnitId == medicineUnitId)
            ?? throw new InvalidOperationException(
                $"MedicineUnit {medicineUnitId} not found on Medicine {MedicineId}.");

        return unit.UnitPrice;
    }

    /// <summary>
    /// Sets the specified MedicineUnit as the base unit, unsetting any previous base unit.
    /// The caller must persist the entity after calling this.
    /// </summary>
    public void SetBaseUnit(int medicineUnitId)
    {
        foreach (var u in Units)
            u.IsBaseUnit = (u.MedicineUnitId == medicineUnitId);

        var target = Units.FirstOrDefault(u => u.MedicineUnitId == medicineUnitId)
            ?? throw new InvalidOperationException(
                $"MedicineUnit {medicineUnitId} not found on Medicine {MedicineId}.");

        // Base unit must have conversion factor of 1
        target.ConversionFactorToBase = 1m;
    }

    /// <summary>
    /// Sets the specified MedicineUnit as the default POS unit, unsetting any previous default.
    /// The caller must persist the entity after calling this.
    /// </summary>
    public void SetDefaultUnit(int medicineUnitId)
    {
        if (!Units.Any(u => u.MedicineUnitId == medicineUnitId))
            throw new InvalidOperationException(
                $"MedicineUnit {medicineUnitId} not found on Medicine {MedicineId}.");

        foreach (var u in Units)
            u.IsDefault = (u.MedicineUnitId == medicineUnitId);
    }
}
