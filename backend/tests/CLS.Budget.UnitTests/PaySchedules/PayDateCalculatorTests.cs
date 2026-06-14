using CLS.Budget.Application.PaySchedules;
using CLS.Budget.Domain;
using CLS.Budget.Domain.Entities;
using FluentAssertions;

namespace CLS.Budget.UnitTests.PaySchedules;

public class PayDateCalculatorTests
{
    [Fact]
    public void GetPayDatesInRange_SemiMonthly_ReturnsFirstAndFifteenth()
    {
        var schedule = SemiMonthlySchedule(day1: 1, day2: 15);
        var start = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2026, 5, 31, 0, 0, 0, DateTimeKind.Utc);

        var dates = PayDateCalculator.GetPayDatesInRange(schedule, start, end);

        dates.Should().Equal(
            new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 5, 15, 0, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void GetPayDatesInRange_BiWeekly_ReturnsEveryFourteenDaysFromAnchor()
    {
        var anchor = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);
        var schedule = new PaySchedule
        {
            PayFrequencyTypeId = PayFrequencyTypeIds.BiWeekly,
            AnchorDate = anchor
        };
        var start = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2026, 5, 31, 0, 0, 0, DateTimeKind.Utc);

        var dates = PayDateCalculator.GetPayDatesInRange(schedule, start, end);

        dates.Should().Equal(
            new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 5, 15, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 5, 29, 0, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void GetPayDatesInRange_Weekly_ReturnsSevenDayIntervals()
    {
        var anchor = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);
        var schedule = new PaySchedule
        {
            PayFrequencyTypeId = PayFrequencyTypeIds.Weekly,
            AnchorDate = anchor
        };
        var start = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2026, 5, 31, 0, 0, 0, DateTimeKind.Utc);

        var dates = PayDateCalculator.GetPayDatesInRange(schedule, start, end);

        dates.Should().HaveCount(5);
        dates[0].Should().Be(new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc));
        dates[^1].Should().Be(new DateTime(2026, 5, 29, 0, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void GetPayPeriodBoundaries_SemiMonthly_BuildsBeforeAndAfterSplit()
    {
        var schedule = SemiMonthlySchedule(day1: 1, day2: 15);
        var start = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2026, 5, 31, 0, 0, 0, DateTimeKind.Utc);

        var periods = PayDateCalculator.GetPayPeriodBoundaries(schedule, start, end);

        periods.Should().HaveCount(2);
        periods[0].Label.Should().Be("Before 5/15");
        periods[0].PeriodStart.Should().Be(start);
        periods[0].PeriodEnd.Should().Be(new DateTime(2026, 5, 15, 0, 0, 0, DateTimeKind.Utc));
        periods[1].Label.Should().Be("After 5/15");
        periods[1].PeriodStart.Should().Be(new DateTime(2026, 5, 16, 0, 0, 0, DateTimeKind.Utc));
        periods[1].PeriodEnd.Should().Be(end);
    }

    [Fact]
    public void GetPayPeriodBoundaries_SemiMonthly_JuneBudget_HasTwoHalfMonthPanels()
    {
        var schedule = SemiMonthlySchedule(day1: 1, day2: 15);
        var start = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2026, 6, 30, 0, 0, 0, DateTimeKind.Utc);

        var periods = PayDateCalculator.GetPayPeriodBoundaries(schedule, start, end);

        periods.Should().HaveCount(2);
        periods[0].Label.Should().Be("Before 6/15");
        periods[0].PeriodEnd.Should().Be(new DateTime(2026, 6, 15, 0, 0, 0, DateTimeKind.Utc));
        periods[1].Label.Should().Be("After 6/15");
        periods[1].PeriodStart.Should().Be(new DateTime(2026, 6, 16, 0, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void ResolveSemiMonthlyDay_UsesLastDayWhenDayIsEndOfMonthMarker()
    {
        var date = PayDateCalculator.ResolveSemiMonthlyDay(2026, 2, PayDateCalculator.EndOfMonthDay);
        date.Should().Be(new DateTime(2026, 2, 28, 0, 0, 0, DateTimeKind.Utc));
    }

    private static PaySchedule SemiMonthlySchedule(int day1, int day2) => new()
    {
        PayFrequencyTypeId = PayFrequencyTypeIds.SemiMonthly,
        SemiMonthlyDay1 = day1,
        SemiMonthlyDay2 = day2
    };
}
