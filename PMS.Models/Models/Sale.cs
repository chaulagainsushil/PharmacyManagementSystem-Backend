using PMS.Models.Enums;

namespace PMS.Models.Models;

public class Sale
{
    public int SaleId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;

    public int CustomerId { get; set; }
    public int PharmacistId { get; set; }
    public DateTime SaleDate { get; set; } = DateTime.UtcNow;

    public decimal Subtotal { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }

    public PaymentMode PaymentMode { get; set; } = PaymentMode.Cash;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Customer Customer { get; set; } = null!;
    public User Pharmacist { get; set; } = null!;
    public ICollection<SaleItem> SaleItems { get; set; } = new List<SaleItem>();
}
