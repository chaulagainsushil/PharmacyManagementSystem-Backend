namespace PMS.Common.DTOs.Category;

/// <summary>Full detail response — used for GET by ID.</summary>
public class CategoryResponseDto
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int MedicineCount { get; set; }
    public DateTime UpdatedAt { get; set; }
}
