using System.ComponentModel.DataAnnotations;

namespace PMS.Common.DTOs.Uom;

public class CreateUomDto
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string Symbol { get; set; } = string.Empty;
}

public class UpdateUomDto
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string Symbol { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}
