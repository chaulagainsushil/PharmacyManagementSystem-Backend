namespace PMS.Models.Models;

public class Customer : IHasTenant
{
    public int CustomerId { get; set; }

    /// <inheritdoc/>
    public Guid TenantId { get; set; }

    public string FullName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;   // mandatory; unique per tenant
    public string? Address { get; set; }
    public bool IsDefault { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Sale> Sales { get; set; } = new List<Sale>();
}
