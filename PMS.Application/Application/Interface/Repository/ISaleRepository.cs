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
    /// Reload a sale with all navigation properties needed for the response DTO.
    /// </summary>
    Task<Sale?> GetSaleWithDetailsAsync(int saleId);

    /// <summary>
    /// Return all sales with navigation properties, ordered newest first.
    /// </summary>
    Task<IReadOnlyList<Sale>> GetAllSalesAsync();

    /// <summary>
    /// Generate a unique, sequential invoice number (e.g. INV-20260730-0001).
    /// </summary>
    Task<string> GenerateInvoiceNumberAsync();
}
