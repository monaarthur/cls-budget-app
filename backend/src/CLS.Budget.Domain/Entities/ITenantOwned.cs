namespace CLS.Budget.Domain.Entities;

/// <summary>
/// Marks an entity whose rows belong to a single <see cref="Tenant"/> and are
/// isolated via the EF Core global query filter and auto-stamped on save.
/// </summary>
public interface ITenantOwned
{
    Guid TenantId { get; set; }
}
