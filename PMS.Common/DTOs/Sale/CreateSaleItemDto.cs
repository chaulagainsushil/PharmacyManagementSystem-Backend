using System.ComponentModel.DataAnnotations;

namespace PMS.Common.DTOs.Sale;

/// <summary>
/// DTO for creating a sale line item. Can be either a Medicine or a Product.
/// Set ItemType to determine which entity is being sold.
/// </summary>
public class CreateSaleItemDto
{
    /// <summary>Type of item: "Medicine" or "Product". Defaults to "Medicine".</summary>
    public string ItemType { get; set; } = "Medicine";

    // ── Medicine fields (required when ItemType = "Medicine") ──
    
    /// <summary>Medicine ID. Required if ItemType is "Medicine".</summary>
    public int? MedicineId { get; set; }

    /// <summary>
    /// The MedicineUnit the customer is purchasing in (e.g. the Strip unit or Tablet unit).
    /// Must be an active unit linked to the given MedicineId.
    /// Required if ItemType is "Medicine".
    /// </summary>
    public int? MedicineUnitId { get; set; }

    // ── Product fields (required when ItemType = "Product") ──
    
    /// <summary>Product ID. Required if ItemType is "Product".</summary>
    public int? ProductId { get; set; }

    /// <summary>
    /// The ProductUnit the customer is purchasing in (e.g. Box, Tablet).
    /// Required if ItemType is "Product".
    /// </summary>
    public int? ProductUnitId { get; set; }

    // ── Common fields ──
    
    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1.")]
    public int Quantity { get; set; }

    [Range(0, 100, ErrorMessage = "DiscountPercent must be between 0 and 100.")]
    public decimal DiscountPercent { get; set; } = 0;
}
