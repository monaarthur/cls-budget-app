namespace CLS.Budget.Domain.Entities;

/// <summary>
/// Tenant's live credit-card payoff plan with payments and version history.
/// At most one plan should be Active per tenant at a time.
/// </summary>
public class ActivePayoffPlan : ITenantOwned
{
    public int ActivePayoffPlanId { get; set; }
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    /// <summary>Active, Completed, or Abandoned.</summary>
    public string Status { get; set; } = ActivePayoffPlanStatuses.Active;
    public int? SourceSavedPayoffPlanId { get; set; }
    public DateTime StartedOnUtc { get; set; }
    public DateTime? EndedOnUtc { get; set; }
    public int CurrentVersionNumber { get; set; } = 1;
    /// <summary>Total card debt when the plan was first activated.</summary>
    public decimal StartingDebt { get; set; }

    /// <summary>improveCredit, lowerUtilization, minimizeInterest, or null.</summary>
    public string? Goal { get; set; }
    /// <summary>Avalanche, Snowball, or MinimumsOnly.</summary>
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

    public DateTime CreatedOnUtc { get; set; }
    public DateTime UpdatedOnUtc { get; set; }

    public ICollection<PayoffPlanVersion> Versions { get; set; } = [];
    public ICollection<PayoffPlanPayment> Payments { get; set; } = [];
    public ICollection<PayoffPlanEvent> Events { get; set; } = [];
}
