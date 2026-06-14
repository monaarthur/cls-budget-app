namespace CLS.Budget.Domain.Entities;

public class IncomeSource
{
    public int IncomeSourceId { get; set; }
    public string Name { get; set; } = null!;
    public bool IsActive { get; set; } = true;
}
