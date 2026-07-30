namespace PMS.Common.DTOs.Category;

/// <summary>Lightweight response — used for GET all (list).</summary>
public class CategoryListItemDto
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int MedicineCount { get; set; }
}
