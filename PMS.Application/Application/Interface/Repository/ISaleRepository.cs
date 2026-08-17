using PMS.Common.DTOs.Sale;
using PMS.Models.Models;

namespace PMS.Application.Application.Interface.Repository;

public interface ISaleRepository
{
    /// <summary>
    /// Fetch a medicine with its active MedicineUnits AND non-expired, in-stock batches
    /// ordered by FEFO (nearest expiry first).
    /// Returns null if medicine not found or inactive.
    /// </summary>
    Task<Medicine?> GetMedicineWithUnitsAndFefoStockAsync(int medicineId);

    /// <summary>
    /// Fetch a product with its active ProductUnits.
    /// Returns null if product not found or inactive.
    /// </summary>
    Task<Product?> GetProductWithUnitsAsync(int productId);

    /// <summary>Returns true if the customer exists.</summary>
    Task<bool> CustomerExistsAsync(int customerId);

    /// <summary>Returns true if the pharmacist (User) exists.</summary>
    Task<bool> PharmacistExistsAsync(int pharmacistId);

    /// <summary>
    /// Persists a completed Sale (with its SaleItems already populated)
    /// and deducts <paramref name="batchDeductions"/> from MedicineBatch.QuantityInTablets —
    /// all inside a single database transaction.
    /// </summary>
    Task<Sale> SaveSaleAsync(Sale sale, IReadOnlyList<(int BatchId, int BaseUnitsToDeduct)> batchDeductions);

    /// <summary>
    /// Reload a sale with all fields projected directly to a response DTO.
    /// </summary>
    Task<SaleResponseDto?> GetSaleWithDetailsAsync(int saleId);

    /// <summary>
    /// Return all sales projected to response DTOs, ordered newest first.
    /// </summary>
    Task<IReadOnlyList<SaleResponseDto>> GetAllSalesAsync();

    /// <summary>
    /// Generate a unique, sequential invoice number (e.g. INV-20260730-0001).
    /// </summary>
    Task<string> GenerateInvoiceNumberAsync();
}
