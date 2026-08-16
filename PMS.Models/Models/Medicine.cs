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

    /// <summary>Total stock across all batches, in base units (computed — not mapped to DB).</summary>
    public int TotalStockInBaseUnit => Batches?.Sum(b => b.QuantityInBaseUnit) ?? 0;

    /// <summary>
    /// Convert a quantity in the given <paramref name="medicineUnitId"/> to base-unit quantity.
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
    /// Returns the selling price for the given MedicineUnit.
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
