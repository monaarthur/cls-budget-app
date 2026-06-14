namespace CLS.Budget.Application.Incomes.Dtos;

public sealed class IncomeSummaryResponse
{
    public int BudgetId { get; init; }
    public decimal Total { get; init; }
    public IReadOnlyList<IncomeSummaryItem> Items { get; init; } = [];
}

public sealed class IncomeSummaryItem
{
    public int IncomeSourceId { get; init; }
    public string IncomeSourceName { get; init; } = null!;
    public decimal Total { get; init; }
}
