namespace CLS.Budget.Domain.Entities;

public class BudgetPaymentStatus
{
    public int BudgetPaymentStatusId { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
}
