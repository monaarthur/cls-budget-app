using System.Text.Json.Serialization;
using CLS.Budget.Application.Common.Serialization;

namespace CLS.Budget.Application.CreditCardEngine.Dtos;

public sealed class CompareLoanSavingsRequest
{
    /// <summary>Current monthly card payment budget (minimums + extra), before loan payment.</summary>
    public decimal TotalMonthlyDebtPayment { get; init; }

    /// <summary>Avalanche, Snowball, or MinimumsOnly. Defaults to Avalanche.</summary>
    public string? Strategy { get; init; }

    [JsonConverter(typeof(NullableDateOnlyUtcJsonConverter))]
    public DateTime? StartDate { get; init; }

    public decimal? TargetUtilizationPercent { get; init; }
    public bool PayOverLimitFirst { get; init; }
    public bool EnableCashAdvanceBalanceMoves { get; init; }
    public IReadOnlyList<PromotionalBalanceTransferDto>? PromotionalTransfers { get; init; }
    public string? PostUtilizationStrategy { get; init; }

    public decimal LoanAmount { get; init; }
    public decimal LoanAnnualPercentageRate { get; init; }
    public string? LoanApplyStrategy { get; init; }
    public IReadOnlyList<int>? LoanApplyCreditCardIds { get; init; }
    public string LoanType { get; init; } = null!;
    public int? LoanTermMonths { get; init; }
    public int? LoanInterestOnlyMonths { get; init; }
    public decimal? LoanFixedMonthlyPayment { get; init; }
}

public sealed class LoanSavingsScenarioDto
{
    public string Label { get; init; } = null!;
    public string Strategy { get; init; } = null!;
    public decimal TotalInterest { get; init; }
    public decimal TotalPrincipalPaid { get; init; }
    public decimal TotalPaid { get; init; }
    public int MonthsToPayoff { get; init; }
    public DateOnly? EstimatedPayoffDate { get; init; }
    public bool IsValid { get; init; }
    public IReadOnlyList<string> Warnings { get; init; } = [];
}

public sealed class CompareLoanSavingsResultDto
{
    public LoanSavingsScenarioDto WithoutLoan { get; init; } = null!;
    public LoanSavingsScenarioDto WithLoan { get; init; } = null!;
    /// <summary>Positive means the loan path pays less interest.</summary>
    public decimal InterestSaved { get; init; }
    /// <summary>Positive means the loan path finishes sooner.</summary>
    public int MonthsSaved { get; init; }
    /// <summary>Positive means the loan path costs less overall (principal + interest paid in plan).</summary>
    public decimal TotalPaidSaved { get; init; }
    public string Summary { get; init; } = null!;
}
