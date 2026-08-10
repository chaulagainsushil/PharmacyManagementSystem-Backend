using System.ComponentModel.DataAnnotations;

namespace PMS.Common.DTOs.Supplier;

public class UpdateSupplierDto
{
    [Required(ErrorMessage = "Supplier name is required.")]
    [MaxLength(200, ErrorMessage = "Name must not exceed 200 characters.")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(20, ErrorMessage = "Phone must not exceed 20 characters.")]
    public string? Phone { get; set; }

    [MaxLength(500, ErrorMessage = "Address must not exceed 500 characters.")]
    public string? Address { get; set; }

    public DateTime UpdatedAt { get; set; }
}
