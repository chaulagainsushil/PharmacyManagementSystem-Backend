namespace PMS.Models.Models;

/// <summary>
/// Marker interface for every entity that is scoped to a tenant.
/// ApplicationDbContext uses this interface via reflection to register
/// a global EF Core query filter on every implementing entity, ensuring
/// all queries are automatically restricted to the current tenant.
/// </summary>
public interface IHasTenant
{
    /// <summary>The tenant this record belongs to (maps to PharmacyProfile.TenantId).</summary>
    Guid TenantId { get; set; }
}
