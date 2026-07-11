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
    public string Phone { get; init; } = null!;
    public string Email { get; init; } = null!;
    public string Url { get; init; } = null!;
    public string? Username { get; init; }
    public string? Notes { get; init; }
    public bool IsPaidOff { get; init; }
    public DateTime? PaidOffDate { get; init; }
    public bool? IsCreditCard { get; init; }
    public int AccountCategoryId { get; init; }
    /// <summary>Purchase APR percent (e.g. 22.99), from CreditCardDetail.</summary>
    public decimal? InterestRate { get; init; }
}
