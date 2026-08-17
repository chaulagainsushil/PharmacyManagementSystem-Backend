namespace PMS.Common.DTOs.Sale;

/// <summary>
/// Response DTO for a sale line item. Can represent either a Medicine or Product sale.
/// </summary>
public class SaleItemResponseDto
{
    public int SaleItemId { get; set; }
    
    /// <summary>Type of item: "Medicine" or "Product"</summary>
    public string ItemType { get; set; } = "Medicine";
    
    // ── Medicine fields ──
    public int? MedicineId { get; set; }
    public string? MedicineName { get; set; }
    public int? BatchId { get; set; }
    public string? BatchNumber { get; set; }
    public int? MedicineUnitId { get; set; }
    
    // ── Product fields ──
    public int? ProductId { get; set; }
    public string? ProductName { get; set; }
    public int? ProductUnitId { get; set; }

    /// <summary>Human-readable unit name, e.g. "Strip" or "Box".</summary>
    public string UomName { get; set; } = string.Empty;

    /// <summary>Quantity in the customer-facing unit (e.g. 2 strips, 3 boxes).</summary>
    public int Quantity { get; set; }

    /// <summary>Quantity in base units actually deducted from stock (for Medicine only).</summary>
    public int BaseQuantityDeducted { get; set; }

    public decimal UnitPrice { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal LineTotal { get; set; }
}
