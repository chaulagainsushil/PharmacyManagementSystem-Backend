using PMS.Application.Application.Interface.Repository;
using PMS.Application.Application.Interface.Service;
using PMS.Common.DTOs.Disposal;
using PMS.Models.Models;

namespace PMS.Application.Application.Service;

public class DisposalService : IDisposalService
{
    private readonly IDisposalRepository _repo;

    public DisposalService(IDisposalRepository repo) => _repo = repo;

    public async Task<(bool Success, string Message, DisposalResponseDto? Data)> CreateDisposalAsync(
        CreateDisposalRequestDto dto)
    {
        if (!await _repo.PharmacistExistsAsync(dto.PharmacistId))
            return (false, $"Pharmacist with ID {dto.PharmacistId} not found.", null);

        if (dto.Items == null || dto.Items.Count == 0)
            return (false, "At least one item is required.", null);

        var deductions   = new List<(int BatchId, int TabletsToDeduct)>();
        var plannedItems = new List<ExpiredMedicineDisposalItem>();

        foreach (var itemDto in dto.Items)
        {
            var batch = await _repo.GetBatchAsync(itemDto.BatchId);
            if (batch is null)
                return (false, $"Batch with ID {itemDto.BatchId} not found.", null);

            if (itemDto.QuantityInTablets > batch.QuantityInTablets)
                return (false,
                    $"Cannot dispose {itemDto.QuantityInTablets} tablets from batch '{batch.BatchNumber}' " +
                    $"— only {batch.QuantityInTablets} available.",
                    null);

            plannedItems.Add(new ExpiredMedicineDisposalItem
            {
                MedicineId        = batch.MedicineId,
                BatchId           = itemDto.BatchId,
                QuantityInTablets = itemDto.QuantityInTablets,
                Reason            = itemDto.Reason.Trim()
            });

            deductions.Add((itemDto.BatchId, itemDto.QuantityInTablets));
        }

        var disposalNumber = await _repo.GenerateDisposalNumberAsync();

        var disposal = new ExpiredMedicineDisposal
        {
            DisposalNumber = disposalNumber,
            PharmacistId   = dto.PharmacistId,
            DisposalDate   = DateTime.UtcNow,
            CreatedAt      = DateTime.UtcNow,
            Items          = plannedItems
        };

        var saved = await _repo.SaveDisposalAsync(disposal, deductions);

        var full = await _repo.GetDisposalWithDetailsAsync(saved.DisposalId)
            ?? throw new InvalidOperationException("Failed to reload disposal after save.");

        return (true, "Disposal recorded successfully.", full);
    }

    public async Task<IReadOnlyList<DisposalResponseDto>> GetAllAsync() =>
        await _repo.GetAllDisposalsAsync();

    public async Task<DisposalResponseDto?> GetByIdAsync(int id) =>
        await _repo.GetDisposalWithDetailsAsync(id);
}
