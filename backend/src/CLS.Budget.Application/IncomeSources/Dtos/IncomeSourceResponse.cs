namespace CLS.Budget.Application.IncomeSources.Dtos;

public sealed class IncomeSourceResponse
{
    public int IncomeSourceId { get; init; }
    public string Name { get; init; } = null!;
    public bool IsActive { get; init; }
}
