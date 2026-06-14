import {
  PayFrequencyTypeIds,
  type PayScheduleConfig,
  type PayScheduleResponse,
} from "@/features/pay-schedules/types/paySchedule";

export const END_OF_MONTH_DAY = 31;

export interface PayPeriodBoundary {
  periodStart: string;
  periodEnd: string;
  label: string;
}

export function toScheduleConfig(
  schedule: PayScheduleResponse,
): PayScheduleConfig {
  return {
    payFrequencyTypeId: schedule.payFrequencyTypeId,
    anchorDate: schedule.anchorDate,
    semiMonthlyDay1: schedule.semiMonthlyDay1,
    semiMonthlyDay2: schedule.semiMonthlyDay2,
  };
}

export function toUtcDate(iso: string): Date {
  const value = iso.includes("T") ? iso : `${iso}T00:00:00.000Z`;
  const date = new Date(value);
  return new Date(
    Date.UTC(date.getUTCFullYear(), date.getUTCMonth(), date.getUTCDate()),
  );
}

export function toUtcDateIso(date: Date): string {
  return new Date(
    Date.UTC(date.getUTCFullYear(), date.getUTCMonth(), date.getUTCDate()),
  ).toISOString();
}

export function resolveSemiMonthlyDay(
  year: number,
  month: number,
  day: number,
): Date {
  const lastDay = new Date(Date.UTC(year, month + 1, 0)).getUTCDate();
  const resolvedDay = day === END_OF_MONTH_DAY ? lastDay : Math.min(day, lastDay);
  return new Date(Date.UTC(year, month, resolvedDay));
}

export function getPayDatesInRange(
  schedule: PayScheduleConfig,
  rangeStart: string,
  rangeEnd: string,
): string[] {
  const start = toUtcDate(rangeStart);
  const end = toUtcDate(rangeEnd);
  if (end.getTime() < start.getTime()) return [];

  switch (schedule.payFrequencyTypeId) {
    case PayFrequencyTypeIds.Weekly:
      return getIntervalPayDates(start, end, schedule.anchorDate, 7);
    case PayFrequencyTypeIds.BiWeekly:
      return getIntervalPayDates(start, end, schedule.anchorDate, 14);
    case PayFrequencyTypeIds.SemiMonthly:
      return getSemiMonthlyPayDates(
        start,
        end,
        schedule.semiMonthlyDay1,
        schedule.semiMonthlyDay2,
      );
    default:
      return [];
  }
}

export function getPayPeriodBoundaries(
  schedule: PayScheduleConfig,
  rangeStart: string,
  rangeEnd: string,
): PayPeriodBoundary[] {
  if (schedule.payFrequencyTypeId === PayFrequencyTypeIds.SemiMonthly) {
    return getSemiMonthlyHalfMonthBoundaries(schedule, rangeStart, rangeEnd);
  }

  const start = toUtcDate(rangeStart);
  const end = toUtcDate(rangeEnd);
  const payDates = getPayDatesInRange(schedule, rangeStart, rangeEnd).map(
    toUtcDate,
  );

  if (payDates.length === 0) {
    return [
      {
        periodStart: toUtcDateIso(start),
        periodEnd: toUtcDateIso(end),
        label: `${formatShortDate(start)} – ${formatShortDate(end)}`,
      },
    ];
  }

  const boundaries: PayPeriodBoundary[] = [];
  const lastPayBeforeStart = getLastPayDateBeforeRange(schedule, rangeStart);
  let previousExclusive = lastPayBeforeStart
    ? toUtcDate(lastPayBeforeStart)
    : addDays(start, -1);

  for (const payDate of payDates) {
    const periodStart = addDays(previousExclusive, 1);
    boundaries.push({
      periodStart: toUtcDateIso(periodStart),
      periodEnd: toUtcDateIso(payDate),
      label: `Through ${formatShortDate(payDate)}`,
    });
    previousExclusive = payDate;
  }

  if (previousExclusive.getTime() < end.getTime()) {
    const periodStart = addDays(previousExclusive, 1);
    boundaries.push({
      periodStart: toUtcDateIso(periodStart),
      periodEnd: toUtcDateIso(end),
      label: `After ${formatShortDate(periodStart)}`,
    });
  }

  return boundaries;
}

