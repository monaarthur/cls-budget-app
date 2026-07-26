using CLS.Budget.Domain.CreditCardEngine.Payoff;

namespace CLS.Budget.Domain.CreditCardEngine.Forecast;

public sealed record ForecastCharge(
    int MonthOffset,
    decimal Amount,
    int? CreditCardId = null);

public sealed record ForecastOneTimePayment(
    int MonthOffset,
    decimal Amount);

public sealed record ForecastPaymentOverride(
    int MonthOffset,
    decimal TotalMonthlyDebtPayment);

public sealed record ForecastIncomeChange(
    int MonthOffset,
    decimal MonthlyNetIncomeDelta);

public sealed record ForecastExpenseChange(
    int MonthOffset,
    decimal MonthlyExpenseDelta);

public sealed record ForecastRequest(
    IReadOnlyCollection<CreditCardPayoffInput> CreditCards,
    PayoffStrategyType Strategy,
    decimal TotalMonthlyDebtPayment,
    DateOnly StartDate,
    int ForecastMonths,
    decimal MonthlyNetIncome = 0m,
    decimal MonthlyExpenses = 0m,
    decimal? TargetUtilizationPercent = null,
    bool PayOverLimitFirst = false,
    IReadOnlyList<ForecastCharge>? AdditionalCharges = null,
    IReadOnlyList<ForecastOneTimePayment>? OneTimePayments = null,
    IReadOnlyList<ForecastPaymentOverride>? PaymentOverrides = null,
    IReadOnlyList<ForecastIncomeChange>? IncomeChanges = null,
    IReadOnlyList<ForecastExpenseChange>? ExpenseChanges = null);

public sealed record ForecastMonthSnapshot(
    DateOnly Month,
    int MonthIndex,
    decimal StartingDebt,
    decimal NewCharges,
    decimal Interest,
    decimal Payments,
    decimal EndingDebt,
    decimal TotalCreditLimit,
    decimal OverallUtilizationPercentage,
    decimal AvailableCash,
    int CardsPaidOffThisMonth,
    decimal CumulativeInterest);

public sealed record ForecastResult(
    PayoffStrategyType Strategy,
    decimal StartingDebt,
    decimal TotalMonthlyDebtPayment,
    int ForecastMonths,
    DateOnly? EstimatedDebtFreeDate,
    decimal TotalInterestPaid,
    IReadOnlyList<ForecastMonthSnapshot> Months,
    IReadOnlyList<string> Warnings,
    bool IsValid);
