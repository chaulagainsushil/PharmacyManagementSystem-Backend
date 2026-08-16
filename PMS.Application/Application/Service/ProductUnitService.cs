using PMS.Application.Application.Interface.Repository;
using PMS.Application.Application.Interface.Service;
using PMS.Common.DTOs.Product;
using PMS.Models.Models;

namespace PMS.Application.Application.Service;

public class ProductUnitService : IProductUnitService
{
    private readonly IProductUnitRepository _repo;
    private readonly IUnitOfMeasureRepository _uomRepo;

    public ProductUnitService(IProductUnitRepository repo, IUnitOfMeasureRepository uomRepo)
    {
        _repo = repo;
        _uomRepo = uomRepo;
    }

    public async Task<(bool Success, string Message, IReadOnlyList<ProductUnitDto>? Data)> AddDualUnitAsync(
        int productId, AddDualUnitCommand cmd)
    {
        // Validate first unit UoM exists
        var firstUom = await _uomRepo.GetByIdAsync(cmd.FirstUnit.UnitOfMeasureId);
        if (firstUom is null)
            return (false, $"Unit of measure with ID {cmd.FirstUnit.UnitOfMeasureId} not found.", null);

        // Validate second unit UoM if provided
        UnitOfMeasure? secondUom = null;
        if (cmd.SecondUnit is not null)
        {
            secondUom = await _uomRepo.GetByIdAsync(cmd.SecondUnit.UnitOfMeasureId);
            if (secondUom is null)
                return (false, $"Unit of measure with ID {cmd.SecondUnit.UnitOfMeasureId} not found.", null);

            // First and second cannot be the same
            if (cmd.FirstUnit.UnitOfMeasureId == cmd.SecondUnit.UnitOfMeasureId)
                return (false, "First unit and second unit cannot be the same UoM.", null);
        }

        // Check for duplicates
        if (await _repo.ExistsAsync(productId, cmd.FirstUnit.UnitOfMeasureId))
            return (false, $"This product already has '{firstUom.Name}' attached as a unit.", null);

        if (cmd.SecondUnit is not null && await _repo.ExistsAsync(productId, cmd.SecondUnit.UnitOfMeasureId))
            return (false, $"This product already has '{secondUom?.Name ?? "this unit"}' attached as a unit.", null);

        // Load product to manage base unit flag
        var product = await _repo.GetProductWithUnitsAsync(productId);
        if (product is null)
            return (false, $"Product with ID {productId} not found.", null);

        // Clear any existing base unit flag (only one allowed)
        foreach (var existingUnit in product.Units.Where(u => u.IsBaseUnit))
        {
            existingUnit.IsBaseUnit = false;
            await _repo.UpdateAsync(existingUnit);
        }

        // Create first unit (base)
        var firstUnit = new ProductUnit
        {
            ProductId = productId,
            UnitOfMeasureId = cmd.FirstUnit.UnitOfMeasureId,
            IsBaseUnit = true,
            TotalUnitQty = cmd.FirstUnit.TotalUnitQty,
            ConversionFactorToBase = cmd.FirstUnit.ConversionFactorToBase,
            PurchasePrice = cmd.FirstUnit.PurchasePrice,
            SalePrice = cmd.FirstUnit.SalePrice
        };

        await _repo.CreateAsync(firstUnit);

        // Create second unit if provided
        if (cmd.SecondUnit is not null)
        {
            // Auto-calculate TotalUnitQty for second unit
            decimal secondUnitTotalQty = cmd.FirstUnit.TotalUnitQty * cmd.FirstUnit.ConversionFactorToBase;

            // Auto-calculate pricing if not provided by user
            decimal secondUnitPurchasePrice = cmd.SecondUnit.PurchasePrice ?? 
                (secondUnitTotalQty > 0 ? cmd.FirstUnit.PurchasePrice / secondUnitTotalQty : 0);
            decimal secondUnitSalePrice = cmd.SecondUnit.SalePrice ?? 
                (secondUnitTotalQty > 0 ? cmd.FirstUnit.SalePrice / secondUnitTotalQty : 0);

            var secondUnit = new ProductUnit
            {
                ProductId = productId,
                UnitOfMeasureId = cmd.SecondUnit.UnitOfMeasureId,
                IsBaseUnit = false,
                // ConversionFactorToBase for sub unit is 1 (it's already expressed in its own terms)
                ConversionFactorToBase = 1m,
                TotalUnitQty = secondUnitTotalQty,
                PurchasePrice = secondUnitPurchasePrice,
                SalePrice = secondUnitSalePrice
            };

            await _repo.CreateAsync(secondUnit);
        }

        // Return all units for this product
        var allUnits = await _repo.GetByProductIdAsync(productId);
        var dtos = allUnits.Select(MapToDto).ToList();

        return (true, "Units attached successfully.", dtos);
    }

    public async Task<(bool Found, ProductUnitsResponse? Data)> GetUnitsAsync(int productId)
    {
        var product = await _repo.GetProductWithUnitsAsync(productId);
        if (product is null)
            return (false, null);

        var unitDtos = product.Units.Select(MapToDto).ToList();

        var response = new ProductUnitsResponse
        {
            ProductId = product.ProductId,
            ProductName = product.Name,
            Units = unitDtos
        };

        return (true, response);
    }

    // ── Mapping ────────────────────────────────────────────────────────────

    private static ProductUnitDto MapToDto(ProductUnit pu) => new()
    {
        ProductUnitId = pu.ProductUnitId,
        ProductId = pu.ProductId,
        UnitOfMeasureId = pu.UnitOfMeasureId,
        UomName = pu.UnitOfMeasure?.Name ?? string.Empty,
        UomSymbol = pu.UnitOfMeasure?.Symbol ?? string.Empty,
        IsBaseUnit = pu.IsBaseUnit,
        ConversionFactorToBase = pu.ConversionFactorToBase,
        TotalUnitQty = pu.TotalUnitQty,
        PurchasePrice = pu.PurchasePrice,
        SalePrice = pu.SalePrice,
        UpdatedAt = pu.UpdatedAt
    };
}
