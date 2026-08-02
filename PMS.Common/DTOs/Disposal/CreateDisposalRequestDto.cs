using System.ComponentModel.DataAnnotations;

namespace PMS.Common.DTOs.Disposal;

public class CreateDisposalRequestDto
{
    [Required]
    public int PharmacistId { get; set; }

    [Required, MinLength(1, ErrorMessage = "At least one item is required.")]
    public List<CreateDisposalItemDto> Items { get; set; } = new();
}
