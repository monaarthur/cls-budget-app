namespace CLS.Budget.Domain.Entities;

public class Account : ITenantOwned
{
    public int AccountId { get; set; }
    public Guid TenantId { get; set; }
    public string Name { get; set; } = null!;
    public string Number { get; set; } = null!;
    public string? Description { get; set; }
    public decimal Balance { get; set; }
    public decimal Limit { get; set; }
    public DateTime AccountOpenDate { get; set; }
    public decimal? MonthlyPayment { get; set; }
    public int? PaymentDay { get; set; }
    public string Phone { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Url { get; set; } = null!;
        public string? Username { get; set; }
        // NOTE: don’t store plaintext passwords in real apps—hash/salt instead.
        public string? Password { get; set; }
        public string? Notes { get; set; }
        public bool IsPaidOff { get; set; }
        public DateTime? PaidOffDate { get; set; } // (typo fixed from PaidOfDate)
    public bool? IsCreditCard { get; set; }
    public int AccountCategoryId { get; set; }
    public CreditCardDetail? CreditCardDetail { get; set; }
}