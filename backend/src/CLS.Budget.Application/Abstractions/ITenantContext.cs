namespace CLS.Budget.Application.Abstractions;

/// <summary>
/// Supplies the tenant that the current operation is scoped to. Backed by the
/// authenticated user's <c>tenant_id</c> claim at runtime, with a development fallback.
/// </summary>
public interface ITenantContext
{
    Guid TenantId { get; }
}
