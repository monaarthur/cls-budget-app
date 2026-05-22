namespace CLS.Budget.Import.MayBudgets;

internal sealed class MayBudgetImportOptions
{
    public int Month { get; init; } = 5;
    public int Year { get; init; } = 2026;
    public int BudgetTemplateId { get; init; } = 1;
    public bool DryRun { get; init; }
    public bool ActiveOnly { get; init; }
}
