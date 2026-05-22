namespace CLS.Budget.Domain.Entities;

public class BudgetTemplate
{
    public int BudgetTemplateId { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string? AccountIds { get; set; }
}
