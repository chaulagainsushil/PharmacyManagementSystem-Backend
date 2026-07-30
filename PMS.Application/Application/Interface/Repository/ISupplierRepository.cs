using PMS.Models.Models;

namespace PMS.Application.Application.Interface.Repository;

public interface ISupplierRepository
{
    Task<IEnumerable<Supplier>> GetAllAsync();
    Task<Supplier?> GetByIdAsync(int id);
    Task<bool> ExistsByNameAsync(string name, int? excludeId = null);
    Task<Supplier> CreateAsync(Supplier supplier);
    Task<Supplier> UpdateAsync(Supplier supplier);
    Task<bool> DeleteAsync(int id);
    Task<bool> HasBatchesAsync(int id);
}
