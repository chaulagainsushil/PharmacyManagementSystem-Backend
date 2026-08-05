namespace PMS.Common.DTOs.MedicineBatch;

/// <summary>Lightweight — GET all.</summary>
public class MedicineBatchListItemDto
{
    public int BatchId { get; set; }
    public int MedicineId { get; set; }
    public string MedicineName { get; set; } = string.Empty;
    public string BatchNumber { get; set; } = string.Empty;
    public string? SupplierName { get; set; }
    public DateTime ExpiryDate { get; set; }
    public int QuantityInTablets { get; set; }
    public decimal PurchasePricePerTablet { get; set; }
    public bool IsExpired { get; set; }
}
