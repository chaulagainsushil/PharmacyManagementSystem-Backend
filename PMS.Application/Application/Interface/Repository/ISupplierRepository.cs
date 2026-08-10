using PMS.Common.DTOs.Supplier;
using PMS.Models.Models;

namespace PMS.Application.Application.Interface.Repository;

public interface ISupplierRepository
{
    Task<IEnumerable<SupplierListItemDto>> GetAllAsync();
    Task<SupplierResponseDto?> GetByIdAsync(int id);

    /// <summary>Returns the tracked entity for update/delete operations.</summary>
    Task<Supplier?> GetEntityByIdAsync(int id);

    Task<bool> ExistsByNameAsync(string name, int? excludeId = null);
    Task<Supplier> CreateAsync(Supplier supplier);
    Task<Supplier> UpdateAsync(Supplier supplier);
    Task<bool> DeleteAsync(int id);
    Task<bool> HasBatchesAsync(int id);
}
