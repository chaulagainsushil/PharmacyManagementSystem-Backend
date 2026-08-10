namespace PMS.Models.Interfaces;

/// <summary>
/// Provides the current tenant's ID extracted from the authenticated user's JWT claims.
/// Scoped per-request — safe to inject into services and repositories.
/// </summary>
public interface ITenantProvider
{
    /// <summary>
    /// The TenantId parsed from the "tenantId" claim in the current JWT.
    /// Returns <see cref="Guid.Empty"/> for unauthenticated requests or when the claim is absent.
    /// </summary>
    Guid TenantId { get; }

    /// <summary>
    /// True when a non-empty TenantId is available for the current request.
    /// </summary>
    bool HasTenant => TenantId != Guid.Empty;
}
