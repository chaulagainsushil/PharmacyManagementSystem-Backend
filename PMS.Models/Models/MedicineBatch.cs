namespace PMS.Models.Models;

public class MedicineBatch : IHasTenant
{
    public int BatchId { get; set; }

    /// <inheritdoc/>
    public Guid TenantId { get; set; }

    public int MedicineId { get; set; }
    public string BatchNumber { get; set; } = string.Empty;
    public int? SupplierId { get; set; }

    public DateTime? ManufactureDate { get; set; }
    public DateTime ExpiryDate { get; set; }

    /// <summary>
    /// Stock level stored in the medicine's base unit (e.g. Tablets, Capsules, ml, g).
    /// When a purchase is received in a non-base unit (e.g. 50 Strips where 1 Strip = 10 Tablets),
    /// the batch service converts and stores the result here (e.g. 500 Tablets).
    /// </summary>
    public int QuantityInBaseUnit { get; set; }

    /// <summary>
    /// Cost price per one base-unit item (e.g. cost per Tablet, cost per ml).
    /// Stored in base units so that FEFO cost calculations are consistent.
    /// </summary>
    public decimal CostPricePerBaseUnit { get; set; }

    /// <summary>
    /// Optional: the MedicineUnit in which this batch was received/purchased.
    /// Preserved so that purchase history, GRN receipts and batch reports can show
    /// the original purchase unit (e.g. "50 Strips") instead of the converted base quantity.
    /// Null means the batch was entered directly in base units.
    /// </summary>
    public int? PurchaseMedicineUnitId { get; set; }

    /// <summary>
    /// The original purchase quantity in <see cref="PurchaseMedicineUnitId"/> units.
    /// Example: if 50 Strips were received, this is 50 (while QuantityInBaseUnit = 500).
    /// Null when PurchaseMedicineUnitId is null.
    /// </summary>
    public int? PurchaseQuantityInPurchaseUnit { get; set; }

    public DateTime ReceivedDate { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Medicine Medicine { get; set; } = null!;
    public Supplier? Supplier { get; set; }
    public MedicineUnit? PurchaseMedicineUnit { get; set; }
    public ICollection<SaleItem> SaleItems { get; set; } = new List<SaleItem>();

    public bool IsExpired => ExpiryDate.Date < DateTime.UtcNow.Date;
}
