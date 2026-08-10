using PMS.Models.Enums;

namespace PMS.Common.DTOs.Sale;

public class SaleResponseDto
{
    public int SaleId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;

    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public int PharmacistId { get; set; }
    public string PharmacistName { get; set; } = string.Empty;

    public DateTime SaleDate { get; set; }
    public PaymentMode PaymentMode { get; set; }

    public decimal Subtotal { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime UpdatedAt { get; set; }

    public List<SaleItemResponseDto> Items { get; set; } = new();
}
