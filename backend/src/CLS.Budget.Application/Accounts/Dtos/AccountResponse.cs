namespace CLS.Budget.Application.Accounts.Dtos;

public sealed class AccountResponse
{
    public int AccountId { get; init; }
    public string Name { get; init; } = null!;
    public string Number { get; init; } = null!;
    public string? Description { get; init; }
    public decimal Balance { get; init; }
    public decimal Limit { get; init; }
    public DateTime AccountOpenDate { get; init; }
    public decimal? MonthlyPayment { get; init; }
    public int? PaymentDay { get; init; }
    public int? GracePeriod { get; init; }
    public int? GraceDay { get; init; }
    public string Phone { get; init; } = null!;
    public string Email { get; init; } = null!;
    public string Url { get; init; } = null!;
    public string? Username { get; init; }
    public string? Notes { get; init; }
    public bool IsPaidOff { get; init; }
    public DateTime? PaidOffDate { get; init; }
    public bool? IsCreditCard { get; init; }
    public int AccountCategoryId { get; init; }
    public string? AccountCategoryName { get; init; }
    public int? AccountSubCategoryId { get; init; }
    public string? AccountSubCategoryName { get; init; }
    /// <summary>Purchase APR percent (e.g. 22.99), from CreditCardDetail.</summary>
    public decimal? InterestRate { get; init; }
    public decimal? PromotionalAnnualPercentageRate { get; init; }
    public DateTime? PromotionalRateExpirationDate { get; init; }
    /// <summary>Minimum payment percent of balance (e.g. 2.00 = 2%).</summary>
    public decimal? MinimumPaymentPercentage { get; init; }
    public decimal? MinimumPaymentFloor { get; init; }
    /// <summary>Optional cash advance APR percent (e.g. 28.99).</summary>
    public decimal? CashOutInterestRate { get; init; }
    /// <summary>Optional cash advance fee percent (e.g. 5.00 = 5%).</summary>
    public decimal? CashAdvanceFeePercentage { get; init; }
    /// <summary>When false, omitted from multi-card payoff analysis.</summary>
    public bool IncludeInPayoffAnalysis { get; init; } = true;
}
