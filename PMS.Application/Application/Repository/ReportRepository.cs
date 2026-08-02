using Microsoft.EntityFrameworkCore;
using PMS.Application.Application.Interface.Repository;
using PMS.Common.DTOs.Report;
using PMS.Repository.Data;

namespace PMS.Application.Application.Repository;

public class ReportRepository : IReportRepository
{
    private readonly ApplicationDbContext _db;

    public ReportRepository(ApplicationDbContext db) => _db = db;

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
            TotalSales      = sales.Count,
            TotalRevenue    = sales.Sum(s => s.TotalAmount),
            TotalItemsSold  = itemCount,
        };
    }

    public async Task<List<TopMedicineDto>> GetTopMedicinesAsync(DateTime from, DateTime to, int top = 10) =>
        await _db.SaleItems
            .Where(si => si.Sale.SaleDate >= from && si.Sale.SaleDate < to)
            .GroupBy(si => new { si.MedicineId, si.Medicine.Name })
            .Select(g => new TopMedicineDto
            {
                MedicineId          = g.Key.MedicineId,
                MedicineName        = g.Key.Name,
                TotalQuantitySold   = g.Sum(x => x.Quantity),
                TotalRevenue        = g.Sum(x => x.LineTotal),
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
                ManufacturerId      = g.Key.ManufacturerId!.Value,
                ManufacturerName    = g.Key.Name,
                TotalMedicinesSold  = g.Select(x => x.MedicineId).Distinct().Count(),
                TotalRevenue        = g.Sum(x => x.LineTotal),
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
                SupplierId              = g.Key.SupplierId!.Value,
                SupplierName            = g.Key.Name,
                TotalBatchesSupplied    = g.Count(),
                TotalTabletsSupplied    = g.Sum(b => b.QuantityInTablets),
            })
            .OrderByDescending(x => x.TotalTabletsSupplied)
            .Take(top)
            .ToListAsync();
}
