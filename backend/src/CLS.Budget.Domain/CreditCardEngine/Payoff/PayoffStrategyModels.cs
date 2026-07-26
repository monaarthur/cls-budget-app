using CLS.Budget.Domain.CreditCardEngine.Loan;

namespace CLS.Budget.Domain.CreditCardEngine.Payoff;

public enum PayoffStrategyType
{
    Avalanche = 1,
    Snowball = 2,
    MinimumsOnly = 3
}

public sealed record CreditCardPayoffInput(
    int CreditCardId,
    string Name,
    decimal CurrentBalance,
    decimal CreditLimit,
    decimal AnnualPercentageRate,
    decimal? FixedMonthlyPayment,
    decimal? MinimumPaymentPercentage,
    decimal? MinimumPaymentFloor,
    decimal? PromotionalAnnualPercentageRate,
    DateOnly? PromotionalRateExpirationDate,
    /// <summary>Optional cash advance APR percent used for cash-advance balance transfers.</summary>
    decimal? CashAdvanceInterestRate = null,
    /// <summary>Optional cash advance fee percent of the advance amount (e.g. 5 = 5%).</summary>
    decimal? CashAdvanceFeePercentage = null);

/// <summary>
/// A planned promotional balance transfer applied during the payoff simulation.
/// Month offset 0 applies in the first forecast month.
/// </summary>
public sealed record PromotionalBalanceTransferPlan(
    int FromCreditCardId,
    int ToCreditCardId,
    /// <summary>When null, transfer as much as fits (source balance and destination available credit).</summary>
    decimal? Amount,
    decimal PromotionalAnnualPercentageRate,
    int PromotionalPeriodMonths,
    int ApplyAtMonthOffset = 0);

public sealed record PayoffPlanRequest(
    IReadOnlyCollection<CreditCardPayoffInput> CreditCards,
    decimal TotalMonthlyDebtPayment,
    PayoffStrategyType Strategy,
    DateOnly StartDate,
    /// <summary>
    /// When set (1–99), Avalanche/Snowball first bring each card to this utilization
    /// using strategy order, then finish paying balances to zero.
    /// </summary>
    decimal? TargetUtilizationPercent = null,
    /// <summary>
    /// When true, extra payments first bring any over-limit balances back to the credit
    /// limit (to avoid over-limit fees) before utilization / strategy payoff.
    /// </summary>
    bool PayOverLimitFirst = false,
    /// <summary>
    /// When true, after payments use another card's available credit to help pay the
    /// focus card when that card's cash-advance APR is lower; otherwise fall back to
    /// the highest-APR card with available credit.
    /// </summary>
    bool EnableCashAdvanceBalanceMoves = false,
    IReadOnlyList<PromotionalBalanceTransferPlan>? PromotionalTransfers = null,
    /// <summary>
    /// Optional strategy used after every card meets the utilization target.
    /// When null, the original Strategy continues for payoff to zero.
    /// </summary>
    PayoffStrategyType? PostUtilizationStrategy = null,
    /// <summary>
    /// Optional loan principal applied to card balances first, then included as a
    /// separate debt in the payoff plan at <see cref="LoanAnnualPercentageRate"/>.
    /// Application order uses <see cref="LoanApplyStrategy"/> (Avalanche/Snowball).
    /// </summary>
    decimal? LoanAmount = null,
    /// <summary>APR percent for the optional loan (e.g. 9.99 = 9.99%).</summary>
    decimal? LoanAnnualPercentageRate = null,
    /// <summary>
    /// Avalanche or Snowball order for applying loan proceeds to cards.
    /// Ignored when <see cref="LoanApplyCreditCardIds"/> is non-empty.
    /// When null, falls back to the plan Strategy, or Avalanche for MinimumsOnly.
    /// </summary>
    PayoffStrategyType? LoanApplyStrategy = null,
    /// <summary>Optional loan product type for repayment rules.</summary>
    LoanType? LoanType = null,
    /// <summary>Term in months (personal, home equity, 401k, HELOC total term).</summary>
    int? LoanTermMonths = null,
    /// <summary>HELOC interest-only draw period months.</summary>
    int? LoanInterestOnlyMonths = null,
    /// <summary>Required for family loans; optional override for others.</summary>
    decimal? LoanFixedMonthlyPayment = null,
    /// <summary>
    /// When non-empty, apply loan proceeds only to these credit cards, in list order.
    /// </summary>
    IReadOnlyList<int>? LoanApplyCreditCardIds = null);

public sealed record BalanceTransferLeg(
    int CounterpartyCreditCardId,
    string CounterpartyName,
    decimal Amount,
    /// <summary>"In" means balance transferred onto this card; "Out" means transferred away.</summary>
    string Direction);

public sealed record MonthlyPayoffScheduleItem(
    DateOnly Month,
    int CreditCardId,
    string CreditCardName,
    decimal StartingBalance,
    decimal InterestCharged,
    decimal PaymentApplied,
    decimal MinimumPaymentApplied,
    decimal ExtraPaymentApplied,
    decimal PrincipalApplied,
    decimal BalanceTransferredIn,
    decimal BalanceTransferredOut,
    IReadOnlyList<BalanceTransferLeg> Transfers,
    decimal EndingBalance);

public sealed record CardPayoffSummary(
    int CreditCardId,
    string Name,
    int PriorityOrder,
    DateOnly? EstimatedPayoffDate,
    decimal TotalInterestPaid,
    int MonthsToPayoff);

public sealed record PayoffPlanResult(
    PayoffStrategyType Strategy,
    decimal StartingDebt,
    decimal TotalMonthlyDebtPayment,
    decimal CombinedMinimumPayments,
    DateOnly? OverallDebtFreeDate,
    int MonthsToPayoff,
    decimal TotalInterestPaid,
    decimal TotalPrincipalPaid,
    decimal? InterestSavedVersusMinimums,
    IReadOnlyList<CardPayoffSummary> CardOrder,
    IReadOnlyList<MonthlyPayoffScheduleItem> Schedule,
    IReadOnlyList<string> Warnings,
    bool IsValid);
