namespace PMS.Common.DTOs.Sale;

public class SaleItemResponseDto
{
    public int SaleItemId { get; set; }
    public int MedicineId { get; set; }
    public string MedicineName { get; set; } = string.Empty;
    public int BatchId { get; set; }
    public string BatchNumber { get; set; } = string.Empty;

    /// <summary>The MedicineUnit used for this line item.</summary>
    public int MedicineUnitId { get; set; }

    /// <summary>Human-readable unit name, e.g. "Strip".</summary>
    public string UomName { get; set; } = string.Empty;

    /// <summary>Quantity in the customer-facing unit (e.g. 2 strips).</summary>
    public int Quantity { get; set; }

    /// <summary>Quantity in base units actually deducted from stock (e.g. 20 tablets).</summary>
    public int BaseQuantityDeducted { get; set; }

    public decimal UnitPrice { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal LineTotal { get; set; }
}
