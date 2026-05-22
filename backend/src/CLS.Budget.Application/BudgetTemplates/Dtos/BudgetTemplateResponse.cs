namespace CLS.Budget.Application.BudgetTemplates.Dtos;

public sealed class BudgetTemplateResponse
{
    public int BudgetTemplateId { get; init; }
    public string Name { get; init; } = null!;
    public string? Description { get; init; }
    public IReadOnlyList<int> AccountIds { get; init; } = [];
}
