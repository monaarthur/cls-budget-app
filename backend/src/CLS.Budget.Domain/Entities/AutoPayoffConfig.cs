namespace CLS.Budget.Domain.Entities;

/// <summary>
/// Tenant AutoPayoff rules: which bills to skip, which categories to hit first,
/// and whether remaining items are ordered by payoff/due date.
/// </summary>
public class AutoPayoffConfig : ITenantOwned
{
    public int AutoPayoffConfigId { get; set; }
    public Guid TenantId { get; set; }
    public string Name { get; set; } = "";
    /// <summary>JSON array of BudgetPaymentStatus ids to skip.</summary>
    public string ExcludeStatusIdsJson { get; set; } = "[]";
    /// <summary>JSON array of AccountCategory ids to skip.</summary>
    public string ExcludeCategoryIdsJson { get; set; } = "[]";
    /// <summary>JSON array of Account ids to skip.</summary>
    public string ExcludeAccountIdsJson { get; set; } = "[]";
    /// <summary>JSON array of AccountCategory ids in priority order (first = highest).</summary>
    public string TargetCategoryIdsJson { get; set; } = "[]";
    public bool TargetByPayoffDate { get; set; }
    public decimal ExtraMonthlyAmount { get; set; }
    public DateTime CreatedOnUtc { get; set; }
    public DateTime UpdatedOnUtc { get; set; }
}
