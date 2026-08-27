using PMS.Application.Application.Interface.Repository;
using PMS.Application.Application.Interface.Service;
using PMS.Common.DTOs.Sale;
using PMS.Models.Models;

namespace PMS.Application.Application.Service;

/// <summary>
/// All business rules for selling medicine live here.
/// The repository handles data access and returns response DTOs directly.
/// </summary>
public class SaleService : ISaleService
{
    private readonly ISaleRepository _repo;

    public SaleService(ISaleRepository repo) => _repo = repo;

    /// <inheritdoc/>
    public async Task<(bool Success, string Message, SaleResponseDto? Data)> CreateSaleAsync(
        CreateSaleRequestDto dto)
    {
        // ── 1. Validate customer and pharmacist exist ─────────────────────────
        if (!await _repo.CustomerExistsAsync(dto.CustomerId))
            return (false, $"Customer with ID {dto.CustomerId} not found.", null);

        if (!await _repo.PharmacistExistsAsync(dto.PharmacistId))
            return (false, $"Pharmacist with ID {dto.PharmacistId} not found.", null);

        // ── 2. Validate each line item and plan FEFO deductions ───────────────
        var plannedItems        = new List<PlannedSaleItem>();
        var batchDeductions     = new List<(int BatchId, int BaseUnitsToDeduct)>();
        var medicineCache       = new Dictionary<int, Medicine>();
        var batchRemainingStock = new Dictionary<int, int>();

        foreach (var itemDto in dto.Items)
        {
            if (itemDto.ItemType == "Product")
            {
                // ── Product Sale Logic ─────────────────────────────────────────
                if (!itemDto.ProductId.HasValue || !itemDto.ProductUnitId.HasValue)
                    return (false, "ProductId and ProductUnitId are required for Product sales.", null);

                var product = await _repo.GetProductWithUnitsAsync(itemDto.ProductId.Value);
                if (product is null)
                    return (false, $"Product with ID {itemDto.ProductId} not found or inactive.", null);

                var productUnit = product.Units.FirstOrDefault(u => u.ProductUnitId == itemDto.ProductUnitId.Value);
                if (productUnit is null)
                    return (false,
                        $"ProductUnit {itemDto.ProductUnitId} is not attached to product '{product.Name}'.",
                        null);

                // Simple calculation for products (no batch/FEFO logic)
                var unitPrice      = productUnit.SalePrice / productUnit.TotalUnitQty;  // price per single item
                var grossAmount    = unitPrice * itemDto.Quantity;
                var discountAmount = Math.Round(grossAmount * itemDto.DiscountPercent / 100m, 4);
                var lineTotal      = Math.Round(grossAmount - discountAmount, 4);

                plannedItems.Add(new PlannedSaleItem(
                    ItemType             : "Product",
                    MedicineId           : null,
                    BatchId              : null,
                    MedicineUnitId       : null,
                    ProductId            : itemDto.ProductId.Value,
                    ProductUnitId        : itemDto.ProductUnitId.Value,
                    Quantity             : itemDto.Quantity,
                    BaseQuantityDeducted : 0,  // Products don't track batch deductions
                    UnitPrice            : unitPrice,
                    DiscountPercent      : itemDto.DiscountPercent,
                    DiscountAmount       : discountAmount,
                    LineTotal            : lineTotal
                ));
            }
            else  // Medicine sale (default)
            {
                // ── Medicine Sale Logic (existing FEFO logic) ──────────────────
                if (!itemDto.MedicineId.HasValue || !itemDto.MedicineUnitId.HasValue)
                    return (false, "MedicineId and MedicineUnitId are required for Medicine sales.", null);

                if (!medicineCache.TryGetValue(itemDto.MedicineId.Value, out var medicine))
                {
                    medicine = await _repo.GetMedicineWithUnitsAndFefoStockAsync(itemDto.MedicineId.Value);
                    if (medicine is null)
                        return (false, $"Medicine with ID {itemDto.MedicineId} not found or inactive.", null);

                    medicineCache[itemDto.MedicineId.Value] = medicine;
                    foreach (var b in medicine.Batches)
                    {
                        if (!batchRemainingStock.ContainsKey(b.BatchId))
                            batchRemainingStock[b.BatchId] = b.QuantityInBaseUnit;
                    }
                }

                var saleUnit = medicine.Units.FirstOrDefault(u => u.MedicineUnitId == itemDto.MedicineUnitId.Value);
                if (saleUnit is null)
                    return (false,
                        $"MedicineUnit {itemDto.MedicineUnitId} is not attached to medicine '{medicine.Name}'.",
                        null);

                // ── CanSell validation ──────────────────────────────────────────
                if (!saleUnit.CanSell)
                    return (false,
                        $"Unit '{saleUnit.UnitOfMeasure?.Name ?? saleUnit.MedicineUnitId.ToString()}' " +
                        $"is not marked as sellable (CanSell = false) for medicine '{medicine.Name}'. " +
                        "Use a unit that is enabled for sales.",
                        null);

                // ── Active check ────────────────────────────────────────────────
                if (!saleUnit.IsActive)
                    return (false,
                        $"Unit '{saleUnit.UnitOfMeasure?.Name ?? saleUnit.MedicineUnitId.ToString()}' " +
                        $"is inactive for medicine '{medicine.Name}' and cannot be used for sales.",
                        null);

                var baseUnitsRequested = medicine.ConvertToBaseQuantity(itemDto.MedicineUnitId.Value, itemDto.Quantity);
                var unitPrice          = saleUnit.UnitPrice;   // selling price, not cost price

                var totalAvailable = medicine.Batches.Sum(b => batchRemainingStock.TryGetValue(b.BatchId, out var s) ? s : 0);
                if (totalAvailable < baseUnitsRequested)
                    return (false,
                        $"Insufficient stock for '{medicine.Name}'. " +
                        $"Requested {baseUnitsRequested} base unit(s) but only {totalAvailable} available.",
                        null);

                // FEFO split
                var remaining = baseUnitsRequested;
                foreach (var batch in medicine.Batches)
                {
                    if (remaining <= 0) break;
                    var availableInBatch = batchRemainingStock.TryGetValue(batch.BatchId, out var bQty) ? bQty : 0;
                    if (availableInBatch <= 0) continue;

                    var takeFromBatch = Math.Min(remaining, availableInBatch);
                    batchRemainingStock[batch.BatchId] -= takeFromBatch;
                    remaining -= takeFromBatch;

                    var customerUnitsTaken = takeFromBatch / saleUnit.ConversionFactorToBase;
                    var grossAmount        = customerUnitsTaken * unitPrice;
                    var discountAmount     = Math.Round(grossAmount * itemDto.DiscountPercent / 100m, 4);
                    var lineTotal          = Math.Round(grossAmount - discountAmount, 4);

                    plannedItems.Add(new PlannedSaleItem(
                        ItemType             : "Medicine",
                        MedicineId           : itemDto.MedicineId.Value,
                        BatchId              : batch.BatchId,
                        MedicineUnitId       : itemDto.MedicineUnitId.Value,
                        ProductId            : null,
                        ProductUnitId        : null,
                        Quantity             : (int)Math.Ceiling(customerUnitsTaken),
                        BaseQuantityDeducted : takeFromBatch,
                        UnitPrice            : unitPrice,
                        DiscountPercent      : itemDto.DiscountPercent,
                        DiscountAmount       : discountAmount,
                        LineTotal            : lineTotal
                    ));

                    batchDeductions.Add((batch.BatchId, takeFromBatch));
                }
            }
        }

        // ── 3. Build the Sale aggregate ───────────────────────────────────────
        var invoiceNumber         = await _repo.GenerateInvoiceNumberAsync();
        var subtotal              = plannedItems.Sum(p => p.LineTotal);
        var invoiceDiscountAmount = Math.Round(subtotal * dto.DiscountPercent / 100m, 4);
        var totalAmount           = Math.Round(subtotal - invoiceDiscountAmount, 4);

        var sale = new Sale
        {
            InvoiceNumber   = invoiceNumber,
            CustomerId      = dto.CustomerId,
            PharmacistId    = dto.PharmacistId,
            SaleDate        = DateTime.UtcNow,
            PaymentMode     = dto.PaymentMode,
            Subtotal        = subtotal,
            DiscountPercent = dto.DiscountPercent,
            DiscountAmount  = invoiceDiscountAmount,
            TotalAmount     = totalAmount,
            CreatedAt       = DateTime.UtcNow,
            SaleItems       = plannedItems.Select(p => new SaleItem
            {
                ItemType             = p.ItemType,
                MedicineId           = p.MedicineId,
                BatchId              = p.BatchId,
                MedicineUnitId       = p.MedicineUnitId,
                ProductId            = p.ProductId,
                ProductUnitId        = p.ProductUnitId,
                Quantity             = p.Quantity,
                BaseQuantityDeducted = p.BaseQuantityDeducted,
                UnitPrice            = p.UnitPrice,
                DiscountPercent      = p.DiscountPercent,
                DiscountAmount       = p.DiscountAmount,
                LineTotal            = p.LineTotal
            }).ToList()
        };

        // ── 4. Persist inside a single transaction ────────────────────────────
        var saved = await _repo.SaveSaleAsync(sale, batchDeductions);

        // ── 5. Reload with projected DTO ──────────────────────────────────────
        var full = await _repo.GetSaleWithDetailsAsync(saved.SaleId)
            ?? throw new InvalidOperationException("Failed to reload sale after save.");

        return (true, "Sale created successfully.", full);
    }

    // ── Query methods ──────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<SaleResponseDto>> GetAllAsync() =>
        await _repo.GetAllSalesAsync();

    public async Task<SaleResponseDto?> GetByIdAsync(int id) =>
        await _repo.GetSaleWithDetailsAsync(id);

    // ── Private record for in-memory FEFO planning ─────────────────────────────

    private sealed record PlannedSaleItem(
        string  ItemType,
        int?    MedicineId,
        int?    BatchId,
        int?    MedicineUnitId,
        int?    ProductId,
        int?    ProductUnitId,
        int     Quantity,
        int     BaseQuantityDeducted,
        decimal UnitPrice,
        decimal DiscountPercent,
        decimal DiscountAmount,
        decimal LineTotal);
}
