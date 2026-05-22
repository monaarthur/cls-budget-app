namespace CLS.Budget.Application.BudgetPaymentStatuses.Dtos;

public sealed class BudgetPaymentStatusResponse
{
    public int BudgetPaymentStatusId { get; init; }
    public string Name { get; init; } = null!;
    public string? Description { get; init; }
}
