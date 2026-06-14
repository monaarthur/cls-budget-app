namespace CLS.Budget.Application.PaySchedules.Dtos;

public sealed class PayScheduleResponse
{
    public int PayScheduleId { get; init; }
    public int IncomeSourceId { get; init; }
    public string IncomeSourceName { get; init; } = null!;
    public int PayFrequencyTypeId { get; init; }
    public string PayFrequencyTypeName { get; init; } = null!;
    public string Name { get; init; } = null!;
    public DateTime? AnchorDate { get; init; }
    public int? DayOfWeek { get; init; }
    public int? SemiMonthlyDay1 { get; init; }
    public int? SemiMonthlyDay2 { get; init; }
    public bool IsDefault { get; init; }
    public bool IsActive { get; init; }
}
