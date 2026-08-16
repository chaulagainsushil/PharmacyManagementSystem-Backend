using Microsoft.EntityFrameworkCore;
using PMS.Application.Application.Interface.Repository;
using PMS.Common.DTOs.Report;
using PMS.Repository.Data;

namespace PMS.Application.Application.Repository;

public class ReportRepository : IReportRepository
{
    private readonly ApplicationDbContext _db;

    public ReportRepository(ApplicationDbContext db) => _db = db;

    // ── Sales Summary ─────────────────────────────────────────────────────────

    public async Task<SalesSummaryDto> GetSalesSummaryAsync(DateTime from, DateTime to)
    {
        var sales = await _db.Sales
            .Where(s => s.SaleDate >= from && s.SaleDate < to)
            .ToListAsync();

        var itemCount = await _db.SaleItems
            .Where(si => si.Sale.SaleDate >= from && si.Sale.SaleDate < to)
            .SumAsync(si => (int?)si.Quantity) ?? 0;

        return new SalesSummaryDto
        {
            TotalSales     = sales.Count,
            TotalRevenue   = sales.Sum(s => s.TotalAmount),
            TotalItemsSold = itemCount,
        };
    }

    // ── Monthly Breakdown ─────────────────────────────────────────────────────

    public async Task<List<MonthlySalesSummaryDto>> GetMonthlySalesAsync(int months = 12)
    {
        var cutoff = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc)
            .AddMonths(-(months - 1));

