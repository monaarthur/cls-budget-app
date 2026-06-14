namespace CLS.Budget.Application.Budgets.Dtos;

public sealed class BudgetResponse
{
    public int BudgetId { get; init; }
    public string Name { get; init; } = null!;
    public DateTime StartPeriod { get; init; }
    public DateTime EndPeriod { get; init; }
    public int BudgetTemplateId { get; init; }
    public string? Notes { get; init; }
    public IReadOnlyList<int> AccountIds { get; init; } = [];
    public int? PayScheduleId { get; init; }
}
