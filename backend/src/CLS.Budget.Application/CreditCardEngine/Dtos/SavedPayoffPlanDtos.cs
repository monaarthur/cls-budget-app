using System.Text.Json.Serialization;
using CLS.Budget.Application.Common.Serialization;

namespace CLS.Budget.Application.CreditCardEngine.Dtos;

public sealed class SavedPayoffPlanDto
{
    public int SavedPayoffPlanId { get; init; }
    public string Name { get; init; } = null!;
    public string? Goal { get; init; }
    public string Strategy { get; init; } = null!;
    public decimal ExtraMonthlyPayment { get; init; }
    public decimal TotalMonthlyDebtPayment { get; init; }
    public decimal? TargetUtilizationPercent { get; init; }
    public bool PayOverLimitFirst { get; init; }
    public string? PostUtilizationStrategy { get; init; }
    public bool EnableCashAdvanceBalanceMoves { get; init; }
    public decimal? LoanAmount { get; init; }
    public decimal? LoanAnnualPercentageRate { get; init; }
    public string? LoanApplyStrategy { get; init; }
    public IReadOnlyList<int> LoanApplyCreditCardIds { get; init; } = [];
    public string? LoanType { get; init; }
    public int? LoanTermMonths { get; init; }
    public int? LoanInterestOnlyMonths { get; init; }
    public decimal? LoanFixedMonthlyPayment { get; init; }
    public IReadOnlyList<PromotionalBalanceTransferDto> PromotionalTransfers { get; init; } = [];
    public DateTime CreatedOnUtc { get; init; }
    public DateTime UpdatedOnUtc { get; init; }
}

public sealed class SavePayoffPlanRequest
{
    public string Name { get; init; } = null!;
    /// <summary>improveCredit, lowerUtilization, minimizeInterest, or null.</summary>
    public string? Goal { get; init; }
    /// <summary>Avalanche, Snowball, or MinimumsOnly.</summary>
    public string Strategy { get; init; } = null!;
    public decimal ExtraMonthlyPayment { get; init; }
    public decimal TotalMonthlyDebtPayment { get; init; }
    public decimal? TargetUtilizationPercent { get; init; }
    public bool PayOverLimitFirst { get; init; }
    public string? PostUtilizationStrategy { get; init; }
    public bool EnableCashAdvanceBalanceMoves { get; init; }
    public decimal? LoanAmount { get; init; }
    public decimal? LoanAnnualPercentageRate { get; init; }
    /// <summary>Avalanche or Snowball when a loan amount is set.</summary>
    public string? LoanApplyStrategy { get; init; }
    public IReadOnlyList<int> LoanApplyCreditCardIds { get; init; } = [];
    public string? LoanType { get; init; }
    public int? LoanTermMonths { get; init; }
    public int? LoanInterestOnlyMonths { get; init; }
    public decimal? LoanFixedMonthlyPayment { get; init; }
    public IReadOnlyList<PromotionalBalanceTransferDto>? PromotionalTransfers { get; init; }

    [JsonConverter(typeof(NullableDateOnlyUtcJsonConverter))]
    public DateTime? StartDate { get; init; }
}

public sealed class UpdateSavedPayoffPlanRequest
{
    public string Name { get; init; } = null!;
    public string? Goal { get; init; }
    public string Strategy { get; init; } = null!;
    public decimal ExtraMonthlyPayment { get; init; }
    public decimal TotalMonthlyDebtPayment { get; init; }
    public decimal? TargetUtilizationPercent { get; init; }
    public bool PayOverLimitFirst { get; init; }
    public string? PostUtilizationStrategy { get; init; }
    public bool EnableCashAdvanceBalanceMoves { get; init; }
    public decimal? LoanAmount { get; init; }
    public decimal? LoanAnnualPercentageRate { get; init; }
    public string? LoanApplyStrategy { get; init; }
    public IReadOnlyList<int> LoanApplyCreditCardIds { get; init; } = [];
    public string? LoanType { get; init; }
    public int? LoanTermMonths { get; init; }
    public int? LoanInterestOnlyMonths { get; init; }
    public decimal? LoanFixedMonthlyPayment { get; init; }
    public IReadOnlyList<PromotionalBalanceTransferDto>? PromotionalTransfers { get; init; }
}

public sealed class CompareSavedPayoffPlansRequest
{
    public IReadOnlyList<int> PlanIds { get; init; } = [];

    [JsonConverter(typeof(NullableDateOnlyUtcJsonConverter))]
    public DateTime? StartDate { get; init; }
}

public sealed class CompareSavedPayoffPlansResultDto
{
    public IReadOnlyList<SavedPayoffPlanCompareItemDto> Plans { get; init; } = [];
}

public sealed class SavedPayoffPlanCompareItemDto
{
    public int SavedPayoffPlanId { get; init; }
    public string Name { get; init; } = null!;
    public PayoffStrategySummaryDto StrategySummary { get; init; } = null!;
}
