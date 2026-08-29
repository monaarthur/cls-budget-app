namespace CLS.Budget.Domain.AutoPayoff;

/// <summary>
/// Ranks eligible debts for extra AutoPayoff money using exclude lists,
/// target-first categories, and optional payoff-date ordering.
/// </summary>
public sealed class AutoPayoffEngine
{
    public AutoPayoffPlan BuildPlan(
        IReadOnlyList<AutoPayoffAccountInput> accounts,
        AutoPayoffRules rules,
        decimal extraPayment,
        DateOnly asOf)
    {
        var excludeStatuses = rules.ExcludeStatusIds.ToHashSet();
        var excludeCategories = rules.ExcludeCategoryIds.ToHashSet();
        var excludeAccounts = rules.ExcludeAccountIds.ToHashSet();
        var targetOrder = rules.TargetCategoryIds
            .Select((id, index) => (id, index))
            .Where(x => !excludeCategories.Contains(x.id))
            .GroupBy(x => x.id)
            .Select(g => g.First())
            .ToDictionary(x => x.id, x => x.index);

        var excluded = new List<AutoPayoffExcludedItem>();
        var eligible = new List<(AutoPayoffAccountInput Account, DateOnly? TargetDate)>();

        foreach (var account in accounts)
        {
            var reason = ExclusionReason(
                account,
                excludeStatuses,
                excludeCategories,
                excludeAccounts);
            if (reason is not null)
            {
                excluded.Add(new AutoPayoffExcludedItem(account.AccountId, account.Name, reason));
                continue;
            }

            eligible.Add((account, ResolveTargetDate(account, asOf)));
        }

        var ordered = eligible
            .OrderBy(item =>
                targetOrder.TryGetValue(item.Account.CategoryId, out var priority)
                    ? priority
                    : int.MaxValue)
            .ThenBy(item =>
                rules.TargetByPayoffDate
                    ? item.TargetDate ?? DateOnly.MaxValue
                    : DateOnly.MinValue)
            .ThenBy(item => item.Account.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var extra = Math.Max(0, extraPayment);
        var remainingExtra = extra;
        var queue = new List<AutoPayoffQueueItem>();

        for (var i = 0; i < ordered.Count; i++)
        {
            var (account, targetDate) = ordered[i];
            var monthly = account.MonthlyPayment ?? 0m;
            var remainingBalance = Math.Max(0, account.Balance);
            var extraAllocated = Math.Min(remainingExtra, remainingBalance);
            remainingExtra -= extraAllocated;

            var inTargetCategory = targetOrder.ContainsKey(account.CategoryId);
            var reason = inTargetCategory
                ? "Target category first"
                : rules.TargetByPayoffDate
                    ? "Ordered by payoff / due date"
                    : "Eligible after target categories";

            queue.Add(new AutoPayoffQueueItem(
                Rank: i + 1,
                AccountId: account.AccountId,
                Name: account.Name,
                CategoryId: account.CategoryId,
                CategoryName: account.CategoryName,
                Balance: remainingBalance,
                MonthlyPayment: monthly,
                TargetDate: targetDate,
                Reason: reason,
                ExtraAllocated: extraAllocated));
        }

        return new AutoPayoffPlan(
            queue,
            excluded,
            extra,
            extra - remainingExtra,
            remainingExtra);
    }

    private static string? ExclusionReason(
        AutoPayoffAccountInput account,
        HashSet<int> excludeStatuses,
        HashSet<int> excludeCategories,
        HashSet<int> excludeAccounts)
    {
        if (account.IsPaidOff || account.Balance <= 0)
        {
            return "Paid off or zero balance";
        }

        if (!account.IncludeInPayoffAnalysis)
        {
            return "Excluded from payoff analysis";
        }

        if (excludeAccounts.Contains(account.AccountId))
        {
            return "Account excluded";
        }

        if (excludeCategories.Contains(account.CategoryId))
        {
            return "Category excluded";
        }

        if (account.PaymentStatusId is int statusId && excludeStatuses.Contains(statusId))
        {
            return $"Status excluded ({account.PaymentStatusName ?? statusId.ToString()})";
        }

        return null;
    }

    internal static DateOnly? ResolveTargetDate(AutoPayoffAccountInput account, DateOnly asOf)
    {
        if (account.NextPaymentDate is { } paymentDate)
        {
            return paymentDate;
        }

        if (account.PaymentDay is int day and > 0)
        {
            var clamped = Math.Min(day, DateTime.DaysInMonth(asOf.Year, asOf.Month));
            var thisMonth = new DateOnly(asOf.Year, asOf.Month, clamped);
            if (thisMonth >= asOf)
            {
                return thisMonth;
            }

            var next = asOf.AddMonths(1);
            var nextDay = Math.Min(day, DateTime.DaysInMonth(next.Year, next.Month));
            return new DateOnly(next.Year, next.Month, nextDay);
        }

        var monthly = account.MonthlyPayment ?? 0m;
        if (monthly > 0 && account.Balance > 0)
        {
            var months = (int)Math.Ceiling(account.Balance / monthly);
            return asOf.AddMonths(Math.Max(1, months));
        }

        return null;
    }
}
