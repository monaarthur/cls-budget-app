namespace CLS.Budget.Domain.Entities;

/// <summary>
/// Top-level account category (e.g. Credit Card, Loan).
/// System rows have <see cref="TenantId"/> null and <see cref="IsSystem"/> true.
/// Tenant-owned custom categories have a TenantId.
/// </summary>
public class AccountCategory
{
    public int AccountCategoryId { get; set; }
    /// <summary>Null for system-wide seeded categories.</summary>
    public Guid? TenantId { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public bool IsSystem { get; set; }
    public ICollection<AccountSubCategory> SubCategories { get; set; } = [];
}
