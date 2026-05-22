namespace CLS.Budget.Domain.Entities;

/// <summary>
/// Additional fields for credit card accounts (1:1 with <see cref="Account"/>).
/// </summary>
public class CreditCardDetail
{
    public int CreditCardDetailId { get; set; }
    public int AccountId { get; set; }
    public Account? Account { get; set; }
    public decimal? InterestRate { get; set; }
    public decimal? Limit { get; set; }
    public decimal? CashOutInterestRate { get; set; }

}
