namespace CLS.Budget.Domain.Entities;

/// <summary>
/// Optional subcategory under an <see cref="AccountCategory"/> (tenant-scoped).
/// </summary>
public class AccountSubCategory : ITenantOwned
{
    public int AccountSubCategoryId { get; set; }
    public Guid TenantId { get; set; }
    public int AccountCategoryId { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public AccountCategory AccountCategory { get; set; } = null!;
}
