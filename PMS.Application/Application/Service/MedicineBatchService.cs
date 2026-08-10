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

    public async Task<IEnumerable<MedicineBatchListItemDto>> GetAllAsync()
    {
        return await _repo.GetAllAsync();
    }

    public async Task<IEnumerable<MedicineBatchListItemDto>> GetByMedicineIdAsync(int medicineId)
    {
        return await _repo.GetByMedicineIdAsync(medicineId);
    }

    public async Task<MedicineBatchResponseDto?> GetByIdAsync(int id)
    {
        return await _repo.GetByIdAsync(id);
    }

    public async Task<(bool Success, string Message, MedicineBatchResponseDto? Data)> CreateAsync(
        CreateMedicineBatchDto dto)
    {
        if (await _repo.ExistsByBatchNumberAsync(dto.BatchNumber))
            return (false, $"Batch number '{dto.BatchNumber}' already exists.", null);

        if (dto.ExpiryDate.Date <= DateTime.UtcNow.Date)
            return (false, "Expiry date must be in the future.", null);

        var batch = new MedicineBatch
        {
            MedicineId              = dto.MedicineId,
            BatchNumber             = dto.BatchNumber.Trim(),
            SupplierId              = dto.SupplierId,
            ManufactureDate         = dto.ManufactureDate,
            ExpiryDate              = dto.ExpiryDate,
            QuantityInTablets       = dto.QuantityInTablets,
            PurchasePricePerTablet  = dto.PurchasePricePerTablet,
            ReceivedDate            = dto.ReceivedDate == default ? DateTime.UtcNow : dto.ReceivedDate
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

        entity.BatchNumber            = dto.BatchNumber.Trim();
        entity.SupplierId             = dto.SupplierId;
        entity.ManufactureDate        = dto.ManufactureDate;
        entity.ExpiryDate             = dto.ExpiryDate;
        entity.QuantityInTablets      = dto.QuantityInTablets;
        entity.PurchasePricePerTablet = dto.PurchasePricePerTablet;

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

    public async Task<IEnumerable<MedicineBatchListItemDto>> GetNearExpiryAsync(int days = 45)
    {
        return await _repo.GetNearExpiryAsync(days);
    }
}
