namespace PMS.Models.Models;

public class Medicine
{
    public int MedicineId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? GenericName { get; set; }
    public int? CategoryId { get; set; }
    public int? ManufacturerId { get; set; }

    public int TabletsPerStrip { get; set; } = 1;
    public decimal StripPrice { get; set; }
    public decimal TabletPrice { get; set; }

    public int ReorderLevel { get; set; } = 10;
    public bool RequiresPrescription { get; set; } = false;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Category? Category { get; set; }
    public Manufacturer? Manufacturer { get; set; }
    public ICollection<MedicineBatch> Batches { get; set; } = new List<MedicineBatch>();
    public ICollection<SaleItem> SaleItems { get; set; } = new List<SaleItem>();

    // Domain helper — total stock across all batches, in tablets
    public int TotalStockInTablets => Batches?.Sum(b => b.QuantityInTablets) ?? 0;
}
