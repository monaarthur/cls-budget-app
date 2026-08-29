namespace CLS.Budget.Domain.AutoPayoff;

public sealed record AutoPayoffRules(
    IReadOnlyList<int> ExcludeStatusIds,
    IReadOnlyList<int> ExcludeCategoryIds,
    IReadOnlyList<int> ExcludeAccountIds,
    IReadOnlyList<int> TargetCategoryIds,
    bool TargetByPayoffDate);

public sealed record AutoPayoffAccountInput(
    int AccountId,
    string Name,
    int CategoryId,
    string CategoryName,
    decimal Balance,
    decimal? MonthlyPayment,
    int? PaymentDay,
    DateOnly? NextPaymentDate,
    int? PaymentStatusId,
    string? PaymentStatusName,
    bool IsPaidOff,
    bool IncludeInPayoffAnalysis);

public sealed record AutoPayoffQueueItem(
    int Rank,
    int AccountId,
    string Name,
    int CategoryId,
    string CategoryName,
    decimal Balance,
    decimal MonthlyPayment,
    DateOnly? TargetDate,
    string Reason,
    decimal ExtraAllocated);

public sealed record AutoPayoffExcludedItem(
    int AccountId,
    string Name,
    string Reason);

public sealed record AutoPayoffPlan(
    IReadOnlyList<AutoPayoffQueueItem> Queue,
    IReadOnlyList<AutoPayoffExcludedItem> Excluded,
    decimal ExtraPayment,
    decimal ExtraAllocated,
    decimal ExtraRemaining);
