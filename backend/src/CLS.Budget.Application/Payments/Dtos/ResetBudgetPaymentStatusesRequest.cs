namespace CLS.Budget.Application.Payments.Dtos;

public sealed class ResetBudgetPaymentStatusesRequest
{
    public int BudgetPaymentStatusId { get; init; }
}

public sealed class ResetBudgetPaymentStatusesResponse
{
    public int UpdatedCount { get; init; }
    public int BudgetPaymentStatusId { get; init; }
}
