using CLS.Budget.Domain;

namespace CLS.Budget.Import.MayBudgets;

internal static class BudgetPaymentStatusMapper
{
    public static int MapStatusId(string? status) =>
        status?.Trim().ToLowerInvariant() switch
        {
            "paid" => BudgetPaymentStatusIds.Paid,
            "scheduled" => BudgetPaymentStatusIds.Scheduled,
            "failed" => BudgetPaymentStatusIds.Failed,
            "overdue" => BudgetPaymentStatusIds.Overdue,
            "planned" or "hold" or "" or null => BudgetPaymentStatusIds.Pending,
            _ => BudgetPaymentStatusIds.Pending
        };

    public static bool IsCleared(string? status, DateTime? datePaid) =>
        MapStatusId(status) == BudgetPaymentStatusIds.Paid && datePaid.HasValue;
}
