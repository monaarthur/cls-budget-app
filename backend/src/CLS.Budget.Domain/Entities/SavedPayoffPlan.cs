namespace CLS.Budget.Domain.Entities;

/// <summary>
/// Named payoff plan configuration for multi-plan comparison.
/// </summary>
public class SavedPayoffPlan : ITenantOwned
{
    public int SavedPayoffPlanId { get; set; }
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    /// <summary>improveCredit, lowerUtilization, minimizeInterest, or null.</summary>
    public string? Goal { get; set; }
    /// <summary>Avalanche, Snowball, or MinimumsOnly.</summary>
    public string Strategy { get; set; } = string.Empty;
    public decimal ExtraMonthlyPayment { get; set; }
    public decimal TotalMonthlyDebtPayment { get; set; }
    public decimal? TargetUtilizationPercent { get; set; }
    public bool PayOverLimitFirst { get; set; }
    /// <summary>Avalanche, Snowball, or null.</summary>
    public string? PostUtilizationStrategy { get; set; }
    public bool EnableCashAdvanceBalanceMoves { get; set; }
    /// <summary>Optional loan principal applied first, then repaid in the plan.</summary>
    public decimal? LoanAmount { get; set; }
    /// <summary>APR percent for the optional loan.</summary>
    public decimal? LoanAnnualPercentageRate { get; set; }
    /// <summary>Avalanche, Snowball, or SelectedAccounts order for applying loan proceeds; null when no loan.</summary>
    public string? LoanApplyStrategy { get; set; }
    /// <summary>JSON array of credit card account ids when LoanApplyStrategy is SelectedAccounts.</summary>
    public string? LoanApplyCreditCardIdsJson { get; set; }
    /// <summary>Personal, HomeEquity, Heloc, Retirement401k, Family.</summary>
    public string? LoanType { get; set; }
    public int? LoanTermMonths { get; set; }
    public int? LoanInterestOnlyMonths { get; set; }
    public decimal? LoanFixedMonthlyPayment { get; set; }
    /// <summary>JSON array of promotional transfer DTOs.</summary>
    public string? PromotionalTransfersJson { get; set; }
    public DateTime CreatedOnUtc { get; set; }
    public DateTime UpdatedOnUtc { get; set; }
}
