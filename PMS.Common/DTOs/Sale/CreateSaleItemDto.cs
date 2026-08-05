using System.ComponentModel.DataAnnotations;

namespace PMS.Common.DTOs.Sale;

public class CreateSaleItemDto
{
    [Required(ErrorMessage = "MedicineId is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "MedicineId must be a positive integer.")]
    public int MedicineId { get; set; }

    /// <summary>
    /// The MedicineUnit the customer is purchasing in (e.g. the Strip unit or Tablet unit).
    /// Must be an active unit linked to the given MedicineId.
    /// </summary>
    [Required(ErrorMessage = "MedicineUnitId is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "MedicineUnitId must be a positive integer.")]
    public int MedicineUnitId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1.")]
    public int Quantity { get; set; }

    [Range(0, 100, ErrorMessage = "DiscountPercent must be between 0 and 100.")]
    public decimal DiscountPercent { get; set; } = 0;
}
