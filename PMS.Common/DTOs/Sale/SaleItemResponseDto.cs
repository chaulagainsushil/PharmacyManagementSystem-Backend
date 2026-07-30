using PMS.Models.Enums;

namespace PMS.Common.DTOs.Sale;

public class SaleItemResponseDto
{
    public int SaleItemId { get; set; }
    public int MedicineId { get; set; }
    public string MedicineName { get; set; } = string.Empty;
    public int BatchId { get; set; }
    public string BatchNumber { get; set; } = string.Empty;

    public SaleUnitType SaleUnitType { get; set; }
    public int Quantity { get; set; }
    public int TabletsDeducted { get; set; }
    public decimal UnitPrice { get; set; }

    public decimal DiscountPercent { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal LineTotal { get; set; }
}
