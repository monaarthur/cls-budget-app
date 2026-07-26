namespace CLS.Budget.Application.CreditCardEngine.Dtos;

public sealed class AnalyzeCashFlowRequest
{
    public decimal MonthlyNetIncome { get; init; }
    public decimal RequiredExpenses { get; init; }
    public decimal VariableExpenses { get; init; }

    /// <summary>
    /// When null, the service sums minimum payments from active credit cards.
    /// </summary>
    public decimal? ExistingDebtMinimums { get; init; }

    public decimal EmergencySavingsContribution { get; init; }
    public decimal SafetyBuffer { get; init; }
    public decimal AdditionalAvailableFunds { get; init; }

    /// <summary>
    /// Optional override for the recommended extra debt payment.
    /// </summary>
    public decimal? UserOverrideExtraPayment { get; init; }
}

public sealed class CashFlowAnalysisResultDto
{
    public decimal MonthlyDisposableIncome { get; init; }
    public decimal RequiredDebtMinimums { get; init; }
    public decimal SafeExtraDebtPayment { get; init; }
    public decimal AggressiveExtraDebtPayment { get; init; }
    public decimal RemainingCashBuffer { get; init; }
    public decimal RecommendedExtraDebtPayment { get; init; }
    public bool UsedUserOverride { get; init; }
    public decimal SuggestedTotalMonthlyDebtPayment { get; init; }
}
