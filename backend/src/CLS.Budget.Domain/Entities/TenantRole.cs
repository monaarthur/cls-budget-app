namespace CLS.Budget.Domain.Entities;

/// <summary>
/// Role of an <see cref="AppUser"/> within their <see cref="Tenant"/>.
/// </summary>
public enum TenantRole
{
    Member = 0,
    Owner = 1
}
