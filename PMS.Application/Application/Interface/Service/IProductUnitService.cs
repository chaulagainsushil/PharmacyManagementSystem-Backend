using PMS.Common.DTOs.Product;

namespace PMS.Application.Application.Interface.Service;

public interface IProductUnitService
{
    /// <summary>
    /// Attach a base unit and optional sub unit to a product in one call.
    /// Auto-calculates second-unit TotalUnitQty and pricing if not provided.
    /// </summary>
    Task<(bool Success, string Message, IReadOnlyList<ProductUnitDto>? Data)> AddDualUnitAsync(
        int productId, AddDualUnitCommand cmd);

    /// <summary>
    /// Get all units for a product.
    /// </summary>
    Task<(bool Found, ProductUnitsResponse? Data)> GetUnitsAsync(int productId);
}
