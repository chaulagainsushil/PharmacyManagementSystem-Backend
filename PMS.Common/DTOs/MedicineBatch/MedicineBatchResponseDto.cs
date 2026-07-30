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
    public int QuantityInTablets { get; set; }
    public decimal PurchasePricePerTablet { get; set; }
    public DateTime ReceivedDate { get; set; }
    public bool IsExpired { get; set; }
}
