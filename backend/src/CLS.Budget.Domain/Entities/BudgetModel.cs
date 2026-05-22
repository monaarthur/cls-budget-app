namespace CLS.Budget.Domain.Entities;

public class BudgetModel
{
    public int BudgetId { get; set; }
    public string Name { get; set; } = null!;
    public DateTime StartPeriod { get; set; }
    public DateTime EndPeriod { get; set; }
    public int BudgetTemplateId { get; set; }
    public string? AccountIds { get; set; }
    public BudgetTemplate? BudgetTemplate { get; set; }
    public ICollection<BudgetPayment> BudgetPayments { get; set; } = new List<BudgetPayment>();
}
