using PMS.Models.Interfaces;

namespace PMS.TenantProvider;

/// <summary>
/// Reads the "tenantId" claim from the current HTTP request's JWT token.
/// Registered as a scoped service in DI so it has access to <see cref="IHttpContextAccessor"/>.
/// </summary>
public class HttpContextTenantProvider : ITenantProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpContextTenantProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <inheritdoc/>
    public Guid TenantId
    {
        get
        {
            var claim = _httpContextAccessor.HttpContext?
                .User.FindFirst("tenantId")?.Value;

            return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
        }
    }
}
