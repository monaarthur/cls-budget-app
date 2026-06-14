namespace CLS.Budget.Application.PaySchedules.Dtos;

public sealed class PayScheduleDatesResponse
{
    public int PayScheduleId { get; init; }
    public DateTime RangeStart { get; init; }
    public DateTime RangeEnd { get; init; }
    public IReadOnlyList<DateTime> PayDates { get; init; } = [];
    public IReadOnlyList<PayPeriodBoundaryResponse> Periods { get; init; } = [];
}

public sealed class PayPeriodBoundaryResponse
{
    public DateTime PeriodStart { get; init; }
    public DateTime PeriodEnd { get; init; }
    public string Label { get; init; } = null!;
}
