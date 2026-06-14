namespace CLS.Budget.Application.Incomes.Dtos;

public sealed class IncomeResponse
{
    public int BudgetIncomeId { get; init; }
    public int BudgetId { get; init; }
    public int IncomeSourceId { get; init; }
    public string IncomeSourceName { get; init; } = null!;
    public decimal Amount { get; init; }
    public DateTime ReceivedDate { get; init; }
    public string? Notes { get; init; }
}
