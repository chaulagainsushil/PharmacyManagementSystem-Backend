using PMS.Common.DTOs.Report;

namespace PMS.Application.Application.Interface.Service;

public interface IReportService
{
    Task<DashboardReportDto> GetDashboardReportAsync();
    Task<SalesSummaryDto> GetTodaySalesAsync();
    Task<SalesSummaryDto> GetMonthSalesAsync();
    Task<List<TopMedicineDto>> GetTopMedicinesAsync(int top = 10);
    Task<List<TopManufacturerDto>> GetTopManufacturersAsync(int top = 10);
    Task<List<TopSupplierDto>> GetTopSuppliersAsync(int top = 10);
}
