namespace CLS.Budget.Application.PaySchedules.Dtos;

public sealed class PayFrequencyTypeResponse
{
    public int PayFrequencyTypeId { get; init; }
    public string Name { get; init; } = null!;
    public string? Description { get; init; }
}
