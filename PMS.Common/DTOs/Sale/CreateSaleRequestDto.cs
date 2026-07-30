using System.ComponentModel.DataAnnotations;
using PMS.Models.Enums;

namespace PMS.Common.DTOs.Sale;

public class CreateSaleRequestDto
{
    [Required(ErrorMessage = "CustomerId is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "CustomerId must be a positive integer.")]
    public int CustomerId { get; set; }

    [Required(ErrorMessage = "PharmacistId is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "PharmacistId must be a positive integer.")]
    public int PharmacistId { get; set; }

    [Range(0, 100, ErrorMessage = "DiscountPercent must be between 0 and 100.")]
    public decimal DiscountPercent { get; set; } = 0;

    [Required(ErrorMessage = "PaymentMode is required.")]
    public PaymentMode PaymentMode { get; set; } = PaymentMode.Cash;

    [Required(ErrorMessage = "At least one sale item is required.")]
    [MinLength(1, ErrorMessage = "At least one sale item is required.")]
    public List<CreateSaleItemDto> Items { get; set; } = new();
}
