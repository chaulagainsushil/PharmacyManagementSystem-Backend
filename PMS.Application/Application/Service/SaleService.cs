using PMS.Application.Application.Interface.Repository;
using PMS.Application.Application.Interface.Service;
using PMS.Common.DTOs.Sale;
using PMS.Models.Enums;
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
        // We collect everything we need before touching the DB inside the
        // transaction so we can give clear error messages up-front.
        //
        // plannedItems     — one entry per (MedicineId, BatchId) pairing produced
        //                    by FEFO splitting across batches.
        // batchDeductions  — flat list of (BatchId, tablets) sent to the repo.

        var plannedItems    = new List<PlannedSaleItem>();
        var batchDeductions = new List<(int BatchId, int TabletsToDeduct)>();

        foreach (var itemDto in dto.Items)
        {
            // Load medicine + its eligible FEFO batches
            var medicine = await _repo.GetMedicineWithFefoStockAsync(itemDto.MedicineId);
            if (medicine is null)
                return (false, $"Medicine with ID {itemDto.MedicineId} not found or inactive.", null);

            // Convert requested quantity to tablets
            var tabletsRequested = itemDto.SaleUnitType == SaleUnitType.Strip
                ? itemDto.Quantity * medicine.TabletsPerStrip
                : itemDto.Quantity;

            // Determine unit price (snapshot at sale time)
            var unitPrice = itemDto.SaleUnitType == SaleUnitType.Strip
                ? medicine.StripPrice
                : medicine.TabletPrice;

            // Check total available stock across non-expired batches
            var totalAvailable = medicine.Batches.Sum(b => b.QuantityInTablets);
            if (totalAvailable < tabletsRequested)
                return (false,
                    $"Insufficient stock for '{medicine.Name}'. " +
                    $"Requested {tabletsRequested} tablet(s) but only {totalAvailable} available.",
                    null);

            // ── FEFO split: drain batches from nearest expiry first ────────────
            var remaining = tabletsRequested;
            foreach (var batch in medicine.Batches) // already sorted by ExpiryDate ASC
            {
                if (remaining <= 0) break;

                var takeFromBatch = Math.Min(remaining, batch.QuantityInTablets);
                remaining -= takeFromBatch;

                // Convert tablets taken back into the unit the customer ordered
                // so we price correctly: strips × stripPrice  OR  tablets × tabletPrice
                var unitQuantityTaken = itemDto.SaleUnitType == SaleUnitType.Strip
                    ? takeFromBatch / medicine.TabletsPerStrip
                    : takeFromBatch;

                // grossAmount is always in the customer's chosen unit
                var grossAmount    = unitQuantityTaken * unitPrice;
                var discountAmount = Math.Round(grossAmount * itemDto.DiscountPercent / 100m, 4);
                var lineTotal      = Math.Round(grossAmount - discountAmount, 4);

                plannedItems.Add(new PlannedSaleItem(
                    MedicineId      : itemDto.MedicineId,
                    BatchId         : batch.BatchId,
                    SaleUnitType    : itemDto.SaleUnitType,
                    Quantity        : unitQuantityTaken,
                    TabletsDeducted : takeFromBatch,
                    UnitPrice       : unitPrice,
                    DiscountPercent : itemDto.DiscountPercent,
                    DiscountAmount  : discountAmount,
                    LineTotal       : lineTotal
                ));

                batchDeductions.Add((batch.BatchId, takeFromBatch));
            }
        }

        // ── 3. Build the Sale aggregate ───────────────────────────────────────
        var invoiceNumber = await _repo.GenerateInvoiceNumberAsync();

        var subtotal = plannedItems.Sum(p => p.LineTotal);

        // Invoice-level discount applied on top of per-item discounts
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
                MedicineId      = p.MedicineId,
                BatchId         = p.BatchId,
                SaleUnitType    = p.SaleUnitType,
                Quantity        = p.Quantity,
                UnitPrice       = p.UnitPrice,
                DiscountPercent = p.DiscountPercent,
                DiscountAmount  = p.DiscountAmount,
                LineTotal       = p.LineTotal
            }).ToList()
        };

        // ── 4. Persist inside a single transaction (repo handles this) ─────────
        var saved = await _repo.SaveSaleAsync(sale, batchDeductions);

        // ── 5. Reload with navigation properties and build response DTO ────────
        var full = await _repo.GetSaleWithDetailsAsync(saved.SaleId)
            ?? throw new InvalidOperationException("Failed to reload sale after save.");

        return (true, "Sale created successfully.", MapToResponse(full));
    }

    // ── Query methods ──────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task<IReadOnlyList<SaleResponseDto>> GetAllAsync()
    {
        var sales = await _repo.GetAllSalesAsync();
        return sales.Select(MapToResponse).ToList();
    }

    /// <inheritdoc/>
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
            SaleItemId      = si.SaleItemId,
            MedicineId      = si.MedicineId,
            MedicineName    = si.Medicine?.Name ?? string.Empty,
            BatchId         = si.BatchId,
            BatchNumber     = si.Batch?.BatchNumber ?? string.Empty,
            SaleUnitType    = si.SaleUnitType,
            Quantity        = si.Quantity,
            // TabletsDeducted: recompute from stored Quantity + unit type
            TabletsDeducted = si.SaleUnitType == SaleUnitType.Strip && si.Medicine is not null
                                ? si.Quantity * si.Medicine.TabletsPerStrip
                                : si.Quantity,
            UnitPrice       = si.UnitPrice,
            DiscountPercent = si.DiscountPercent,
            DiscountAmount  = si.DiscountAmount,
            LineTotal       = si.LineTotal
        }).ToList()
    };

    // ── Private record for in-memory FEFO planning ─────────────────────────────

    private sealed record PlannedSaleItem(
        int          MedicineId,
        int          BatchId,
        SaleUnitType SaleUnitType,
        int          Quantity,
        int          TabletsDeducted,
        decimal      UnitPrice,
        decimal      DiscountPercent,
        decimal      DiscountAmount,
        decimal      LineTotal);
}
