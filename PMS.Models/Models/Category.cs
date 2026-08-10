namespace PMS.Models.Models;

public class Category : IHasTenant
{
    public int CategoryId { get; set; }

    /// <inheritdoc/>
    public Guid TenantId { get; set; }

    public string CategoryName { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Medicine> Medicines { get; set; } = new List<Medicine>();
}