function getSemiMonthlyHalfMonthBoundaries(
  schedule: PayScheduleConfig,
  rangeStart: string,
  rangeEnd: string,
): PayPeriodBoundary[] {
  const start = toUtcDate(rangeStart);
  const end = toUtcDate(rangeEnd);
  const day1 = schedule.semiMonthlyDay1;
  const day2 = schedule.semiMonthlyDay2;

  if (day1 == null || day2 == null) {
    return [
      {
        periodStart: toUtcDateIso(start),
        periodEnd: toUtcDateIso(end),
        label: `${formatShortDate(start)} – ${formatShortDate(end)}`,
      },
    ];
  }

  const splitDay = Math.max(day1, day2);
  const splitDate = resolveSemiMonthlyDay(
    start.getUTCFullYear(),
    start.getUTCMonth(),
    splitDay,
  );
  const splitLabel = formatShortDate(splitDate);
  const period1End =
    splitDate.getTime() <= end.getTime() ? splitDate : end;
  const period2Start = addDays(splitDate, 1);

  if (period2Start.getTime() > end.getTime()) {
    return [
      {
        periodStart: toUtcDateIso(start),
        periodEnd: toUtcDateIso(end),
        label: `Before ${splitLabel}`,
      },
    ];
  }

  return [
    {
      periodStart: toUtcDateIso(start),
      periodEnd: toUtcDateIso(period1End),
      label: `Before ${splitLabel}`,
    },
    {
      periodStart: toUtcDateIso(period2Start),
      periodEnd: toUtcDateIso(end),
      label: `After ${splitLabel}`,
    },
  ];
}

/** Paychecks before the budget month start belong in the first pay period. */
export function resolvePaymentPayPeriodIndex(
  paymentDate: string,
  periods: ReadonlyArray<{ periodStart: string; periodEnd: string }>,
  budgetStartPeriod: string,
): number {
  const periodIndex = periods.findIndex((period) =>
    paymentDateInPeriod(paymentDate, period.periodStart, period.periodEnd),
  );
  if (periodIndex >= 0) return periodIndex;
  if (periods.length === 0) return -1;

  const paymentMs = toUtcDate(paymentDate).getTime();
  const budgetStartMs = toUtcDate(budgetStartPeriod).getTime();
  const firstPeriodStartMs = toUtcDate(periods[0].periodStart).getTime();

  if (paymentMs < budgetStartMs || paymentMs < firstPeriodStartMs) {
    return 0;
  }

  return -1;
}

function getLastPayDateBeforeRange(
  schedule: PayScheduleConfig,
  rangeStart: string,
): string | null {
  const start = toUtcDate(rangeStart);
  const lookbackStart = addDays(start, -35);
  const lookbackEnd = addDays(start, -1);
  const dates = getPayDatesInRange(
    schedule,
    toUtcDateIso(lookbackStart),
    toUtcDateIso(lookbackEnd),
  );
  return dates.length > 0 ? dates[dates.length - 1]! : null;
}

function getIntervalPayDates(
  rangeStart: Date,
  rangeEnd: Date,
  anchorDate: string | null,
  intervalDays: number,
): string[] {
  if (!anchorDate) return [];

  let cursor = toUtcDate(anchorDate);
  while (cursor.getTime() > rangeStart.getTime()) {
    cursor = addDays(cursor, -intervalDays);
  }
  while (cursor.getTime() < rangeStart.getTime()) {
    cursor = addDays(cursor, intervalDays);
  }

  const dates: string[] = [];
  while (cursor.getTime() <= rangeEnd.getTime()) {
    dates.push(toUtcDateIso(cursor));
    cursor = addDays(cursor, intervalDays);
  }

  return dates;
}

function getSemiMonthlyPayDates(
  rangeStart: Date,
  rangeEnd: Date,
  day1: number | null,
  day2: number | null,
): string[] {
  if (
    day1 == null ||
    day2 == null ||
    day1 < 1 ||
    day1 > 31 ||
    day2 < 1 ||
    day2 > 31
  ) {
    return [];
  }

  const dates: string[] = [];
  let cursor = new Date(
    Date.UTC(rangeStart.getUTCFullYear(), rangeStart.getUTCMonth(), 1),
  );
  const endMonth = new Date(
    Date.UTC(rangeEnd.getUTCFullYear(), rangeEnd.getUTCMonth(), 1),
  );

  while (cursor.getTime() <= endMonth.getTime()) {
    for (const day of [...new Set([day1, day2])].sort((a, b) => a - b)) {
      const payDate = resolveSemiMonthlyDay(
        cursor.getUTCFullYear(),
        cursor.getUTCMonth(),
        day,
      );
      if (
        payDate.getTime() >= rangeStart.getTime() &&
        payDate.getTime() <= rangeEnd.getTime()
      ) {
        dates.push(toUtcDateIso(payDate));
      }
    }
    cursor = new Date(
      Date.UTC(cursor.getUTCFullYear(), cursor.getUTCMonth() + 1, 1),
    );
  }

  dates.sort();
  return dates;
}

function addDays(date: Date, days: number): Date {
  const next = new Date(date);
  next.setUTCDate(next.getUTCDate() + days);
  return next;
}

function formatShortDate(date: Date): string {
  return date.toLocaleDateString("en-US", {
    month: "numeric",
    day: "numeric",
    timeZone: "UTC",
  });
}

export function paymentDateInPeriod(
  paymentDate: string,
  periodStart: string,
  periodEnd: string,
): boolean {
  const paymentMs = toUtcDate(paymentDate).getTime();
  return (
    paymentMs >= toUtcDate(periodStart).getTime() &&
    paymentMs <= toUtcDate(periodEnd).getTime()
  );
}
