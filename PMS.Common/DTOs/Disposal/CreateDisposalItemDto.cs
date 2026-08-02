using System.ComponentModel.DataAnnotations;

namespace PMS.Common.DTOs.Disposal;

public class CreateDisposalItemDto
{
    [Required]
    public int MedicineId { get; set; }

    [Required]
    public int BatchId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1.")]
    public int QuantityInTablets { get; set; }

    [Required(ErrorMessage = "Reason is required.")]
    [MaxLength(500)]
    public string Reason { get; set; } = string.Empty;
}
