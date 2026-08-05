using PMS.Models.Models;

namespace PMS.Application.Application.Interface.Repository;

public interface IUnitOfMeasureRepository
{
    Task<IReadOnlyList<UnitOfMeasure>> GetAllAsync(bool includeInactive = false);
    Task<UnitOfMeasure?> GetByIdAsync(int id);
    Task<bool> ExistsByNameAsync(string name, int? excludeId = null);
    Task<UnitOfMeasure> CreateAsync(UnitOfMeasure uom);
    Task<UnitOfMeasure> UpdateAsync(UnitOfMeasure uom);
    Task<bool> DeleteAsync(int id);
}
