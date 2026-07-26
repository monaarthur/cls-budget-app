namespace CLS.Budget.Domain.Entities;

/// <summary>
/// Additional fields for credit card accounts (1:1 with <see cref="Account"/>).
/// </summary>
public class CreditCardDetail : ITenantOwned
{
    public int CreditCardDetailId { get; set; }
    public Guid TenantId { get; set; }
    public int AccountId { get; set; }
    public Account? Account { get; set; }
    public decimal? InterestRate { get; set; }
    public decimal? Limit { get; set; }
    public decimal? CashOutInterestRate { get; set; }

    /// <summary>
    /// Optional cash advance fee as a percent of the advance amount (e.g. 5.00 = 5%).
    /// </summary>
    public decimal? CashAdvanceFeePercentage { get; set; }

    /// <summary>Promotional purchase APR percent (e.g. 0 for 0% intro).</summary>
    public decimal? PromotionalAnnualPercentageRate { get; set; }

    /// <summary>UTC date when the promotional APR ends (inclusive through this date).</summary>
    public DateTime? PromotionalRateExpirationDate { get; set; }

    /// <summary>Minimum payment as percent of balance (e.g. 2.00 = 2%).</summary>
    public decimal? MinimumPaymentPercentage { get; set; }

    /// <summary>Floor dollar amount used with <see cref="MinimumPaymentPercentage"/>.</summary>
    public decimal? MinimumPaymentFloor { get; set; }

    /// <summary>
    /// When false, the card is omitted from multi-card payoff analysis
    /// (e.g. a balance already on a creditor payment plan).
    /// </summary>
    public bool IncludeInPayoffAnalysis { get; set; } = true;
}
