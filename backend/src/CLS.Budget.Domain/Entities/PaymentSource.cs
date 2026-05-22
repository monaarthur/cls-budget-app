namespace CLS.Budget.Domain.Entities;

public class PaymentSource
{
    public int PaymentSourceId { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
}
