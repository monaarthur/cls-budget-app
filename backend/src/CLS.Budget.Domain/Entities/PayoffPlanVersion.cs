namespace CLS.Budget.Domain.Entities;

/// <summary>
/// Frozen snapshot of active payoff plan configuration at start or revise.
/// </summary>
public class PayoffPlanVersion : ITenantOwned
{
    public int PayoffPlanVersionId { get; set; }
    public Guid TenantId { get; set; }
    public int ActivePayoffPlanId { get; set; }
    public int VersionNumber { get; set; }
    public string? Reason { get; set; }
    public DateTime CreatedOnUtc { get; set; }

    public string? Goal { get; set; }
    public string Strategy { get; set; } = string.Empty;
    public decimal ExtraMonthlyPayment { get; set; }
    public decimal TotalMonthlyDebtPayment { get; set; }
    public decimal? TargetUtilizationPercent { get; set; }
    public bool PayOverLimitFirst { get; set; }
    public string? PostUtilizationStrategy { get; set; }
    public bool EnableCashAdvanceBalanceMoves { get; set; }
    public decimal? LoanAmount { get; set; }
    public decimal? LoanAnnualPercentageRate { get; set; }
    public string? LoanApplyStrategy { get; set; }
    public string? LoanApplyCreditCardIdsJson { get; set; }
    public string? LoanType { get; set; }
    public int? LoanTermMonths { get; set; }
    public int? LoanInterestOnlyMonths { get; set; }
    public decimal? LoanFixedMonthlyPayment { get; set; }
    public string? PromotionalTransfersJson { get; set; }

    /// <summary>Card debt at the moment this version was created.</summary>
    public decimal SnapshotDebt { get; set; }
    public int ProjectedMonthsToPayoff { get; set; }
    public decimal ProjectedTotalInterest { get; set; }
    public DateOnly? ProjectedPayoffDate { get; set; }
    public bool ProjectionIsValid { get; set; }

    public ActivePayoffPlan ActivePayoffPlan { get; set; } = null!;
    public ICollection<PayoffPlanPayment> Payments { get; set; } = [];
}
