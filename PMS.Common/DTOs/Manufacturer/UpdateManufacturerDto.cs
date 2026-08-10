using System.ComponentModel.DataAnnotations;

namespace PMS.Common.DTOs.Manufacturer;

public class UpdateManufacturerDto
{
    [Required(ErrorMessage = "Manufacturer name is required.")]
    [MaxLength(200, ErrorMessage = "Name must not exceed 200 characters.")]
    public string Name { get; set; } = string.Empty;

    public DateTime UpdatedAt { get; set; }
}
