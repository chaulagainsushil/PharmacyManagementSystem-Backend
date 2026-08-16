using Moq;
using PMS.Application.Application.Interface.Repository;
using PMS.Application.Application.Service;
using PMS.Common.DTOs.MedicineBatch;
using PMS.Common.DTOs.Sale;
using PMS.Models.Enums;
using PMS.Models.Models;
using Xunit;

namespace PMS.Tests;

/// <summary>
/// Tests for the medicine unit system:
///   - CanSell validation in SaleService
///   - CanPurchase enforcement in MedicineBatchService
///   - Price independence (selling price ≠ cost price ≠ MRP)
///   - FEFO stock deduction uses QuantityInBaseUnit
/// </summary>
public class MedicineUnitSystemTests
{
    private static readonly Guid Tenant = Guid.NewGuid();

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static UnitOfMeasure MakeUom(int id, string name) => new()
    {
        UnitOfMeasureId = id,
        Name            = name,
        Symbol          = name.ToLower()[..Math.Min(3, name.Length)],
        TenantId        = Tenant
    };

    private static MedicineUnit MakeUnit(
        int id, int medicineId, int uomId,
        decimal factor = 1m,
        bool isBase = false,
        bool isDefault = false,
        bool canSell = true,
        bool canPurchase = false,
        decimal unitPrice = 0m,
        decimal costPrice = 0m,
        decimal mrp = 0m) => new()
    {
        MedicineUnitId         = id,
        TenantId               = Tenant,
        MedicineId             = medicineId,
        UnitOfMeasureId        = uomId,
        ConversionFactorToBase = factor,
        IsBaseUnit             = isBase,
        IsDefault              = isDefault,
        IsActive               = true,
        CanSell                = canSell,
        CanPurchase            = canPurchase,
        UnitPrice              = unitPrice,
        CostPrice              = costPrice,
        Mrp                    = mrp,
        UnitOfMeasure          = MakeUom(uomId, $"UOM-{uomId}")
    };

    private static MedicineBatch MakeBatch(int id, int medicineId, int qty, decimal cost = 1m) => new()
    {
        BatchId              = id,
        TenantId             = Tenant,
        MedicineId           = medicineId,
        BatchNumber          = $"BATCH-{id:D3}",
        ExpiryDate           = DateTime.UtcNow.AddMonths(6),
        QuantityInBaseUnit   = qty,
        CostPricePerBaseUnit = cost
    };

    private static Medicine MakeMedicine(int id, IEnumerable<MedicineUnit> units, IEnumerable<MedicineBatch> batches) =>
        new()
        {
            MedicineId = id,
            TenantId   = Tenant,
            Name       = $"Medicine-{id}",
            IsActive   = true,
            Units      = units.ToList(),
            Batches    = batches.ToList()
        };

    // ── SaleService — CanSell validation ─────────────────────────────────────

