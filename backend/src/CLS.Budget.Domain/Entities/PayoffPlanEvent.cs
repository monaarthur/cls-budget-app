namespace CLS.Budget.Domain.Entities;

/// <summary>
/// Append-only history entry for an active payoff plan lifecycle.
/// </summary>
public class PayoffPlanEvent : ITenantOwned
{
    public int PayoffPlanEventId { get; set; }
    public Guid TenantId { get; set; }
    public int ActivePayoffPlanId { get; set; }
    /// <summary>Started, PaymentRecorded, PaymentVoided, Revised, Completed, Abandoned.</summary>
    public string EventType { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string? PayloadJson { get; set; }
    public DateTime CreatedOnUtc { get; set; }

    public ActivePayoffPlan ActivePayoffPlan { get; set; } = null!;
}