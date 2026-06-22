using CLS.Budget.Domain;
using CLS.Budget.Domain.Entities;

namespace CLS.Budget.Application.PaySchedules;

public static class PayDateCalculator
{
    public const int EndOfMonthDay = 31;

    public static IReadOnlyList<DateTime> GetPayDatesInRange(
        PaySchedule schedule,
        DateTime rangeStart,
        DateTime rangeEnd)
    {
        var start = ToUtcDate(rangeStart);
        var end = ToUtcDate(rangeEnd);
        if (end < start)
        {
            return [];
        }

        return schedule.PayFrequencyTypeId switch
        {
            PayFrequencyTypeIds.Weekly => GetIntervalPayDates(start, end, schedule.AnchorDate, 7),
            PayFrequencyTypeIds.BiWeekly => GetIntervalPayDates(start, end, schedule.AnchorDate, 14),
            PayFrequencyTypeIds.SemiMonthly => GetSemiMonthlyPayDates(
                start,
                end,
                schedule.SemiMonthlyDay1,
                schedule.SemiMonthlyDay2),
            _ => throw new ArgumentOutOfRangeException(
                nameof(schedule.PayFrequencyTypeId),
                schedule.PayFrequencyTypeId,
                "Unsupported pay frequency type.")
        };
    }

    public static IReadOnlyList<PayPeriodBoundary> GetPayPeriodBoundaries(
        PaySchedule schedule,
        DateTime rangeStart,
        DateTime rangeEnd)
    {
        var start = ToUtcDate(rangeStart);
        var end = ToUtcDate(rangeEnd);

        if (schedule.PayFrequencyTypeId == PayFrequencyTypeIds.SemiMonthly)
        {
            return GetSemiMonthlyHalfMonthBoundaries(schedule, start, end);
        }

        var payDates = GetPayDatesInRange(schedule, start, end);

        if (payDates.Count == 0)
        {
            return
            [
                new PayPeriodBoundary(start, end, $"{start:M/d} – {end:M/d}")
            ];
        }

        var boundaries = new List<PayPeriodBoundary>();
        var lastPayBeforeStart = GetLastPayDateBeforeRange(schedule, start);
        var previousExclusive = lastPayBeforeStart ?? start.AddDays(-1);

        foreach (var payDate in payDates)
        {
            boundaries.Add(new PayPeriodBoundary(
                previousExclusive.AddDays(1),
                payDate,
                FormatPeriodLabel(previousExclusive.AddDays(1), payDate, payDate)));
            previousExclusive = payDate;
        }

        if (previousExclusive < end)
        {
            boundaries.Add(new PayPeriodBoundary(
                previousExclusive.AddDays(1),
                end,
                FormatPeriodLabel(previousExclusive.AddDays(1), end, null, isAfter: true)));
        }

        return boundaries;
    }

    private static IReadOnlyList<PayPeriodBoundary> GetSemiMonthlyHalfMonthBoundaries(
        PaySchedule schedule,
        DateTime start,
        DateTime end)
    {
        if (schedule.SemiMonthlyDay1 is null or < 1 or > 31 ||
            schedule.SemiMonthlyDay2 is null or < 1 or > 31)
        {
            return [new PayPeriodBoundary(start, end, $"{start:M/d} – {end:M/d}")];
        }

        var splitDay = Math.Max(schedule.SemiMonthlyDay1.Value, schedule.SemiMonthlyDay2.Value);
        var splitDate = ResolveSemiMonthlyDay(start.Year, start.Month, splitDay);
        var period1End = splitDate <= end ? splitDate.AddDays(-1) : end;
        var period2Start = splitDate;

        if (period2Start > end)
        {
            return
            [
                new PayPeriodBoundary(start, end, $"Before {splitDate:M/d}")
            ];
        }

        if (period1End < start)
        {
            return
            [
                new PayPeriodBoundary(period2Start, end, $"After {splitDate:M/d}")
            ];
        }

        return
        [
            new PayPeriodBoundary(start, period1End, $"Before {splitDate:M/d}"),
            new PayPeriodBoundary(period2Start, end, $"After {splitDate:M/d}")
        ];
    }

    internal static DateTime ToUtcDate(DateTime value) =>
        DateTime.SpecifyKind(value.Date, DateTimeKind.Utc);

    internal static DateTime ResolveSemiMonthlyDay(int year, int month, int day)
    {
        var lastDay = DateTime.DaysInMonth(year, month);
        var resolvedDay = day == EndOfMonthDay ? lastDay : Math.Min(day, lastDay);
        return new DateTime(year, month, resolvedDay, 0, 0, 0, DateTimeKind.Utc);
    }

    private static IReadOnlyList<DateTime> GetIntervalPayDates(
        DateTime rangeStart,
        DateTime rangeEnd,
        DateTime? anchorDate,
        int intervalDays)
    {
        if (anchorDate is null)
        {
            return [];
        }

        var anchor = ToUtcDate(anchorDate.Value);
        var cursor = anchor;

        while (cursor > rangeStart)
        {
            cursor = cursor.AddDays(-intervalDays);
        }

        while (cursor < rangeStart)
        {
            cursor = cursor.AddDays(intervalDays);
        }

        var dates = new List<DateTime>();
        while (cursor <= rangeEnd)
        {
            dates.Add(cursor);
            cursor = cursor.AddDays(intervalDays);
        }

        return dates;
    }

    private static IReadOnlyList<DateTime> GetSemiMonthlyPayDates(
        DateTime rangeStart,
        DateTime rangeEnd,
        int? day1,
        int? day2)
    {
        if (day1 is null or < 1 or > 31 || day2 is null or < 1 or > 31)
        {
            return [];
        }

        var dates = new List<DateTime>();
        var cursor = new DateTime(rangeStart.Year, rangeStart.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var endMonth = new DateTime(rangeEnd.Year, rangeEnd.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        while (cursor <= endMonth)
        {
            foreach (var day in new[] { day1.Value, day2.Value }.Distinct().Order())
            {
                var payDate = ResolveSemiMonthlyDay(cursor.Year, cursor.Month, day);
                if (payDate >= rangeStart && payDate <= rangeEnd)
                {
                    dates.Add(payDate);
                }
            }

            cursor = cursor.AddMonths(1);
        }

        dates.Sort();
        return dates;
    }

    private static DateTime? GetLastPayDateBeforeRange(PaySchedule schedule, DateTime rangeStart)
    {
        var start = ToUtcDate(rangeStart);
        var lookbackStart = start.AddDays(-35);
        var lookbackEnd = start.AddDays(-1);
        var dates = GetPayDatesInRange(schedule, lookbackStart, lookbackEnd);
        return dates.Count > 0 ? dates[^1] : null;
    }

    private static string FormatPeriodLabel(
        DateTime periodStart,
        DateTime periodEnd,
        DateTime? payDate,
        bool isAfter = false)
    {
        if (isAfter && payDate is null)
        {
            return $"After {periodStart:M/d}";
        }

        if (payDate is not null && periodStart == payDate && periodEnd == payDate)
        {
            return $"Through {payDate:M/d}";
        }

        if (payDate is not null)
        {
            return $"Through {payDate:M/d}";
        }

        return $"{periodStart:M/d} – {periodEnd:M/d}";
    }
}

public sealed record PayPeriodBoundary(
    DateTime PeriodStart,
    DateTime PeriodEnd,
    string Label);
