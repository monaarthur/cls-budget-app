using CLS.Budget.Application.Abstractions;
using CLS.Budget.Domain;

namespace CLS.Budget.Infrastructure.Tenancy;

/// <summary>
/// Fallback tenant context for hosts without an HTTP request (e.g. the import CLI,
/// design-time tooling). Resolves to the seeded default tenant.
/// </summary>
public sealed class DefaultTenantContext : ITenantContext
{
    public Guid TenantId => SeedTenant.DefaultTenantId;
}
