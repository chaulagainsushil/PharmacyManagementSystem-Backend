using System.ComponentModel.DataAnnotations;

namespace PMS.Common.DTOs.Category;

public class CreateCategoryDto
{
    [Required(ErrorMessage = "Category name is required.")]
    [MaxLength(100, ErrorMessage = "Category name must not exceed 100 characters.")]
    public string CategoryName { get; set; } = string.Empty;
}
