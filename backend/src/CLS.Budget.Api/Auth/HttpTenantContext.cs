using CLS.Budget.Application.Abstractions;
using CLS.Budget.Domain;

namespace CLS.Budget.Api.Auth;

/// <summary>
/// Resolves the tenant from the authenticated user's <c>tenant_id</c> claim.
/// Until auth enforcement lands, unauthenticated requests fall back to the seeded default tenant.
/// </summary>
public sealed class HttpTenantContext(IHttpContextAccessor accessor) : ITenantContext
{
    public Guid TenantId
    {
        get
        {
            var value = accessor.HttpContext?.User.FindFirst("tenant_id")?.Value;
            return Guid.TryParse(value, out var id) ? id : SeedTenant.DefaultTenantId;
        }
    }
}
