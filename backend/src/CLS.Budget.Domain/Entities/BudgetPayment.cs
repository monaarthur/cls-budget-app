namespace CLS.Budget.Domain.Entities;

public class BudgetPayment : ITenantOwned
{
    public int BudgetPaymentId { get; set; }
    public Guid TenantId { get; set; }
    public int BudgetId { get; set; }
    public int AccountId { get; set; }
    public Account? Account { get; set; }
    public decimal PaymentMade { get; set; }
    public decimal Amount { get; set; }
    public int BudgetPaymentStatusId { get; set; }
    public BudgetPaymentStatus? BudgetPaymentStatus { get; set; }
    public bool IsCleared { get; set; }
    public DateTime PaymentDate { get; set; }
    public DateTime? ClearedDate { get; set; }
    public int? PaymentSourceId { get; set; }
    public PaymentSource? PaymentSource { get; set; }
    public int? IncomeSourceId { get; set; }
    public IncomeSource? IncomeSource { get; set; }
}
