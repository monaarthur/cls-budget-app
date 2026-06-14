namespace CLS.Budget.Domain.Entities;

public class PayFrequencyType
{
    public int PayFrequencyTypeId { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
}
