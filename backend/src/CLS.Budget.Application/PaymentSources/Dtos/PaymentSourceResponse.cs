namespace CLS.Budget.Application.PaymentSources.Dtos;

public sealed class PaymentSourceResponse
{
    public int PaymentSourceId { get; init; }
    public string Name { get; init; } = null!;
    public string? Description { get; init; }
}
