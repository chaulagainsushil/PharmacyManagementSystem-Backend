namespace PMS.Models.Models;

/// <summary>
/// Represents a line item in a sale. Can be either a Medicine or a Product.
/// - For Medicine: MedicineId, BatchId, and MedicineUnitId are populated
/// - For Product: ProductId and ProductUnitId are populated
/// ItemType determines which entity type is being sold.
/// </summary>
public class SaleItem : IHasTenant
{
    public int SaleItemId { get; set; }

    /// <inheritdoc/>
    public Guid TenantId { get; set; }

    public int SaleId { get; set; }

    /// <summary>Type of item: 'Medicine' or 'Product'</summary>
    public string ItemType { get; set; } = "Medicine"; // Default for backward compatibility

    // ── Medicine-specific fields ──────────────────────────────────────────────
    public int? MedicineId { get; set; }
    public int? BatchId { get; set; }

    /// <summary>FK to MedicineUnit — the unit in which the customer purchased this line.</summary>
    public int? MedicineUnitId { get; set; }

    // ── Product-specific fields ───────────────────────────────────────────────
    public int? ProductId { get; set; }

    /// <summary>FK to ProductUnit — the unit in which the customer purchased this product.</summary>
    public int? ProductUnitId { get; set; }

    // ── Common fields ─────────────────────────────────────────────────────────
    /// <summary>Quantity in the customer's chosen unit (e.g. 2 strips, 5 tablets, 3 boxes).</summary>
    public int Quantity { get; set; }

    /// <summary>
    /// Quantity already converted to base units and deducted from batch stock (Medicine only).
    /// For Products, this may be 0 or equal to Quantity depending on business logic.
    /// Stored so that receipts and reports can show both customer-facing and
    /// inventory quantities without re-computing.
    /// </summary>
    public int BaseQuantityDeducted { get; set; }

    /// <summary>Snapshot of Unit price at the time of the sale.</summary>
    public decimal UnitPrice { get; set; }

    public decimal DiscountPercent { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal LineTotal { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Sale Sale { get; set; } = null!;
    public Medicine? Medicine { get; set; }
    public MedicineBatch? Batch { get; set; }
    public MedicineUnit? MedicineUnit { get; set; }
    public Product? Product { get; set; }
    public ProductUnit? ProductUnit { get; set; }
}
