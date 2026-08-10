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
        var plannedItems    = new List<PlannedSaleItem>();
        var batchDeductions = new List<(int BatchId, int BaseUnitsToDeduct)>();

        foreach (var itemDto in dto.Items)
        {
            var medicine = await _repo.GetMedicineWithUnitsAndFefoStockAsync(itemDto.MedicineId);
            if (medicine is null)
                return (false, $"Medicine with ID {itemDto.MedicineId} not found or inactive.", null);

            var saleUnit = medicine.Units.FirstOrDefault(u => u.MedicineUnitId == itemDto.MedicineUnitId);
            if (saleUnit is null)
                return (false,
                    $"MedicineUnit {itemDto.MedicineUnitId} is not attached to medicine '{medicine.Name}'.",
                    null);

            var baseUnitsRequested = medicine.ConvertToBaseQuantity(itemDto.MedicineUnitId, itemDto.Quantity);
            var unitPrice          = saleUnit.UnitPrice;

            var totalAvailable = medicine.Batches.Sum(b => b.QuantityInTablets);
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

                var takeFromBatch    = Math.Min(remaining, batch.QuantityInTablets);
                remaining -= takeFromBatch;

                var customerUnitsTaken = takeFromBatch / saleUnit.ConversionFactorToBase;
                var grossAmount        = customerUnitsTaken * unitPrice;
                var discountAmount     = Math.Round(grossAmount * itemDto.DiscountPercent / 100m, 4);
                var lineTotal          = Math.Round(grossAmount - discountAmount, 4);

                plannedItems.Add(new PlannedSaleItem(
                    MedicineId          : itemDto.MedicineId,
                    BatchId             : batch.BatchId,
                    MedicineUnitId      : itemDto.MedicineUnitId,
                    Quantity            : (int)Math.Ceiling(customerUnitsTaken),
                    BaseQuantityDeducted: takeFromBatch,
                    UnitPrice           : unitPrice,
                    DiscountPercent     : itemDto.DiscountPercent,
                    DiscountAmount      : discountAmount,
                    LineTotal           : lineTotal
                ));

                batchDeductions.Add((batch.BatchId, takeFromBatch));
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
                MedicineId           = p.MedicineId,
                BatchId              = p.BatchId,
                MedicineUnitId       = p.MedicineUnitId,
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
        int     MedicineId,
        int     BatchId,
        int     MedicineUnitId,
        int     Quantity,
        int     BaseQuantityDeducted,
        decimal UnitPrice,
        decimal DiscountPercent,
        decimal DiscountAmount,
        decimal LineTotal);
}
