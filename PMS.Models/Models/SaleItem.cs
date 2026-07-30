using PMS.Models.Enums;

namespace PMS.Models.Models;

public class SaleItem
{
    public int SaleItemId { get; set; }
    public int SaleId { get; set; }
    public int MedicineId { get; set; }
    public int BatchId { get; set; }

    public SaleUnitType SaleUnitType { get; set; }
    public int Quantity { get; set; }              // strip count OR tablet count, depending on SaleUnitType
    public decimal UnitPrice { get; set; }          // snapshot of tablet_price/strip_price at sale time

    public decimal DiscountPercent { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal LineTotal { get; set; }

    public Sale Sale { get; set; } = null!;
    public Medicine Medicine { get; set; } = null!;
    public MedicineBatch Batch { get; set; } = null!;

    // Domain helper — tablets actually removed from inventory for this line
    public int TabletsDeducted => SaleUnitType == SaleUnitType.Strip
        ? Quantity * Medicine.TabletsPerStrip
        : Quantity;
}
