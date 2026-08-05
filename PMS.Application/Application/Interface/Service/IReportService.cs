using PMS.Common.DTOs.Report;

namespace PMS.Application.Application.Interface.Service;

public interface IReportService
{
    Task<DashboardReportDto> GetDashboardReportAsync();
    Task<SalesSummaryDto> GetTodaySalesAsync();
    Task<SalesSummaryDto> GetMonthSalesAsync();
    Task<List<MonthlySalesSummaryDto>> GetMonthlySalesAsync(int months = 12);
    Task<List<LowStockItemDto>> GetLowStockAsync();
    Task<StockConsumptionReportDto> GetStockConsumptionAsync(
        DateTime? from, DateTime? to, int? supplierId, int? manufacturerId, string sortBy = "quantity");
    Task<List<TopMedicineDto>> GetTopMedicinesAsync(int top = 10);
    Task<List<TopManufacturerDto>> GetTopManufacturersAsync(int top = 10);
    Task<List<TopSupplierDto>> GetTopSuppliersAsync(int top = 10);
}
