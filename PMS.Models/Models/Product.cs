namespace PMS.Models.Models;

/// <summary>
/// Generic product entity — can represent any type of item (not medicine-specific).
/// Products have units of measure attached via ProductUnit.
/// </summary>
public class Product : IHasTenant
{
    public int ProductId { get; set; }

    /// <inheritdoc/>
    public Guid TenantId { get; set; }

    /// <summary>Human-readable product name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Optional description.</summary>
    public string? Description { get; set; }

    /// <summary>Whether this product is active/available.</summary>
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // ── Navigation ─────────────────────────────────────────────────────────────
    public ICollection<ProductUnit> Units { get; set; } = new List<ProductUnit>();
}