        var grouped = await _db.Sales
            .Where(s => s.SaleDate >= cutoff)
            .GroupBy(s => new { s.SaleDate.Year, s.SaleDate.Month })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                TotalSales   = g.Count(),
                TotalRevenue = g.Sum(s => s.TotalAmount),
            })
            .OrderBy(g => g.Year).ThenBy(g => g.Month)
            .ToListAsync();

        var itemsByMonth = await _db.SaleItems
            .Where(si => si.Sale.SaleDate >= cutoff)
            .GroupBy(si => new { si.Sale.SaleDate.Year, si.Sale.SaleDate.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, TotalItems = g.Sum(x => x.Quantity) })
            .ToListAsync();

        return grouped.Select(g =>
        {
            var items = itemsByMonth.FirstOrDefault(i => i.Year == g.Year && i.Month == g.Month);
            return new MonthlySalesSummaryDto
            {
                Year           = g.Year,
                Month          = g.Month,
                MonthLabel     = new DateTime(g.Year, g.Month, 1).ToString("MMM yyyy"),
                TotalSales     = g.TotalSales,
                TotalRevenue   = g.TotalRevenue,
                TotalItemsSold = items?.TotalItems ?? 0,
            };
        }).ToList();
    }

    // ── Low Stock ─────────────────────────────────────────────────────────────

    public async Task<List<LowStockItemDto>> GetLowStockAsync() =>
        await _db.Medicines
            .Where(m => m.IsActive)
            .Select(m => new
            {
                m.MedicineId,
                m.Name,
                m.GenericName,
                m.ReorderLevel,
                CategoryName     = m.Category != null ? m.Category.CategoryName : null,
                ManufacturerName = m.Manufacturer != null ? m.Manufacturer.Name : null,
                TotalStock       = m.Batches.Sum(b => (int?)b.QuantityInBaseUnit) ?? 0
            })
            .Where(m => m.TotalStock <= m.ReorderLevel)
            .OrderBy(m => m.TotalStock)
            .Select(m => new LowStockItemDto
            {
                MedicineId          = m.MedicineId,
                MedicineName        = m.Name,
                GenericName         = m.GenericName,
                CategoryName        = m.CategoryName,
                ManufacturerName    = m.ManufacturerName,
                TotalStockInTablets = m.TotalStock,
                ReorderLevel        = m.ReorderLevel,
                ShortfallInTablets  = m.ReorderLevel - m.TotalStock
            })
            .ToListAsync();

    // ── Stock Consumption ─────────────────────────────────────────────────────

    public async Task<StockConsumptionReportDto> GetStockConsumptionAsync(
        DateTime? from, DateTime? to, int? supplierId, int? manufacturerId, string sortBy)
    {
        var toDate   = DateTime.SpecifyKind((to   ?? DateTime.UtcNow).Date.AddDays(1), DateTimeKind.Utc);
        var fromDate = DateTime.SpecifyKind((from ?? DateTime.MinValue).Date,           DateTimeKind.Utc);

        var query = _db.SaleItems
            .Where(si => si.Sale.SaleDate >= fromDate && si.Sale.SaleDate < toDate)
            .AsQueryable();

        if (manufacturerId.HasValue)
            query = query.Where(si => si.Medicine.ManufacturerId == manufacturerId.Value);

        if (supplierId.HasValue)
            query = query.Where(si =>
                _db.MedicineBatches.Any(b =>
                    b.MedicineId == si.MedicineId && b.SupplierId == supplierId.Value));

        var grouped = await query
            .GroupBy(si => new
            {
                si.MedicineId,
                si.Medicine.Name,
                GenericName      = si.Medicine.GenericName,
                CategoryName     = si.Medicine.Category != null ? si.Medicine.Category.CategoryName : null,
                ManufacturerName = si.Medicine.Manufacturer != null ? si.Medicine.Manufacturer.Name : null,
            })
            .Select(g => new StockConsumptionItemDto
            {
                MedicineId        = g.Key.MedicineId,
                MedicineName      = g.Key.Name,
                GenericName       = g.Key.GenericName,
                CategoryName      = g.Key.CategoryName,
                ManufacturerName  = g.Key.ManufacturerName,
                TotalQuantitySold = g.Sum(x => x.Quantity),
                TotalSalesCount   = g.Select(x => x.SaleId).Distinct().Count(),
                TotalRevenue      = g.Sum(x => x.LineTotal),
            })
            .ToListAsync();

        if (supplierId.HasValue)
        {
            var supplierName = await _db.Suppliers
                .Where(s => s.SupplierId == supplierId.Value)
                .Select(s => s.Name)
                .FirstOrDefaultAsync();
            grouped.ForEach(i => i.SupplierName = supplierName);
        }

        grouped = sortBy?.ToLower() switch
        {
            "revenue" => grouped.OrderByDescending(i => i.TotalRevenue).ToList(),
            _         => grouped.OrderByDescending(i => i.TotalQuantitySold).ToList(),
        };

        return new StockConsumptionReportDto
        {
            FromDate          = from,
            ToDate            = to,
            SupplierId        = supplierId,
            ManufacturerId    = manufacturerId,
            Items             = grouped,
            TotalQuantitySold = grouped.Sum(i => i.TotalQuantitySold),
            TotalRevenue      = grouped.Sum(i => i.TotalRevenue),
        };
    }

    // ── Top N ─────────────────────────────────────────────────────────────────

    public async Task<List<TopMedicineDto>> GetTopMedicinesAsync(DateTime from, DateTime to, int top = 10) =>
        await _db.SaleItems
            .Where(si => si.Sale.SaleDate >= from && si.Sale.SaleDate < to)
            .GroupBy(si => new { si.MedicineId, si.Medicine.Name })
            .Select(g => new TopMedicineDto
            {
                MedicineId        = g.Key.MedicineId,
                MedicineName      = g.Key.Name,
                TotalQuantitySold = g.Sum(x => x.Quantity),
                TotalRevenue      = g.Sum(x => x.LineTotal),
            })
            .OrderByDescending(x => x.TotalQuantitySold)
            .Take(top)
            .ToListAsync();

    public async Task<List<TopManufacturerDto>> GetTopManufacturersAsync(DateTime from, DateTime to, int top = 10) =>
        await _db.SaleItems
            .Where(si => si.Sale.SaleDate >= from && si.Sale.SaleDate < to
                         && si.Medicine.ManufacturerId != null)
            .GroupBy(si => new { si.Medicine.ManufacturerId, si.Medicine.Manufacturer!.Name })
            .Select(g => new TopManufacturerDto
            {
                ManufacturerId     = g.Key.ManufacturerId!.Value,
                ManufacturerName   = g.Key.Name,
                TotalMedicinesSold = g.Select(x => x.MedicineId).Distinct().Count(),
                TotalRevenue       = g.Sum(x => x.LineTotal),
            })
            .OrderByDescending(x => x.TotalRevenue)
            .Take(top)
            .ToListAsync();

    public async Task<List<TopSupplierDto>> GetTopSuppliersAsync(int top = 10) =>
        await _db.MedicineBatches
            .Where(b => b.SupplierId != null)
            .GroupBy(b => new { b.SupplierId, b.Supplier!.Name })
            .Select(g => new TopSupplierDto
            {
                SupplierId           = g.Key.SupplierId!.Value,
                SupplierName         = g.Key.Name,
                TotalBatchesSupplied = g.Count(),
                TotalTabletsSupplied = g.Sum(b => b.QuantityInBaseUnit),
            })
            .OrderByDescending(x => x.TotalTabletsSupplied)
            .Take(top)
            .ToListAsync();
}
