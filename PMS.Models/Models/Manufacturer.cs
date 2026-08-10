namespace PMS.Models.Models;

public class Manufacturer : IHasTenant
{
    public int ManufacturerId { get; set; }

    /// <inheritdoc/>
    public Guid TenantId { get; set; }

    public string Name { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Medicine> Medicines { get; set; } = new List<Medicine>();
}
