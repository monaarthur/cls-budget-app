namespace CLS.Budget.Application.Budgets;

public static class BudgetPaymentDateMapper
{
    /// <summary>
    /// Keeps the source payment's day-of-month, applied to the new budget period
    /// (clamped when the day does not exist in the target month).
    /// </summary>
    public static DateTime MapToBudgetPeriod(DateTime sourcePaymentDate, DateTime budgetStartPeriod)
    {
        var sourceDay = DateTime.SpecifyKind(sourcePaymentDate.ToUniversalTime().Date, DateTimeKind.Utc);
        var periodStart = DateTime.SpecifyKind(budgetStartPeriod.ToUniversalTime().Date, DateTimeKind.Utc);
        var lastDay = DateTime.DaysInMonth(periodStart.Year, periodStart.Month);
        var day = Math.Min(sourceDay.Day, lastDay);
        return new DateTime(periodStart.Year, periodStart.Month, day, 0, 0, 0, DateTimeKind.Utc);
    }
}
