namespace PMS.Common.DTOs.MedicineBatch;

/// <summary>Full detail — GET by ID.</summary>
public class MedicineBatchResponseDto
{
    public int BatchId { get; set; }
    public int MedicineId { get; set; }
    public string MedicineName { get; set; } = string.Empty;
    public string BatchNumber { get; set; } = string.Empty;
    public int? SupplierId { get; set; }
    public string? SupplierName { get; set; }
    public DateTime? ManufactureDate { get; set; }
    public DateTime ExpiryDate { get; set; }

    /// <summary>Current stock expressed in the medicine's base unit (e.g. Tablets, Capsules, ml).</summary>
    public int QuantityInBaseUnit { get; set; }

    /// <summary>Purchase cost per one base-unit item.</summary>
    public decimal CostPricePerBaseUnit { get; set; }

    /// <summary>The MedicineUnit used when this batch was received, if entered in a non-base unit.</summary>
    public int? PurchaseMedicineUnitId { get; set; }

    /// <summary>Human-readable name of the purchase unit (e.g. "Strip").</summary>
    public string? PurchaseUnitName { get; set; }

    /// <summary>
    /// Original quantity received in the purchase unit.
    /// Null when the batch was entered directly in base units.
    /// </summary>
    public int? PurchaseQuantityInPurchaseUnit { get; set; }

    public DateTime ReceivedDate { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsExpired { get; set; }
}
