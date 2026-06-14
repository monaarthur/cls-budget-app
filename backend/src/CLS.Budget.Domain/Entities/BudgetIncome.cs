namespace CLS.Budget.Domain.Entities;

/// <summary>
/// A dated income line item received within a monthly <see cref="BudgetModel"/>,
/// categorized by an <see cref="IncomeSource"/> (Credit Cards, Job Income, Business Income).
/// </summary>
public class BudgetIncome : ITenantOwned
{
    public int BudgetIncomeId { get; set; }
    public Guid TenantId { get; set; }
    public int BudgetId { get; set; }
    public int IncomeSourceId { get; set; }
    public IncomeSource? IncomeSource { get; set; }
    public decimal Amount { get; set; }
    public DateTime ReceivedDate { get; set; }
    public string? Notes { get; set; }
}
