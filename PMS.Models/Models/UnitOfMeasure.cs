namespace PMS.Models.Models;

/// <summary>
/// Master list of units of measure that can be attached to any medicine.
/// Examples: Tablet, Strip, Bottle, Sachet, Packet, Piece, Box.
/// Pharmacists can add custom UoMs as needed — this is not a fixed enum.
/// </summary>
public class UnitOfMeasure
{
    public int UnitOfMeasureId { get; set; }

    /// <summary>Human-readable name, e.g. "Strip", "Bottle".</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Abbreviated symbol, e.g. "tab", "strip", "btl".</summary>
    public string Symbol { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    // Navigation
    public ICollection<MedicineUnit> MedicineUnits { get; set; } = new List<MedicineUnit>();
}
