using PMS.Models.Models;

namespace PMS.Application.Application.Interface.Repository;

public interface IProductUnitRepository
{
    /// <summary>Get all units for a product.</summary>
    Task<IReadOnlyList<ProductUnit>> GetByProductIdAsync(int productId);

    /// <summary>Get a specific unit by ID.</summary>
    Task<ProductUnit?> GetByIdAsync(int productUnitId);

    /// <summary>Check if a product already has a given UoM attached.</summary>
    Task<bool> ExistsAsync(int productId, int unitOfMeasureId, int? excludeId = null);

    /// <summary>Create a new product unit.</summary>
    Task<ProductUnit> CreateAsync(ProductUnit unit);

    /// <summary>Update an existing product unit.</summary>
    Task<ProductUnit> UpdateAsync(ProductUnit unit);

    /// <summary>Load product with its units collection.</summary>
    Task<Product?> GetProductWithUnitsAsync(int productId);
}