    [Fact]
    public async Task SaleService_CanSell_False_RejectsSale()
    {
        // Arrange — Strip unit is NOT sellable
        var tabletUnit = MakeUnit(1, 10, 1, factor: 1m, isBase: true, isDefault: false, canSell: true, unitPrice: 2m);
        var stripUnit  = MakeUnit(2, 10, 2, factor: 10m, isBase: false, isDefault: true, canSell: false, unitPrice: 18m);
        var batch      = MakeBatch(1, 10, qty: 100);
        var medicine   = MakeMedicine(10, new[] { tabletUnit, stripUnit }, new[] { batch });

        var repoMock = new Mock<ISaleRepository>();
        repoMock.Setup(r => r.CustomerExistsAsync(1)).ReturnsAsync(true);
        repoMock.Setup(r => r.PharmacistExistsAsync(1)).ReturnsAsync(true);
        repoMock.Setup(r => r.GetMedicineWithUnitsAndFefoStockAsync(10)).ReturnsAsync(medicine);

        var service = new SaleService(repoMock.Object);
        var dto = new CreateSaleRequestDto
        {
            CustomerId    = 1,
            PharmacistId  = 1,
            DiscountPercent = 0,
            PaymentMode   = PaymentMode.Cash,
            Items = new List<CreateSaleItemDto>
            {
                new() { MedicineId = 10, MedicineUnitId = 2, Quantity = 2 }  // Strip, CanSell=false
            }
        };

        // Act
        var (success, message, _) = await service.CreateSaleAsync(dto);

        // Assert
        Assert.False(success, "Sale should fail when CanSell=false on the chosen unit.");
        Assert.Contains("CanSell", message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SaleService_CanSell_True_AllowsSale()
    {
        // Arrange — Tablet unit IS sellable, CanSell=true
        var tabletUnit = MakeUnit(1, 10, 1, factor: 1m, isBase: true, isDefault: true, canSell: true, unitPrice: 2m);
        var batch      = MakeBatch(1, 10, qty: 50);
        var medicine   = MakeMedicine(10, new[] { tabletUnit }, new[] { batch });

        var savedSale = new Sale { SaleId = 100 };
        var repoMock  = new Mock<ISaleRepository>();
        repoMock.Setup(r => r.CustomerExistsAsync(1)).ReturnsAsync(true);
        repoMock.Setup(r => r.PharmacistExistsAsync(1)).ReturnsAsync(true);
        repoMock.Setup(r => r.GetMedicineWithUnitsAndFefoStockAsync(10)).ReturnsAsync(medicine);
        repoMock.Setup(r => r.SaveSaleAsync(It.IsAny<Sale>(), It.IsAny<IReadOnlyList<(int, int)>>()))
                .ReturnsAsync(savedSale);
        repoMock.Setup(r => r.GenerateInvoiceNumberAsync()).ReturnsAsync("INV-20260812-0001");
        repoMock.Setup(r => r.GetSaleWithDetailsAsync(100))
                .ReturnsAsync(new SaleResponseDto { SaleId = 100 });

        var service = new SaleService(repoMock.Object);
        var dto = new CreateSaleRequestDto
        {
            CustomerId    = 1,
            PharmacistId  = 1,
            DiscountPercent = 0,
            PaymentMode   = PaymentMode.Cash,
            Items = new List<CreateSaleItemDto>
            {
                new() { MedicineId = 10, MedicineUnitId = 1, Quantity = 5 }  // Tablet, CanSell=true
            }
        };

        // Act
        var (success, message, data) = await service.CreateSaleAsync(dto);

        // Assert
        Assert.True(success, $"Sale should succeed when CanSell=true. Message: {message}");
        Assert.NotNull(data);
    }

    [Fact]
    public async Task SaleService_InactiveUnit_RejectsSale()
    {
        // Arrange — unit is active in DB projection but CanSell=true/inactive check
        var tabletUnit = MakeUnit(1, 10, 1, factor: 1m, isBase: true, isDefault: true, canSell: true, unitPrice: 2m);
        // Simulate the unit not being in the active collection (inactive excluded by query)
        var batch    = MakeBatch(1, 10, qty: 50);
        var medicine = MakeMedicine(10, new[] { tabletUnit }, new[] { batch });

        var repoMock = new Mock<ISaleRepository>();
        repoMock.Setup(r => r.CustomerExistsAsync(1)).ReturnsAsync(true);
        repoMock.Setup(r => r.PharmacistExistsAsync(1)).ReturnsAsync(true);
        repoMock.Setup(r => r.GetMedicineWithUnitsAndFefoStockAsync(10)).ReturnsAsync(medicine);

        var service = new SaleService(repoMock.Object);
        var dto = new CreateSaleRequestDto
        {
            CustomerId    = 1,
            PharmacistId  = 1,
            DiscountPercent = 0,
            PaymentMode   = PaymentMode.Cash,
            Items = new List<CreateSaleItemDto>
            {
                new() { MedicineId = 10, MedicineUnitId = 99, Quantity = 1 }  // Non-existent unit
            }
        };

        // Act
        var (success, message, _) = await service.CreateSaleAsync(dto);

        // Assert
        Assert.False(success, "Sale should fail when MedicineUnitId does not belong to the medicine.");
    }

    // ── SaleService — FEFO deduction uses QuantityInBaseUnit ─────────────────

    [Fact]
    public async Task SaleService_StripSale_DeductsCorrectBaseUnits()
    {
        // 1 Strip = 10 Tablets; selling 2 Strips should deduct 20 Tablets from stock
        var tabletUnit = MakeUnit(1, 10, 1, factor: 1m, isBase: true, isDefault: false, canSell: true, unitPrice: 2m);
        var stripUnit  = MakeUnit(2, 10, 2, factor: 10m, isBase: false, isDefault: true, canSell: true, unitPrice: 18m);
        var batch      = MakeBatch(1, 10, qty: 100);  // 100 tablets in stock
        var medicine   = MakeMedicine(10, new[] { tabletUnit, stripUnit }, new[] { batch });

        IReadOnlyList<(int, int)>? capturedDeductions = null;
        var savedSale = new Sale { SaleId = 101 };
        var repoMock  = new Mock<ISaleRepository>();
        repoMock.Setup(r => r.CustomerExistsAsync(1)).ReturnsAsync(true);
        repoMock.Setup(r => r.PharmacistExistsAsync(1)).ReturnsAsync(true);
        repoMock.Setup(r => r.GetMedicineWithUnitsAndFefoStockAsync(10)).ReturnsAsync(medicine);
        repoMock.Setup(r => r.GenerateInvoiceNumberAsync()).ReturnsAsync("INV-20260812-0002");
        repoMock.Setup(r => r.SaveSaleAsync(It.IsAny<Sale>(), It.IsAny<IReadOnlyList<(int, int)>>()))
                .Callback<Sale, IReadOnlyList<(int, int)>>((_, deductions) => capturedDeductions = deductions)
                .ReturnsAsync(savedSale);
        repoMock.Setup(r => r.GetSaleWithDetailsAsync(101))
                .ReturnsAsync(new SaleResponseDto { SaleId = 101 });

        var service = new SaleService(repoMock.Object);
        var dto = new CreateSaleRequestDto
        {
            CustomerId      = 1,
            PharmacistId    = 1,
            DiscountPercent = 0,
            PaymentMode     = PaymentMode.Cash,
            Items = new List<CreateSaleItemDto>
            {
                new() { MedicineId = 10, MedicineUnitId = 2, Quantity = 2 }  // 2 Strips
            }
        };

        var (success, _, _) = await service.CreateSaleAsync(dto);

        Assert.True(success, "Sale of 2 Strips should succeed.");
        Assert.NotNull(capturedDeductions);
        var totalDeducted = capturedDeductions!.Sum(d => d.Item2);
        Assert.Equal(20, totalDeducted);  // 2 Strips × 10 Tablets = 20 base units
    }

    [Fact]
    public async Task SaleService_PriceUsed_IsUnitPrice_NotCostPrice()
    {
        // Confirm that the sale uses UnitPrice (selling price), not CostPrice
        var tabletUnit = MakeUnit(1, 10, 1, factor: 1m, isBase: true, isDefault: true,
            canSell: true, unitPrice: 2.50m, costPrice: 1.20m, mrp: 3.00m);
        var batch    = MakeBatch(1, 10, qty: 100);
        var medicine = MakeMedicine(10, new[] { tabletUnit }, new[] { batch });

        Sale? capturedSale = null;
        var repoMock = new Mock<ISaleRepository>();
        repoMock.Setup(r => r.CustomerExistsAsync(1)).ReturnsAsync(true);
        repoMock.Setup(r => r.PharmacistExistsAsync(1)).ReturnsAsync(true);
        repoMock.Setup(r => r.GetMedicineWithUnitsAndFefoStockAsync(10)).ReturnsAsync(medicine);
        repoMock.Setup(r => r.GenerateInvoiceNumberAsync()).ReturnsAsync("INV-20260812-0003");
        repoMock.Setup(r => r.SaveSaleAsync(It.IsAny<Sale>(), It.IsAny<IReadOnlyList<(int, int)>>()))
                .Callback<Sale, IReadOnlyList<(int, int)>>((sale, _) => capturedSale = sale)
                .ReturnsAsync(new Sale { SaleId = 102 });
        repoMock.Setup(r => r.GetSaleWithDetailsAsync(102))
                .ReturnsAsync(new SaleResponseDto { SaleId = 102 });

        var service = new SaleService(repoMock.Object);
        var dto = new CreateSaleRequestDto
        {
            CustomerId      = 1,
            PharmacistId    = 1,
            DiscountPercent = 0,
            PaymentMode     = PaymentMode.Cash,
            Items = new List<CreateSaleItemDto>
            {
                new() { MedicineId = 10, MedicineUnitId = 1, Quantity = 4 }  // 4 Tablets
            }
        };

        var (success, _, _) = await service.CreateSaleAsync(dto);

        Assert.True(success);
        Assert.NotNull(capturedSale);
        // TotalAmount = 4 × UnitPrice(2.50) = 10.00 (not cost price 4.80 or MRP 12.00)
        Assert.Equal(10.00m, capturedSale!.TotalAmount);
        // UnitPrice on the SaleItem should be the selling price, not cost
        var item = capturedSale.SaleItems.First();
        Assert.Equal(2.50m, item.UnitPrice);
        Assert.NotEqual(1.20m, item.UnitPrice);  // must NOT be cost price
    }

    // ── MedicineBatchService — CanPurchase validation ─────────────────────────

    [Fact]
    public async Task BatchService_CanPurchase_False_RejectsBatch()
    {
        // Strip unit has CanPurchase=false
        var stripUnit = MakeUnit(2, 10, 2, factor: 10m, canPurchase: false);

        var repoMock = new Mock<IMedicineBatchRepository>();
        repoMock.Setup(r => r.ExistsByBatchNumberAsync("BATCH-001", null)).ReturnsAsync(false);
        repoMock.Setup(r => r.GetPurchaseUnitAsync(10, 2)).ReturnsAsync(stripUnit);

        var service = new MedicineBatchService(repoMock.Object);
        var dto = new CreateMedicineBatchDto
        {
            MedicineId             = 10,
            BatchNumber            = "BATCH-001",
            ExpiryDate             = DateTime.UtcNow.AddMonths(6),
            PurchaseMedicineUnitId = 2,
            PurchaseQuantity       = 50,
            CostPricePerPurchaseUnit = 18m
        };

        var (success, message, _) = await service.CreateAsync(dto);

        Assert.False(success, "Batch creation should fail when CanPurchase=false on the unit.");
        Assert.Contains("CanPurchase", message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task BatchService_CanPurchase_True_ConvertsToBaesUnits()
    {
        // Strip unit: CanPurchase=true, 1 Strip = 10 Tablets, cost Rs 18/strip → Rs 1.80/tablet
        var stripUnit = MakeUnit(2, 10, 2, factor: 10m, canPurchase: true, costPrice: 18m);
        stripUnit.UnitOfMeasure = MakeUom(2, "Strip");

        MedicineBatch? capturedBatch = null;
        var repoMock = new Mock<IMedicineBatchRepository>();
        repoMock.Setup(r => r.ExistsByBatchNumberAsync("BATCH-002", null)).ReturnsAsync(false);
        repoMock.Setup(r => r.GetPurchaseUnitAsync(10, 2)).ReturnsAsync(stripUnit);
        repoMock.Setup(r => r.CreateAsync(It.IsAny<MedicineBatch>()))
                .Callback<MedicineBatch>(b => capturedBatch = b)
                .ReturnsAsync((MedicineBatch b) => b);
        repoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
                .ReturnsAsync(new MedicineBatchResponseDto { BatchId = 1, QuantityInBaseUnit = 500, CostPricePerBaseUnit = 1.80m });

        var service = new MedicineBatchService(repoMock.Object);
        var dto = new CreateMedicineBatchDto
        {
            MedicineId               = 10,
            BatchNumber              = "BATCH-002",
            ExpiryDate               = DateTime.UtcNow.AddMonths(6),
            PurchaseMedicineUnitId   = 2,      // Strip
            PurchaseQuantity         = 50,     // 50 Strips
            CostPricePerPurchaseUnit = 18m     // Rs 18 per strip
        };

        var (success, message, _) = await service.CreateAsync(dto);

        Assert.True(success, $"Batch creation should succeed when CanPurchase=true. Message: {message}");
        Assert.NotNull(capturedBatch);
        // 50 Strips × 10 Tablets/Strip = 500 Tablets
        Assert.Equal(500, capturedBatch!.QuantityInBaseUnit);
        // Rs 18/strip ÷ 10 tablets/strip = Rs 1.80/tablet
        Assert.Equal(1.80m, capturedBatch.CostPricePerBaseUnit);
        // Purchase unit metadata preserved
        Assert.Equal(2, capturedBatch.PurchaseMedicineUnitId);
        Assert.Equal(50, capturedBatch.PurchaseQuantityInPurchaseUnit);
    }

    [Fact]
    public async Task BatchService_BaseUnitMode_StoresDirectly()
    {
        // No purchase unit ID — quantity and cost are already in base units
        var repoMock = new Mock<IMedicineBatchRepository>();
        repoMock.Setup(r => r.ExistsByBatchNumberAsync("BATCH-003", null)).ReturnsAsync(false);

        MedicineBatch? capturedBatch = null;
        repoMock.Setup(r => r.CreateAsync(It.IsAny<MedicineBatch>()))
                .Callback<MedicineBatch>(b => capturedBatch = b)
                .ReturnsAsync((MedicineBatch b) => b);
        repoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
                .ReturnsAsync(new MedicineBatchResponseDto { BatchId = 2, QuantityInBaseUnit = 200, CostPricePerBaseUnit = 1.50m });

        var service = new MedicineBatchService(repoMock.Object);
        var dto = new CreateMedicineBatchDto
        {
            MedicineId           = 10,
            BatchNumber          = "BATCH-003",
            ExpiryDate           = DateTime.UtcNow.AddMonths(6),
            PurchaseMedicineUnitId = null,   // base-unit mode
            PurchaseQuantity       = 200,
            CostPricePerBaseUnit   = 1.50m
        };

        var (success, _, _) = await service.CreateAsync(dto);

        Assert.True(success);
        Assert.NotNull(capturedBatch);
        Assert.Equal(200, capturedBatch!.QuantityInBaseUnit);
        Assert.Equal(1.50m, capturedBatch.CostPricePerBaseUnit);
        Assert.Null(capturedBatch.PurchaseMedicineUnitId);
        Assert.Null(capturedBatch.PurchaseQuantityInPurchaseUnit);
    }

    // ── Price independence ────────────────────────────────────────────────────

    [Fact]
    public void MedicineUnit_PricesAreIndependent()
    {
        // Each price field is completely independent — no derived calculations
        var tabletUnit = new MedicineUnit
        {
            MedicineUnitId         = 1,
            ConversionFactorToBase = 1m,
            IsBaseUnit             = true,
            CanSell                = true,
            CostPrice              = 1.20m,    // purchase cost per tablet
            UnitPrice              = 2.00m,    // retail selling price per tablet
            Mrp                    = 2.50m     // MRP printed on pack per tablet
        };

        var stripUnit = new MedicineUnit
        {
            MedicineUnitId         = 2,
            ConversionFactorToBase = 10m,      // 1 strip = 10 tablets
            IsBaseUnit             = false,
            CanSell                = true,
            CostPrice              = 10.00m,   // cost per strip (NOT 1.20 × 10 = 12.00)
            UnitPrice              = 18.00m,   // selling price per strip (NOT 2.00 × 10 = 20.00)
            Mrp                    = 22.00m    // MRP per strip (NOT 2.50 × 10 = 25.00)
        };

        // Prices are independent — strip selling price is NOT tablet_price × conversion
        Assert.NotEqual(tabletUnit.UnitPrice * stripUnit.ConversionFactorToBase, stripUnit.UnitPrice);
        Assert.NotEqual(tabletUnit.CostPrice * stripUnit.ConversionFactorToBase, stripUnit.CostPrice);
        Assert.NotEqual(tabletUnit.Mrp * stripUnit.ConversionFactorToBase, stripUnit.Mrp);

        // Each price can be read independently
        Assert.Equal(2.00m, tabletUnit.UnitPrice);
        Assert.Equal(18.00m, stripUnit.UnitPrice);
        Assert.Equal(10.00m, stripUnit.CostPrice);
        Assert.Equal(22.00m, stripUnit.Mrp);
    }

    [Fact]
    public void Medicine_ConvertToBaseQuantity_UsesConversionFactor_NotPrice()
    {
        // ConvertToBaseQuantity uses ConversionFactorToBase — not UnitPrice
        var tabletUnit = new MedicineUnit
        {
            MedicineUnitId         = 1,
            ConversionFactorToBase = 1m,
            UnitPrice              = 2.00m,
            IsBaseUnit             = true
        };
        var stripUnit = new MedicineUnit
        {
            MedicineUnitId         = 2,
            ConversionFactorToBase = 10m,
            UnitPrice              = 18.00m,   // independent price
            IsBaseUnit             = false
        };

        var medicine = new Medicine
        {
            MedicineId = 1,
            Units = new List<MedicineUnit> { tabletUnit, stripUnit }
        };

        // 3 strips × 10 tablets/strip = 30 tablets
        var baseQty = medicine.ConvertToBaseQuantity(2, 3);
        Assert.Equal(30, baseQty);

        // 5 tablets × 1 = 5 tablets (base unit)
        var baseQtyTablet = medicine.ConvertToBaseQuantity(1, 5);
        Assert.Equal(5, baseQtyTablet);
    }
}
