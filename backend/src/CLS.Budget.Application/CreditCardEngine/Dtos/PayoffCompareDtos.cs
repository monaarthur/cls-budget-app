using System.Text.Json.Serialization;
using CLS.Budget.Application.Common.Serialization;
using CLS.Budget.Domain.CreditCardEngine.Payoff;

namespace CLS.Budget.Application.CreditCardEngine.Dtos;

public sealed class ComparePayoffPlansRequest
{
    public decimal TotalMonthlyDebtPayment { get; init; }

    [JsonConverter(typeof(NullableDateOnlyUtcJsonConverter))]
    public DateTime? StartDate { get; init; }

    /// <summary>
    /// Optional (1–99). Applied to both Avalanche and Snowball: first hit this
    /// utilization per card, then finish paying to zero.
    /// </summary>
    public decimal? TargetUtilizationPercent { get; init; }

    /// <summary>
    /// When true, Avalanche/Snowball first pay any balance above the credit limit
    /// (to help avoid over-limit fees), then continue with utilization / strategy payoff.
    /// </summary>
    public bool PayOverLimitFirst { get; init; }

    /// <summary>
    /// When true, after payments use another card's available credit to help pay the
    /// focus card when that card's cash-advance APR is lower; otherwise use the
    /// highest-APR card with available credit.
    /// </summary>
    public bool EnableCashAdvanceBalanceMoves { get; init; }

    /// <summary>
    /// Optional promotional APR balance transfers applied at scheduled month offsets
    /// during Avalanche/Snowball plans (multi-entry).
    /// </summary>
    public IReadOnlyList<PromotionalBalanceTransferDto>? PromotionalTransfers { get; init; }

    /// <summary>
    /// Optional: "Avalanche" or "Snowball" for payoff order after utilization target is met.
    /// When omitted, each plan keeps its original strategy for phase 2.
    /// </summary>
    public string? PostUtilizationStrategy { get; init; }

    /// <summary>Optional loan principal applied to cards first, then repaid in the plan.</summary>
    public decimal? LoanAmount { get; init; }

    /// <summary>APR percent for the optional loan.</summary>
    public decimal? LoanAnnualPercentageRate { get; init; }

    /// <summary>Avalanche, Snowball, or SelectedAccounts for applying loan proceeds to cards.</summary>
    public string? LoanApplyStrategy { get; init; }

    /// <summary>Credit card account ids when LoanApplyStrategy is SelectedAccounts (apply order).</summary>
    public IReadOnlyList<int>? LoanApplyCreditCardIds { get; init; }

    /// <summary>Personal, HomeEquity, Heloc, Retirement401k, Family.</summary>
    public string? LoanType { get; init; }

    public int? LoanTermMonths { get; init; }

    public int? LoanInterestOnlyMonths { get; init; }

    public decimal? LoanFixedMonthlyPayment { get; init; }
}

public sealed class PromotionalBalanceTransferDto
{
    public int FromCreditCardId { get; init; }
    public int ToCreditCardId { get; init; }
    /// <summary>When null or omitted, transfer as much as source balance and destination room allow.</summary>
    public decimal? Amount { get; init; }
    public decimal PromotionalAnnualPercentageRate { get; init; }
    public int PromotionalPeriodMonths { get; init; }
    /// <summary>0 = first forecast month.</summary>
    public int ApplyAtMonthOffset { get; init; }
}

public sealed class PayoffStrategySummaryDto
{
    public string Strategy { get; init; } = null!;
    public DateOnly? EstimatedPayoffDate { get; init; }
    public decimal TotalInterest { get; init; }
    public int MonthsToPayoff { get; init; }
    public decimal CombinedMinimumPayments { get; init; }
    public bool IsValid { get; init; }
    public IReadOnlyList<string> Warnings { get; init; } = [];
    public IReadOnlyList<CardPayoffOrderDto> CardOrder { get; init; } = [];
}

