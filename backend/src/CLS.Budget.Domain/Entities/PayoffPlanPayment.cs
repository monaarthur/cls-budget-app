namespace CLS.Budget.Domain.Entities;

/// <summary>
/// Payment recorded against an active payoff plan; applied to Account.Balance when created.
/// </summary>
public class PayoffPlanPayment : ITenantOwned
{
    public int PayoffPlanPaymentId { get; set; }
    public Guid TenantId { get; set; }
    public int ActivePayoffPlanId { get; set; }
    public int PayoffPlanVersionId { get; set; }
    public int AccountId { get; set; }
    public decimal Amount { get; set; }
    public DateOnly PaymentDate { get; set; }
    public string? Notes { get; set; }
    public bool IsVoided { get; set; }
    public DateTime? VoidedOnUtc { get; set; }
    public DateTime CreatedOnUtc { get; set; }

    public ActivePayoffPlan ActivePayoffPlan { get; set; } = null!;
    public PayoffPlanVersion PayoffPlanVersion { get; set; } = null!;
    public Account Account { get; set; } = null!;
}
