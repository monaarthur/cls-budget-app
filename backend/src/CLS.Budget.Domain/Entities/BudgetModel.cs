namespace CLS.Budget.Domain.Entities;

public class BudgetModel : ITenantOwned
{
    public int BudgetId { get; set; }
    public Guid TenantId { get; set; }
    public string Name { get; set; } = null!;
    public DateTime StartPeriod { get; set; }
    public DateTime EndPeriod { get; set; }
    public int BudgetTemplateId { get; set; }
    public string? Notes { get; set; }
    public string? AccountIds { get; set; }
    public int? PayScheduleId { get; set; }
    public PaySchedule? PaySchedule { get; set; }
    public BudgetTemplate? BudgetTemplate { get; set; }
    public ICollection<BudgetPayment> BudgetPayments { get; set; } = new List<BudgetPayment>();
}
