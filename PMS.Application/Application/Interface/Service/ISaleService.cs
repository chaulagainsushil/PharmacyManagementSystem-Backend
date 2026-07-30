using PMS.Common.DTOs.Sale;

namespace PMS.Application.Application.Interface.Service;

public interface ISaleService
{
    /// <summary>
    /// Process a sale: validate stock, apply FEFO deduction across batches,
    /// calculate discounts, and persist everything in a single transaction.
    /// Returns (false, errorMessage, null) for any validation failure.
    /// </summary>
    Task<(bool Success, string Message, SaleResponseDto? Data)> CreateSaleAsync(CreateSaleRequestDto dto);

    /// <summary>Return all sales ordered newest first.</summary>
    Task<IReadOnlyList<SaleResponseDto>> GetAllAsync();

    /// <summary>Return a single sale by ID, or null if not found.</summary>
    Task<SaleResponseDto?> GetByIdAsync(int id);
}
