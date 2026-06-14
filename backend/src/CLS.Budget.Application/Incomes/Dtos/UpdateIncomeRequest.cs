namespace CLS.Budget.Application.Incomes.Dtos;

public sealed class UpdateIncomeRequest
{
    public int BudgetId { get; init; }
    public int IncomeSourceId { get; init; }
    public decimal Amount { get; init; }
    public DateTime ReceivedDate { get; init; }
    public string? Notes { get; init; }
}
