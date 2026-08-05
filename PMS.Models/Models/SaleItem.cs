namespace PMS.Models.Models;

public class SaleItem
{
    public int SaleItemId { get; set; }
    public int SaleId { get; set; }
    public int MedicineId { get; set; }
    public int BatchId { get; set; }

    /// <summary>FK to MedicineUnit — the unit in which the customer purchased this line.</summary>
    public int MedicineUnitId { get; set; }

    /// <summary>Quantity in the customer's chosen unit (e.g. 2 strips, 5 tablets).</summary>
    public int Quantity { get; set; }

    /// <summary>
    /// Quantity already converted to base units and deducted from batch stock.
    /// Stored so that receipts and reports can show both customer-facing and
    /// inventory quantities without re-computing.
    /// </summary>
    public int BaseQuantityDeducted { get; set; }

    /// <summary>Snapshot of MedicineUnit.UnitPrice at the time of the sale.</summary>
    public decimal UnitPrice { get; set; }

    public decimal DiscountPercent { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal LineTotal { get; set; }

    // Navigation
    public Sale Sale { get; set; } = null!;
    public Medicine Medicine { get; set; } = null!;
    public MedicineBatch Batch { get; set; } = null!;
    public MedicineUnit MedicineUnit { get; set; } = null!;
}
