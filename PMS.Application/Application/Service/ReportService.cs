using PMS.Application.Application.Interface.Repository;
using PMS.Application.Application.Interface.Service;
using PMS.Common.DTOs.Report;

namespace PMS.Application.Application.Service;

public class ReportService : IReportService
{
    private readonly IReportRepository _repo;

    public ReportService(IReportRepository repo) => _repo = repo;

    public async Task<DashboardReportDto> GetDashboardReportAsync()
    {
        var today     = await GetTodaySalesAsync();
        var month     = await GetMonthSalesAsync();
        var medicines = await GetTopMedicinesAsync(10);
        var mfrs      = await GetTopManufacturersAsync(10);
        var suppliers = await GetTopSuppliersAsync(10);

        return new DashboardReportDto
        {
            Today            = today,
            ThisMonth        = month,
            TopMedicines     = medicines,
            TopManufacturers = mfrs,
            TopSuppliers     = suppliers,
        };
    }

    public async Task<SalesSummaryDto> GetTodaySalesAsync()
    {
        var from = DateTime.UtcNow.Date;
        var to   = from.AddDays(1);
        return await _repo.GetSalesSummaryAsync(from, to);
    }

    public async Task<SalesSummaryDto> GetMonthSalesAsync()
    {
        var now  = DateTime.UtcNow;
        var from = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var to   = from.AddMonths(1);
        return await _repo.GetSalesSummaryAsync(from, to);
    }

    public Task<List<MonthlySalesSummaryDto>> GetMonthlySalesAsync(int months = 12) =>
        _repo.GetMonthlySalesAsync(months);

    public Task<List<LowStockItemDto>> GetLowStockAsync() =>
        _repo.GetLowStockAsync();

    public Task<StockConsumptionReportDto> GetStockConsumptionAsync(
        DateTime? from, DateTime? to, int? supplierId, int? manufacturerId, string sortBy = "quantity") =>
        _repo.GetStockConsumptionAsync(from, to, supplierId, manufacturerId, sortBy);

    public async Task<List<TopMedicineDto>> GetTopMedicinesAsync(int top = 10)
    {
        var from = DateTime.MinValue;
        var to   = DateTime.MaxValue;
        return await _repo.GetTopMedicinesAsync(from, to, top);
    }

    public async Task<List<TopManufacturerDto>> GetTopManufacturersAsync(int top = 10)
    {
        var from = DateTime.MinValue;
        var to   = DateTime.MaxValue;
        return await _repo.GetTopManufacturersAsync(from, to, top);
    }

    public Task<List<TopSupplierDto>> GetTopSuppliersAsync(int top = 10) =>
        _repo.GetTopSuppliersAsync(top);
}
