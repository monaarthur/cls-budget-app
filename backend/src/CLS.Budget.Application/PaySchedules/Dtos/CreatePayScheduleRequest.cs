namespace CLS.Budget.Application.PaySchedules.Dtos;

public sealed class CreatePayScheduleRequest
{
    public int IncomeSourceId { get; init; }
    public int PayFrequencyTypeId { get; init; }
    public string Name { get; init; } = null!;
    public DateTime? AnchorDate { get; init; }
    public int? DayOfWeek { get; init; }
    public int? SemiMonthlyDay1 { get; init; }
    public int? SemiMonthlyDay2 { get; init; }
    public bool IsDefault { get; init; }
    public bool IsActive { get; init; } = true;
}
