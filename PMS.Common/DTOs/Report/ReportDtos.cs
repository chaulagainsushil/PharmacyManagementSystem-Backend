namespace PMS.Common.DTOs.Report;

public class SalesSummaryDto
{
    public int TotalSales { get; set; }
    public decimal TotalRevenue { get; set; }
    public int TotalItemsSold { get; set; }
}

public class TopMedicineDto
{
    public int MedicineId { get; set; }
    public string MedicineName { get; set; } = string.Empty;
    public int TotalQuantitySold { get; set; }    // in tablets
    public decimal TotalRevenue { get; set; }
}

public class TopManufacturerDto
{
    public int ManufacturerId { get; set; }
    public string ManufacturerName { get; set; } = string.Empty;
    public int TotalMedicinesSold { get; set; }   // unique medicines sold
    public decimal TotalRevenue { get; set; }
}

public class TopSupplierDto
{
    public int SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public int TotalBatchesSupplied { get; set; }
    public int TotalTabletsSupplied { get; set; }
}

public class DashboardReportDto
{
    public SalesSummaryDto Today { get; set; } = new();
    public SalesSummaryDto ThisMonth { get; set; } = new();
    public List<TopMedicineDto> TopMedicines { get; set; } = new();
    public List<TopManufacturerDto> TopManufacturers { get; set; } = new();
    public List<TopSupplierDto> TopSuppliers { get; set; } = new();
}

public class MonthlySalesSummaryDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    /// <summary>Month name, e.g. "Jan 2026"</summary>
    public string MonthLabel { get; set; } = string.Empty;
    public int TotalSales { get; set; }
    public decimal TotalRevenue { get; set; }
    public int TotalItemsSold { get; set; }
}

public class LowStockItemDto
{
    public int MedicineId { get; set; }
    public string MedicineName { get; set; } = string.Empty;
    public string? GenericName { get; set; }
    public string? CategoryName { get; set; }
    public string? ManufacturerName { get; set; }
    public int TotalStockInTablets { get; set; }
    public int ReorderLevel { get; set; }
    public int ShortfallInTablets { get; set; }
}

public class StockConsumptionItemDto
{
    public int MedicineId { get; set; }
    public string MedicineName { get; set; } = string.Empty;
    public string? GenericName { get; set; }
    public string? CategoryName { get; set; }
    public string? ManufacturerName { get; set; }
    public string? SupplierName { get; set; }
    public int TotalQuantitySold { get; set; }   // tablets
    public int TotalSalesCount { get; set; }
    public decimal TotalRevenue { get; set; }
}

public class StockConsumptionReportDto
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int? SupplierId { get; set; }
    public int? ManufacturerId { get; set; }
    public List<StockConsumptionItemDto> Items { get; set; } = new();
    public int TotalQuantitySold { get; set; }
    public decimal TotalRevenue { get; set; }
}
