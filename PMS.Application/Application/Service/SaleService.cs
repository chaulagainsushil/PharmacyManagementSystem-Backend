using PMS.Application.Application.Interface.Repository;
using PMS.Application.Application.Interface.Service;
using PMS.Common.DTOs.Sale;
using PMS.Models.Models;

namespace PMS.Application.Application.Service;

/// <summary>
/// All business rules for selling medicine live here.
/// The repository is used only for data access — no EF code appears in this class.
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
        //
        // plannedItems     — one entry per (MedicineId, BatchId) pairing produced
        //                    by FEFO splitting.
        // batchDeductions  — flat list of (BatchId, baseUnits) for the repo.

        var plannedItems    = new List<PlannedSaleItem>();
        var batchDeductions = new List<(int BatchId, int BaseUnitsToDeduct)>();

        foreach (var itemDto in dto.Items)
        {
            // Load medicine with its active MedicineUnits and FEFO batches
            var medicine = await _repo.GetMedicineWithUnitsAndFefoStockAsync(itemDto.MedicineId);
            if (medicine is null)
                return (false, $"Medicine with ID {itemDto.MedicineId} not found or inactive.", null);

            // Validate the requested MedicineUnit belongs to this medicine
            var saleUnit = medicine.Units.FirstOrDefault(u => u.MedicineUnitId == itemDto.MedicineUnitId);
            if (saleUnit is null)
                return (false,
                    $"MedicineUnit {itemDto.MedicineUnitId} is not attached to medicine '{medicine.Name}'.",
                    null);

            // Convert requested quantity to base units (e.g. 2 strips × 10 tablets/strip = 20 tablets)
            var baseUnitsRequested = medicine.ConvertToBaseQuantity(itemDto.MedicineUnitId, itemDto.Quantity);

            // Price snapshot from MedicineUnit at sale time
            var unitPrice = saleUnit.UnitPrice;

            // Check total available stock across non-expired batches
            var totalAvailable = medicine.Batches.Sum(b => b.QuantityInTablets);
            if (totalAvailable < baseUnitsRequested)
                return (false,
                    $"Insufficient stock for '{medicine.Name}'. " +
                    $"Requested {baseUnitsRequested} base unit(s) but only {totalAvailable} available.",
                    null);

            // ── FEFO split: drain batches from nearest expiry first ────────────
            // This logic is unchanged — batches are already sorted ExpiryDate ASC.
            var remaining = baseUnitsRequested;
            foreach (var batch in medicine.Batches)
            {
                if (remaining <= 0) break;

                var takeFromBatch = Math.Min(remaining, batch.QuantityInTablets);
                remaining -= takeFromBatch;

                // Express the deducted base-unit quantity back in customer units for pricing.
                // division is safe because ConversionFactorToBase is always ≥ 1.
                var customerUnitsTaken = takeFromBatch / saleUnit.ConversionFactorToBase;

                var grossAmount    = customerUnitsTaken * unitPrice;
                var discountAmount = Math.Round(grossAmount * itemDto.DiscountPercent / 100m, 4);
                var lineTotal      = Math.Round(grossAmount - discountAmount, 4);

                plannedItems.Add(new PlannedSaleItem(
                    MedicineId         : itemDto.MedicineId,
                    BatchId            : batch.BatchId,
                    MedicineUnitId     : itemDto.MedicineUnitId,
                    Quantity           : (int)Math.Ceiling(customerUnitsTaken),
                    BaseQuantityDeducted: takeFromBatch,
                    UnitPrice          : unitPrice,
                    DiscountPercent    : itemDto.DiscountPercent,
                    DiscountAmount     : discountAmount,
                    LineTotal          : lineTotal
                ));

                batchDeductions.Add((batch.BatchId, takeFromBatch));
            }
        }

        // ── 3. Build the Sale aggregate ───────────────────────────────────────
        var invoiceNumber = await _repo.GenerateInvoiceNumberAsync();

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

        // ── 5. Reload with navigation properties and build response DTO ────────
        var full = await _repo.GetSaleWithDetailsAsync(saved.SaleId)
            ?? throw new InvalidOperationException("Failed to reload sale after save.");

        return (true, "Sale created successfully.", MapToResponse(full));
    }

    // ── Query methods ──────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<SaleResponseDto>> GetAllAsync()
    {
        var sales = await _repo.GetAllSalesAsync();
        return sales.Select(MapToResponse).ToList();
    }

    public async Task<SaleResponseDto?> GetByIdAsync(int id)
    {
        var sale = await _repo.GetSaleWithDetailsAsync(id);
        return sale is null ? null : MapToResponse(sale);
    }

    // ── Mapping ────────────────────────────────────────────────────────────────

    private static SaleResponseDto MapToResponse(Sale s) => new()
    {
        SaleId          = s.SaleId,
        InvoiceNumber   = s.InvoiceNumber,
        CustomerId      = s.CustomerId,
        CustomerName    = s.Customer?.FullName ?? string.Empty,
        PharmacistId    = s.PharmacistId,
        PharmacistName  = s.Pharmacist?.FullName ?? string.Empty,
        SaleDate        = s.SaleDate,
        PaymentMode     = s.PaymentMode,
        Subtotal        = s.Subtotal,
        DiscountPercent = s.DiscountPercent,
        DiscountAmount  = s.DiscountAmount,
        TotalAmount     = s.TotalAmount,
        Items           = s.SaleItems.Select(si => new SaleItemResponseDto
        {
            SaleItemId           = si.SaleItemId,
            MedicineId           = si.MedicineId,
            MedicineName         = si.Medicine?.Name ?? string.Empty,
            BatchId              = si.BatchId,
            BatchNumber          = si.Batch?.BatchNumber ?? string.Empty,
            MedicineUnitId       = si.MedicineUnitId,
            UomName              = si.MedicineUnit?.UnitOfMeasure?.Name ?? string.Empty,
            Quantity             = si.Quantity,
            BaseQuantityDeducted = si.BaseQuantityDeducted,
            UnitPrice            = si.UnitPrice,
            DiscountPercent      = si.DiscountPercent,
            DiscountAmount       = si.DiscountAmount,
            LineTotal            = si.LineTotal
        }).ToList()
    };

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
