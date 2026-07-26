namespace CLS.Budget.Domain.CreditCardEngine.CashFlow;

public sealed record CashFlowRequest(
    decimal MonthlyNetIncome,
    decimal RequiredExpenses,
    decimal VariableExpenses,
    decimal ExistingDebtMinimums,
    decimal EmergencySavingsContribution,
    decimal SafetyBuffer,
    decimal AdditionalAvailableFunds = 0m,
    decimal? UserOverrideExtraPayment = null);

public sealed record CashFlowResult(
    decimal MonthlyDisposableIncome,
    decimal RequiredDebtMinimums,
    decimal SafeExtraDebtPayment,
    decimal AggressiveExtraDebtPayment,
    decimal RemainingCashBuffer,
    decimal RecommendedExtraDebtPayment,
    bool UsedUserOverride,
    IReadOnlyList<string> Warnings,
    bool IsValid);
