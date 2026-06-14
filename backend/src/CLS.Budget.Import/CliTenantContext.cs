using CLS.Budget.Application.Abstractions;

namespace CLS.Budget.Import;

/// <summary>
/// Tenant context for the import CLI. The target tenant is supplied via the
/// <c>--tenant &lt;guid&gt;</c> argument and defaults to the seeded tenant.
/// </summary>
internal sealed class CliTenantContext(Guid tenantId) : ITenantContext
{
    public Guid TenantId { get; } = tenantId;
}
