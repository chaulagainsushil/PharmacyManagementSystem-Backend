namespace PMS.Common.DTOs.MedicineBatch;

/// <summary>Lightweight — GET all / list views.</summary>
public class MedicineBatchListItemDto
{
    public int BatchId { get; set; }
    public int MedicineId { get; set; }
    public string MedicineName { get; set; } = string.Empty;
    public string BatchNumber { get; set; } = string.Empty;
    public string? SupplierName { get; set; }
    public DateTime ExpiryDate { get; set; }

    /// <summary>Current stock in the medicine's base unit.</summary>
    public int QuantityInBaseUnit { get; set; }

    /// <summary>Purchase cost per one base-unit item.</summary>
    public decimal CostPricePerBaseUnit { get; set; }

    /// <summary>Human-readable name of the purchase unit, if any (e.g. "Strip").</summary>
    public string? PurchaseUnitName { get; set; }

    /// <summary>Original quantity received in the purchase unit, if available.</summary>
    public int? PurchaseQuantityInPurchaseUnit { get; set; }

    public DateTime UpdatedAt { get; set; }
    public bool IsExpired { get; set; }
}
