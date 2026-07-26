namespace CLS.Budget.Application.Accounts.Dtos;

public sealed class UpdateAccountRequest
{
    public string Name { get; init; } = null!;
    public string Number { get; init; } = null!;
    public string? Description { get; init; }
    public decimal Balance { get; init; }
    public decimal Limit { get; init; }
    public DateTime AccountOpenDate { get; init; }
    public decimal? MonthlyPayment { get; init; }
    public int? PaymentDay { get; init; }
    public string Phone { get; init; } = null!;
    public string Email { get; init; } = null!;
    public string Url { get; init; } = null!;
    public string? Username { get; init; }
    public string? Password { get; init; }
    public string? Notes { get; init; }
    public bool IsPaidOff { get; init; }
    public DateTime? PaidOffDate { get; init; }
    public bool? IsCreditCard { get; init; }
    public int AccountCategoryId { get; init; }
    /// <summary>Purchase APR percent (e.g. 22.99).</summary>
    public decimal? InterestRate { get; init; }
    public decimal? PromotionalAnnualPercentageRate { get; init; }
    public DateTime? PromotionalRateExpirationDate { get; init; }
    public decimal? MinimumPaymentPercentage { get; init; }
    public decimal? MinimumPaymentFloor { get; init; }
    /// <summary>Optional cash advance APR percent (e.g. 28.99).</summary>
    public decimal? CashOutInterestRate { get; init; }
    /// <summary>Optional cash advance fee percent (e.g. 5.00 = 5%).</summary>
    public decimal? CashAdvanceFeePercentage { get; init; }
    /// <summary>When false, omitted from multi-card payoff analysis. Defaults to true.</summary>
    public bool IncludeInPayoffAnalysis { get; init; } = true;
}
