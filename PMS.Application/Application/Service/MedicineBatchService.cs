using PMS.Application.Application.Interface.Repository;
using PMS.Application.Application.Interface.Service;
using PMS.Common;
using PMS.Common.DTOs.MedicineBatch;
using PMS.Models.Models;

namespace PMS.Application.Application.Service;

public class MedicineBatchService : IMedicineBatchService
{
    private readonly IMedicineBatchRepository _repo;

    public MedicineBatchService(IMedicineBatchRepository repo) => _repo = repo;

    public async Task<IEnumerable<MedicineBatchListItemDto>> GetAllAsync() =>
        await _repo.GetAllAsync();

    public async Task<IEnumerable<MedicineBatchListItemDto>> GetByMedicineIdAsync(int medicineId) =>
        await _repo.GetByMedicineIdAsync(medicineId);

    public async Task<MedicineBatchResponseDto?> GetByIdAsync(int id) =>
        await _repo.GetByIdAsync(id);

    public async Task<(bool Success, string Message, MedicineBatchResponseDto? Data)> CreateAsync(
        CreateMedicineBatchDto dto)
    {
        if (await _repo.ExistsByBatchNumberAsync(dto.BatchNumber))
            return (false, $"Batch number '{dto.BatchNumber}' already exists.", null);

        if (dto.ExpiryDate.Date <= DateTime.UtcNow.Date)
            return (false, "Expiry date must be in the future.", null);

        int    quantityInBaseUnit;
        decimal costPricePerBaseUnit;
        int?   purchaseMedicineUnitId        = null;
        int?   purchaseQuantityInPurchaseUnit = null;

        if (dto.PurchaseMedicineUnitId.HasValue)
        {
            // ── Unit-aware mode: validate the unit and convert ────────────────
            var purchaseUnit = await _repo.GetPurchaseUnitAsync(
                dto.MedicineId, dto.PurchaseMedicineUnitId.Value);

            if (purchaseUnit is null)
                return (false,
                    $"MedicineUnit {dto.PurchaseMedicineUnitId.Value} was not found, " +
                    "is inactive, or does not belong to this medicine.", null);

            if (!purchaseUnit.CanPurchase)
                return (false,
                    $"Unit '{purchaseUnit.UnitOfMeasure?.Name ?? purchaseUnit.MedicineUnitId.ToString()}' " +
                    "is not marked as purchasable (CanPurchase = false). " +
                    "Enable CanPurchase on this unit or choose a different unit.", null);

            // Convert purchase quantity → base units
            // ConversionFactorToBase = how many base units are in 1 purchase unit
            // e.g. 1 Strip = 10 Tablets → factor = 10 → 50 Strips × 10 = 500 Tablets
            quantityInBaseUnit = (int)Math.Round(
                dto.PurchaseQuantity * purchaseUnit.ConversionFactorToBase);

            // Derive cost per base unit from cost per purchase unit
            // e.g. Rs 18 per Strip ÷ 10 = Rs 1.80 per Tablet
            costPricePerBaseUnit = purchaseUnit.ConversionFactorToBase > 0
                ? dto.CostPricePerPurchaseUnit / purchaseUnit.ConversionFactorToBase
                : 0m;

            purchaseMedicineUnitId        = dto.PurchaseMedicineUnitId.Value;
            purchaseQuantityInPurchaseUnit = dto.PurchaseQuantity;
        }
        else
        {
            // ── Base-unit mode: quantity and cost are already in base units ───
            quantityInBaseUnit   = dto.PurchaseQuantity;
            costPricePerBaseUnit = dto.CostPricePerBaseUnit;
        }

        var batch = new MedicineBatch
        {
            MedicineId                   = dto.MedicineId,
            BatchNumber                  = dto.BatchNumber.Trim(),
            SupplierId                   = dto.SupplierId,
            ManufactureDate              = dto.ManufactureDate,
            ExpiryDate                   = dto.ExpiryDate,
            QuantityInBaseUnit           = quantityInBaseUnit,
            CostPricePerBaseUnit         = costPricePerBaseUnit,
            PurchaseMedicineUnitId       = purchaseMedicineUnitId,
            PurchaseQuantityInPurchaseUnit = purchaseQuantityInPurchaseUnit,
            ReceivedDate                 = dto.ReceivedDate == default ? DateTime.UtcNow : dto.ReceivedDate
        };

        var created = await _repo.CreateAsync(batch);
        var full    = await _repo.GetByIdAsync(created.BatchId);
        return (true, "Medicine batch created successfully.", full);
    }

    public async Task<(bool Success, string Message, bool IsConflict, MedicineBatchResponseDto? Data)> UpdateAsync(
        int id, UpdateMedicineBatchDto dto)
    {
        // 1. Read current DTO (includes UpdatedAt)
        var existing = await _repo.GetByIdAsync(id);
        if (existing is null)
            return (false, $"Medicine batch with ID {id} not found.", false, null);

        // 2. Optimistic concurrency check
        if (!ConcurrencyHelper.Matches(existing.UpdatedAt, dto.UpdatedAt))
            return (false, "The record was modified by another user. Please refresh and try again.", true, null);

        // 3. Business rule: duplicate batch number
        if (await _repo.ExistsByBatchNumberAsync(dto.BatchNumber, excludeId: id))
            return (false, $"Batch number '{dto.BatchNumber}' already exists.", false, null);

        // 4. Fetch tracked entity and mutate
        var entity = await _repo.GetEntityByIdAsync(id);
        if (entity is null)
            return (false, $"Medicine batch with ID {id} not found.", false, null);

        entity.BatchNumber          = dto.BatchNumber.Trim();
        entity.SupplierId           = dto.SupplierId;
        entity.ManufactureDate      = dto.ManufactureDate;
        entity.ExpiryDate           = dto.ExpiryDate;
        entity.QuantityInBaseUnit   = dto.QuantityInBaseUnit;
        entity.CostPricePerBaseUnit = dto.CostPricePerBaseUnit;

        await _repo.UpdateAsync(entity);

        var full = await _repo.GetByIdAsync(id);
        return (true, "Medicine batch updated successfully.", false, full);
    }

    public async Task<BulkCreateMedicineBatchResultDto> BulkCreateAsync(BulkCreateMedicineBatchDto dto)
    {
        var results = new List<BulkBatchResultItemDto>();
        int created = 0, failed = 0;

        for (int i = 0; i < dto.Batches.Count; i++)
        {
            var (success, message, data) = await CreateAsync(dto.Batches[i]);
            results.Add(new BulkBatchResultItemDto
            {
                Index   = i,
                Success = success,
                Message = message,
                Data    = data,
            });
            if (success) created++; else failed++;
        }

        return new BulkCreateMedicineBatchResultDto
        {
            TotalRequested = dto.Batches.Count,
            TotalCreated   = created,
            TotalFailed    = failed,
            Results        = results,
        };
    }

    public async Task<(bool Success, string Message)> DeleteAsync(int id)
    {
        var batch = await _repo.GetByIdAsync(id);
        if (batch is null)
            return (false, $"Medicine batch with ID {id} not found.");

        await _repo.DeleteAsync(id);
        return (true, "Medicine batch deleted successfully.");
    }

    public async Task<IEnumerable<MedicineBatchListItemDto>> GetNearExpiryAsync(int days = 45) =>
        await _repo.GetNearExpiryAsync(days);
}
