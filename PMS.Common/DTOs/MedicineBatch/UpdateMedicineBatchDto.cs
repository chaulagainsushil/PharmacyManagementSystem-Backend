using System.ComponentModel.DataAnnotations;

namespace PMS.Common.DTOs.MedicineBatch;

public class UpdateMedicineBatchDto
{
    [Required(ErrorMessage = "Batch number is required.")]
    [MaxLength(100, ErrorMessage = "Batch number must not exceed 100 characters.")]
    public string BatchNumber { get; set; } = string.Empty;

    public int? SupplierId { get; set; }

    public DateTime? ManufactureDate { get; set; }

    [Required(ErrorMessage = "Expiry date is required.")]
    public DateTime ExpiryDate { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1.")]
    public int QuantityInTablets { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Purchase price must be non-negative.")]
    public decimal PurchasePricePerTablet { get; set; }

    public DateTime UpdatedAt { get; set; }
}
