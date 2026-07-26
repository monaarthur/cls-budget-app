using System.Text.Json.Serialization;
using CLS.Budget.Application.Common.Serialization;

namespace CLS.Budget.Application.CreditCardEngine.Dtos;

public sealed class ActivatePayoffPlanRequest
{
    public int? SavedPayoffPlanId { get; init; }
    public string? Name { get; init; }
    public string? Goal { get; init; }
    public string? Strategy { get; init; }
    public decimal? ExtraMonthlyPayment { get; init; }
    public decimal? TotalMonthlyDebtPayment { get; init; }
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
    public string? Reason { get; init; }

    [JsonConverter(typeof(NullableDateOnlyUtcJsonConverter))]
    public DateTime? StartDate { get; init; }
}

public sealed class ReviseActivePayoffPlanRequest
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
    public string? Reason { get; init; }
}

public sealed class RecordPayoffPlanPaymentRequest
{
    public int AccountId { get; init; }
    public decimal Amount { get; init; }

    [JsonConverter(typeof(NullableDateOnlyUtcJsonConverter))]
    public DateTime? PaymentDate { get; init; }

    public string? Notes { get; init; }
}

public sealed class ActivePayoffPlanDto
{
    public int ActivePayoffPlanId { get; init; }
    public string Name { get; init; } = null!;
    public string Status { get; init; } = null!;
    public int? SourceSavedPayoffPlanId { get; init; }
    public DateTime StartedOnUtc { get; init; }
    public DateTime? EndedOnUtc { get; init; }
    public int CurrentVersionNumber { get; init; }
    public decimal StartingDebt { get; init; }
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
    public ActivePayoffPlanProgressDto Progress { get; init; } = null!;
}

public sealed class ActivePayoffPlanProgressDto
{
    public decimal StartingDebt { get; init; }
    public decimal CurrentDebt { get; init; }
    public decimal PaidToDate { get; init; }
    public decimal DebtReduced { get; init; }
    public int ProjectedMonthsRemaining { get; init; }
    public decimal ProjectedRemainingInterest { get; init; }
    public DateOnly? ProjectedPayoffDate { get; init; }
    public bool ProjectionIsValid { get; init; }
    public decimal PlannedMonthlyPayment { get; init; }
    public decimal AverageMonthlyPaid { get; init; }
    public string? AdherenceNote { get; init; }
    public IReadOnlyList<string> Warnings { get; init; } = [];
}

public sealed class PayoffPlanVersionDto
{
    public int PayoffPlanVersionId { get; init; }
    public int VersionNumber { get; init; }
    public string? Reason { get; init; }
    public DateTime CreatedOnUtc { get; init; }
    public string Strategy { get; init; } = null!;
    public decimal TotalMonthlyDebtPayment { get; init; }
    public decimal SnapshotDebt { get; init; }
    public int ProjectedMonthsToPayoff { get; init; }
    public decimal ProjectedTotalInterest { get; init; }
    public DateOnly? ProjectedPayoffDate { get; init; }
    public bool ProjectionIsValid { get; init; }
}

public sealed class PayoffPlanPaymentDto
{
    public int PayoffPlanPaymentId { get; init; }
    public int AccountId { get; init; }
    public string? AccountName { get; init; }
    public decimal Amount { get; init; }
    public DateOnly PaymentDate { get; init; }
    public string? Notes { get; init; }
    public int PayoffPlanVersionId { get; init; }
    public int VersionNumber { get; init; }
    public bool IsVoided { get; init; }
    public DateTime? VoidedOnUtc { get; init; }
    public DateTime CreatedOnUtc { get; init; }
}

public sealed class PayoffPlanEventDto
{
    public int PayoffPlanEventId { get; init; }
    public string EventType { get; init; } = null!;
    public string Summary { get; init; } = null!;
    public string? PayloadJson { get; init; }
    public DateTime CreatedOnUtc { get; init; }
}

public sealed class ActivePayoffPlanHistoryDto
{
    public int ActivePayoffPlanId { get; init; }
    public string Name { get; init; } = null!;
    public string Status { get; init; } = null!;
    public IReadOnlyList<PayoffPlanVersionDto> Versions { get; init; } = [];
    public IReadOnlyList<PayoffPlanPaymentDto> Payments { get; init; } = [];
    public IReadOnlyList<PayoffPlanEventDto> Events { get; init; } = [];
}
