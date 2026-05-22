namespace CLS.Budget.Domain;

/// <summary>
/// Fixed ids for seeded <see cref="Entities.BudgetPaymentStatus"/> rows.
/// </summary>
public static class BudgetPaymentStatusIds
{
    public const int Pending = 1;
    public const int Scheduled = 2;
    public const int Paid = 3;
    public const int Failed = 4;
    public const int Overdue = 5;
}
