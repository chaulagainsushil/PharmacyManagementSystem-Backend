namespace PMS.Models.Models;

public class Supplier : IHasTenant
{
    public int SupplierId { get; set; }

    /// <inheritdoc/>
    public Guid TenantId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<MedicineBatch> Batches { get; set; } = new List<MedicineBatch>();
}
