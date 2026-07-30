namespace PMS.Models.Models;

public class MedicineBatch
{
    public int BatchId { get; set; }
    public int MedicineId { get; set; }
    public string BatchNumber { get; set; } = string.Empty;
    public int? SupplierId { get; set; }

    public DateTime? ManufactureDate { get; set; }
    public DateTime ExpiryDate { get; set; }

    public int QuantityInTablets { get; set; }
    public decimal PurchasePricePerTablet { get; set; }
    public DateTime ReceivedDate { get; set; } = DateTime.UtcNow;

    public Medicine Medicine { get; set; } = null!;
    public Supplier? Supplier { get; set; }
    public ICollection<SaleItem> SaleItems { get; set; } = new List<SaleItem>();

    public bool IsExpired => ExpiryDate.Date < DateTime.UtcNow.Date;
}
