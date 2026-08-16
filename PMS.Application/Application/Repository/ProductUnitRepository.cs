using Microsoft.EntityFrameworkCore;
using PMS.Application.Application.Interface.Repository;
using PMS.Models.Models;
using PMS.Repository.Data;

namespace PMS.Application.Application.Repository;

public class ProductUnitRepository : IProductUnitRepository
{
    private readonly ApplicationDbContext _db;

    public ProductUnitRepository(ApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<ProductUnit>> GetByProductIdAsync(int productId) =>
        await _db.ProductUnits
            .Where(pu => pu.ProductId == productId)
            .OrderByDescending(pu => pu.IsBaseUnit)
            .ThenBy(pu => pu.UnitOfMeasure.Name)
            .AsNoTracking()
            .Select(pu => new ProductUnit
            {
                ProductUnitId = pu.ProductUnitId,
                ProductId = pu.ProductId,
                UnitOfMeasureId = pu.UnitOfMeasureId,
                IsBaseUnit = pu.IsBaseUnit,
                ConversionFactorToBase = pu.ConversionFactorToBase,
                TotalUnitQty = pu.TotalUnitQty,
                PurchasePrice = pu.PurchasePrice,
                SalePrice = pu.SalePrice,
                UpdatedAt = pu.UpdatedAt,
                UnitOfMeasure = new UnitOfMeasure
                {
                    UnitOfMeasureId = pu.UnitOfMeasure.UnitOfMeasureId,
                    Name = pu.UnitOfMeasure.Name,
                    Symbol = pu.UnitOfMeasure.Symbol,
                    IsActive = pu.UnitOfMeasure.IsActive
                }
            })
            .ToListAsync();

    public async Task<ProductUnit?> GetByIdAsync(int productUnitId) =>
        await _db.ProductUnits
            .Where(pu => pu.ProductUnitId == productUnitId)
            .AsNoTracking()
            .Select(pu => new ProductUnit
            {
                ProductUnitId = pu.ProductUnitId,
                ProductId = pu.ProductId,
                UnitOfMeasureId = pu.UnitOfMeasureId,
                IsBaseUnit = pu.IsBaseUnit,
                ConversionFactorToBase = pu.ConversionFactorToBase,
                TotalUnitQty = pu.TotalUnitQty,
                PurchasePrice = pu.PurchasePrice,
                SalePrice = pu.SalePrice,
                UpdatedAt = pu.UpdatedAt,
                UnitOfMeasure = new UnitOfMeasure
                {
                    UnitOfMeasureId = pu.UnitOfMeasure.UnitOfMeasureId,
                    Name = pu.UnitOfMeasure.Name,
                    Symbol = pu.UnitOfMeasure.Symbol
                }
            })
            .FirstOrDefaultAsync();

    public async Task<bool> ExistsAsync(int productId, int unitOfMeasureId, int? excludeId = null) =>
        await _db.ProductUnits.AnyAsync(pu =>
            pu.ProductId == productId &&
            pu.UnitOfMeasureId == unitOfMeasureId &&
            (excludeId == null || pu.ProductUnitId != excludeId));

    public async Task<ProductUnit> CreateAsync(ProductUnit unit)
    {
        _db.ProductUnits.Add(unit);
        await _db.SaveChangesAsync();
        return unit;
    }

    public async Task<ProductUnit> UpdateAsync(ProductUnit unit)
    {
        _db.ProductUnits.Update(unit);
        await _db.SaveChangesAsync();
        return unit;
    }

    public async Task<Product?> GetProductWithUnitsAsync(int productId) =>
        await _db.Products
            .Where(p => p.ProductId == productId && p.IsActive)
            .Select(p => new Product
            {
                ProductId = p.ProductId,
                Name = p.Name,
                IsActive = p.IsActive,
                Units = p.Units
                    .Select(u => new ProductUnit
                    {
                        ProductUnitId = u.ProductUnitId,
                        ProductId = u.ProductId,
                        UnitOfMeasureId = u.UnitOfMeasureId,
                        IsBaseUnit = u.IsBaseUnit,
                        ConversionFactorToBase = u.ConversionFactorToBase,
                        TotalUnitQty = u.TotalUnitQty,
                        PurchasePrice = u.PurchasePrice,
                        SalePrice = u.SalePrice,
                        UpdatedAt = u.UpdatedAt,
                        UnitOfMeasure = new UnitOfMeasure
                        {
                            UnitOfMeasureId = u.UnitOfMeasure.UnitOfMeasureId,
                            Name = u.UnitOfMeasure.Name,
                            Symbol = u.UnitOfMeasure.Symbol
                        }
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync();
}
