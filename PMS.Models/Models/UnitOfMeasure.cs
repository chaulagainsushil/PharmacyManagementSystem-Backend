namespace PMS.Models.Models;

/// <summary>
/// Master list of units of measure that can be attached to any medicine within a tenant.
/// Examples: Tablet, Strip, Bottle, Sachet, Packet, Piece, Box.
/// Pharmacists can add custom UoMs as needed — this is not a fixed enum.
/// </summary>
public class UnitOfMeasure : IHasTenant
{
    public int UnitOfMeasureId { get; set; }

    /// <inheritdoc/>
    public Guid TenantId { get; set; }

    /// <summary>Human-readable name, e.g. "Strip", "Bottle".</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Abbreviated symbol, e.g. "tab", "strip", "btl".</summary>
    public string Symbol { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<MedicineUnit> MedicineUnits { get; set; } = new List<MedicineUnit>();
}
