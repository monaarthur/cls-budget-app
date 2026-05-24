import type { BudgetGridRow } from "@/features/budgets/utils/budgetGridMapper";

export type TrackedPaymentStatus = "pending" | "paid" | "scheduled";

export interface PaymentHalfSummary {
  pending: number;
  paid: number;
  scheduled: number;
}

export interface BudgetPaymentHalfSummaries {
  before: PaymentHalfSummary;
  after: PaymentHalfSummary;
  cutoffLabel: string;
}

function emptySummary(): PaymentHalfSummary {
  return { pending: 0, paid: 0, scheduled: 0 };
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

function getMidMonthCutoffMs(budgetStartPeriod: string): number {
  const budgetStart = new Date(budgetStartPeriod);
  return Date.UTC(
    budgetStart.getUTCFullYear(),
    budgetStart.getUTCMonth(),
    15,
  );
}

export function formatMidMonthCutoffLabel(budgetStartPeriod: string): string {
  const budgetStart = new Date(budgetStartPeriod);
  const month = budgetStart.toLocaleString("en-US", {
    month: "numeric",
    timeZone: "UTC",
  });
  return `${month}/15`;
}

export function summarizePaymentsByHalfMonth(
  rows: BudgetGridRow[],
  budgetStartPeriod: string,
): BudgetPaymentHalfSummaries {
  const cutoffMs = getMidMonthCutoffMs(budgetStartPeriod);
  const before = emptySummary();
  const after = emptySummary();

  for (const row of rows) {
    const status = normalizeTrackedStatus(row.budgetPaymentStatusName);
    if (!status) continue;

    const value = status === "paid" ? row.paymentMade : row.amount;
    const bucket =
      new Date(row.paymentDate).getTime() < cutoffMs ? before : after;
    bucket[status] += value;
  }

  return {
    before,
    after,
    cutoffLabel: formatMidMonthCutoffLabel(budgetStartPeriod),
  };
}
