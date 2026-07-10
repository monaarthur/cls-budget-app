using CLS.Budget.Domain;

namespace CLS.Budget.Application.TransactionImports;

internal static class PostingStatusMapper
{
    public static int MapStatusId(string? postingStatus) =>
        postingStatus?.Trim().ToLowerInvariant() switch
        {
            "posted" => BudgetPaymentStatusIds.Paid,
            "pending" => BudgetPaymentStatusIds.Pending,
            _ => BudgetPaymentStatusIds.Unassigned
        };
}
