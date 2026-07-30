using System.ComponentModel.DataAnnotations;
using PMS.Models.Enums;

namespace PMS.Common.DTOs.Sale;

public class CreateSaleItemDto
{
    [Required(ErrorMessage = "MedicineId is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "MedicineId must be a positive integer.")]
    public int MedicineId { get; set; }

    [Required(ErrorMessage = "SaleUnitType is required.")]
    public SaleUnitType SaleUnitType { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1.")]
    public int Quantity { get; set; }

    [Range(0, 100, ErrorMessage = "DiscountPercent must be between 0 and 100.")]
    public decimal DiscountPercent { get; set; } = 0;
}
