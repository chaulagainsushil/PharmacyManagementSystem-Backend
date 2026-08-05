using PMS.Application.Application.Interface.Repository;
using PMS.Application.Application.Interface.Service;
using PMS.Common.DTOs.MedicineBatch;
using PMS.Models.Models;

namespace PMS.Application.Application.Service;

public class MedicineBatchService : IMedicineBatchService
{
    private readonly IMedicineBatchRepository _repo;

    public MedicineBatchService(IMedicineBatchRepository repo) => _repo = repo;

    public async Task<IEnumerable<MedicineBatchListItemDto>> GetAllAsync()
    {
        var batches = await _repo.GetAllAsync();
        return batches.Select(MapToListItem);
    }

    public async Task<IEnumerable<MedicineBatchListItemDto>> GetByMedicineIdAsync(int medicineId)
    {
        var batches = await _repo.GetByMedicineIdAsync(medicineId);
        return batches.Select(MapToListItem);
    }

    public async Task<MedicineBatchResponseDto?> GetByIdAsync(int id)
    {
        var batch = await _repo.GetByIdAsync(id);
        return batch is null ? null : MapToResponse(batch);
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
        return (true, "Medicine batch created successfully.", MapToResponse(full!));
    }

    public async Task<(bool Success, string Message, MedicineBatchResponseDto? Data)> UpdateAsync(
        int id, UpdateMedicineBatchDto dto)
    {
        var batch = await _repo.GetByIdAsync(id);
        if (batch is null)
            return (false, $"Medicine batch with ID {id} not found.", null);

        if (await _repo.ExistsByBatchNumberAsync(dto.BatchNumber, excludeId: id))
            return (false, $"Batch number '{dto.BatchNumber}' already exists.", null);

        batch.BatchNumber            = dto.BatchNumber.Trim();
        batch.SupplierId             = dto.SupplierId;
        batch.ManufactureDate        = dto.ManufactureDate;
        batch.ExpiryDate             = dto.ExpiryDate;
        batch.QuantityInTablets      = dto.QuantityInTablets;
        batch.PurchasePricePerTablet = dto.PurchasePricePerTablet;

        await _repo.UpdateAsync(batch);
        var full = await _repo.GetByIdAsync(id);
        return (true, "Medicine batch updated successfully.", MapToResponse(full!));
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
        var batches = await _repo.GetNearExpiryAsync(days);
        return batches.Select(MapToListItem);
    }

    // ── Mapping helpers ────────────────────────────────────────────────────────
    private static MedicineBatchResponseDto MapToResponse(MedicineBatch b) => new()
    {
        BatchId                = b.BatchId,
        MedicineId             = b.MedicineId,
        MedicineName           = b.Medicine?.Name ?? string.Empty,
        BatchNumber            = b.BatchNumber,
        SupplierId             = b.SupplierId,
        SupplierName           = b.Supplier?.Name,
        ManufactureDate        = b.ManufactureDate,
        ExpiryDate             = b.ExpiryDate,
        QuantityInTablets      = b.QuantityInTablets,
        PurchasePricePerTablet = b.PurchasePricePerTablet,
        ReceivedDate           = b.ReceivedDate,
        IsExpired              = b.IsExpired
    };

    private static MedicineBatchListItemDto MapToListItem(MedicineBatch b) => new()
    {
        BatchId                = b.BatchId,
        MedicineId             = b.MedicineId,
        MedicineName           = b.Medicine?.Name ?? string.Empty,
        BatchNumber            = b.BatchNumber,
        SupplierName           = b.Supplier?.Name,
        ExpiryDate             = b.ExpiryDate,
        QuantityInTablets      = b.QuantityInTablets,
        PurchasePricePerTablet = b.PurchasePricePerTablet,
        IsExpired              = b.IsExpired
    };
}