public sealed class BalanceTransferLegDto
{
    public int CounterpartyCreditCardId { get; init; }
    public string CounterpartyName { get; init; } = null!;
    public decimal Amount { get; init; }
    public string Direction { get; init; } = null!;
}

public sealed class CardMonthlyBalanceDto
{
    public DateOnly Month { get; init; }
    public decimal StartingBalance { get; init; }
    public decimal InterestCharged { get; init; }
    public decimal PaymentApplied { get; init; }
    public decimal MinimumPaymentApplied { get; init; }
    public decimal ExtraPaymentApplied { get; init; }
    public decimal PrincipalApplied { get; init; }
    public decimal BalanceTransferredIn { get; init; }
    public decimal BalanceTransferredOut { get; init; }
    public IReadOnlyList<BalanceTransferLegDto> Transfers { get; init; } = [];
    public decimal EndingBalance { get; init; }
}

public sealed class CardPayoffOrderDto
{
    public int CreditCardId { get; init; }
    public string Name { get; init; } = null!;
    public int PriorityOrder { get; init; }
    public DateOnly? EstimatedPayoffDate { get; init; }
    public decimal TotalInterestPaid { get; init; }
    public IReadOnlyList<CardMonthlyBalanceDto> MonthlyBalances { get; init; } = [];
}

public sealed class ComparePayoffPlansResultDto
{
    public decimal StartingDebt { get; init; }
    public decimal MonthlyPayment { get; init; }
    public IReadOnlyList<PayoffStrategySummaryDto> Strategies { get; init; } = [];
    public string? RecommendedStrategy { get; init; }
    public string? Reason { get; init; }
}

public sealed class InterestAnalysisRequest
{
    public decimal? MonthlyPayment { get; init; }

    [JsonConverter(typeof(NullableDateOnlyUtcJsonConverter))]
    public DateTime? StartDate { get; init; }
}

public sealed class InterestAnalysisResultDto
{
    public int CreditCardId { get; init; }
    public string Name { get; init; } = null!;
    public decimal DailyInterest { get; init; }
    public decimal EstimatedMonthlyInterest { get; init; }
    public decimal EstimatedAnnualInterest { get; init; }
    public decimal TotalInterestPaid { get; init; }
    public decimal TotalPrincipalPaid { get; init; }
    public decimal RemainingBalance { get; init; }
    public DateOnly? EstimatedPayoffDate { get; init; }
    public int NumberOfPayments { get; init; }
    public bool NegativeAmortizationDetected { get; init; }
}

public sealed class UtilizationSummaryResultDto
{
    public decimal TotalBalances { get; init; }
    public decimal TotalCreditLimits { get; init; }
    public decimal OverallUtilizationPercentage { get; init; }
    public IReadOnlyList<CardUtilizationDto> Cards { get; init; } = [];
    public IReadOnlyList<UtilizationThresholdDto> OverallThresholdTargets { get; init; } = [];
}

public sealed class CardUtilizationDto
{
    public int CreditCardId { get; init; }
    public string Name { get; init; } = null!;
    public decimal CurrentBalance { get; init; }
    public decimal CreditLimit { get; init; }
    public decimal AvailableCredit { get; init; }
    public decimal UtilizationPercentage { get; init; }
    public IReadOnlyList<UtilizationThresholdDto> ThresholdTargets { get; init; } = [];
}

public sealed class UtilizationThresholdDto
{
    public decimal ThresholdPercent { get; init; }
    public decimal TargetBalance { get; init; }
    public decimal PaymentRequired { get; init; }
}

internal static class PayoffStrategyNames
{
    public static string ToDisplayName(PayoffStrategyType strategy) => strategy switch
    {
        PayoffStrategyType.Avalanche => "Avalanche",
        PayoffStrategyType.Snowball => "Snowball",
        PayoffStrategyType.MinimumsOnly => "MinimumsOnly",
        _ => strategy.ToString()
    };
}
