using PMS.Models.Models;

namespace PMS.Application.Application.Interface.Repository;

public interface IMedicineRepository
{
    Task<IEnumerable<Medicine>> GetAllAsync();
    Task<Medicine?> GetByIdAsync(int id);
    Task<bool> ExistsByNameAsync(string name, int? excludeId = null);
    Task<Medicine> CreateAsync(Medicine medicine);
    Task<Medicine> UpdateAsync(Medicine medicine);
    Task<bool> DeleteAsync(int id);
}
