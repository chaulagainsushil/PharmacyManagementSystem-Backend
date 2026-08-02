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
