import type { BudgetGridRow } from "@/features/budgets/utils/budgetGridMapper";
import type { PayScheduleConfig } from "@/features/pay-schedules/types/paySchedule";
import {
  getPayPeriodBoundaries,
  resolvePaymentPayPeriodIndex,
  type PayPeriodBoundary,
} from "@/features/pay-schedules/utils/payDateCalculator";

export type TrackedPaymentStatus = "pending" | "paid" | "scheduled";

export interface PaymentHalfSummary {
  pending: number;
  paid: number;
  scheduled: number;
}

export interface PaymentClearedSummary {
  count: number;
  amount: number;
  total: number;
}

export interface PaymentPeriodSummary {
  label: string;
  periodStart: string;
  periodEnd: string;
  summary: PaymentHalfSummary;
  cleared: PaymentClearedSummary;
}

export interface BudgetPaymentPeriodSummaries {
  periods: PaymentPeriodSummary[];
  noDate: PaymentHalfSummary;
  noDateCleared: PaymentClearedSummary;
}

export type PayPeriodFilter =
  | { type: "period"; index: number }
  | { type: "no-date" };

function emptySummary(): PaymentHalfSummary {
  return { pending: 0, paid: 0, scheduled: 0 };
}

function emptyClearedSummary(): PaymentClearedSummary {
  return { count: 0, amount: 0, total: 0 };
}

export function isPaymentDateSelected(
  paymentDate: string | null | undefined,
): boolean {
  if (paymentDate == null || paymentDate.trim() === "") return false;
  const ms = new Date(paymentDate).getTime();
  return Number.isFinite(ms);
}

function addRowToSummary(
  summary: PaymentHalfSummary,
  row: BudgetGridRow,
): void {
  const status = normalizeTrackedStatus(row.budgetPaymentStatusName);
  if (!status) return;

  const value = status === "paid" ? row.paymentMade : row.amount;
  summary[status] += value;
}

function addRowToCleared(
  cleared: PaymentClearedSummary,
  row: BudgetGridRow,
): void {
  cleared.total += 1;
  if (!row.isCleared) return;
  cleared.count += 1;
  cleared.amount += row.paymentMade;
}

export function normalizeTrackedStatus(
  statusName: string,
): TrackedPaymentStatus | null {
  const normalized = statusName.trim().toLowerCase();
  if (normalized === "pending") return "pending";
  if (normalized === "paid") return "paid";
  if (normalized === "scheduled") return "scheduled";
  return null;
}

export function summarizePaymentsByPayPeriod(
  rows: BudgetGridRow[],
  schedule: PayScheduleConfig,
  budgetStartPeriod: string,
  budgetEndPeriod: string,
): BudgetPaymentPeriodSummaries {
  const boundaries = getPayPeriodBoundaries(
    schedule,
    budgetStartPeriod,
    budgetEndPeriod,
  );

  const periods = boundaries.map((boundary) => ({
    label: formatPeriodTitle(boundary),
    periodStart: boundary.periodStart,
    periodEnd: boundary.periodEnd,
    summary: emptySummary(),
    cleared: emptyClearedSummary(),
  }));

  const noDate = emptySummary();
  const noDateCleared = emptyClearedSummary();

  for (const row of rows) {
    if (!isPaymentDateSelected(row.paymentDate)) {
      addRowToSummary(noDate, row);
      addRowToCleared(noDateCleared, row);
      continue;
    }

    const periodIndex = resolvePaymentPayPeriodIndex(
      row.paymentDate!,
      periods,
      budgetStartPeriod,
    );

    if (periodIndex >= 0) {
      addRowToSummary(periods[periodIndex].summary, row);
      addRowToCleared(periods[periodIndex].cleared, row);
    }
  }

  return { periods, noDate, noDateCleared };
}

export function summarizePaymentsWithNoDate(
  rows: BudgetGridRow[],
): PaymentHalfSummary {
  const summary = emptySummary();

  for (const row of rows) {
    if (isPaymentDateSelected(row.paymentDate)) continue;
    addRowToSummary(summary, row);
  }

  return summary;
}

export function getPaymentHalfSummaryTotal(
  summary: PaymentHalfSummary,
): number {
  return summary.pending + summary.paid + summary.scheduled;
}

export function rowMatchesPayPeriodFilter(
  row: BudgetGridRow,
  filter: PayPeriodFilter | null,
  periods: ReadonlyArray<{ periodStart: string; periodEnd: string }>,
  budgetStartPeriod: string,
): boolean {
  if (!filter) return true;

  if (filter.type === "no-date") {
    return !isPaymentDateSelected(row.paymentDate);
  }

  if (!isPaymentDateSelected(row.paymentDate)) return false;

  const periodIndex = resolvePaymentPayPeriodIndex(
    row.paymentDate!,
    periods,
    budgetStartPeriod,
  );
  return periodIndex === filter.index;
}

function formatPeriodTitle(boundary: PayPeriodBoundary): string {
  if (boundary.label.startsWith("After ")) {
    return `Payment ${boundary.label.toLowerCase()}`;
  }

  if (boundary.label.startsWith("Before ")) {
    return `Payment ${boundary.label.toLowerCase()}`;
  }

  if (boundary.label.startsWith("Through ")) {
    return `Payment through ${boundary.label.replace("Through ", "")}`;
  }

  return boundary.label;
}

/** @deprecated Use summarizePaymentsByPayPeriod */
export function summarizePaymentsByHalfMonth(
  rows: BudgetGridRow[],
  budgetStartPeriod: string,
): {
  before: PaymentHalfSummary;
  after: PaymentHalfSummary;
  cutoffLabel: string;
} {
  const budgetEnd = new Date(budgetStartPeriod);
  budgetEnd.setUTCMonth(budgetEnd.getUTCMonth() + 1);
  budgetEnd.setUTCDate(0);

  const result = summarizePaymentsByPayPeriod(
    rows,
    {
      payFrequencyTypeId: 3,
      anchorDate: null,
      semiMonthlyDay1: 1,
      semiMonthlyDay2: 15,
    },
    budgetStartPeriod,
    budgetEnd.toISOString(),
  );

  return {
    before: result.periods[0]?.summary ?? emptySummary(),
    after: result.periods[1]?.summary ?? emptySummary(),
    cutoffLabel: "15",
  };
}
