import type { BudgetResponse } from "@/features/budgets/types/budget";

export function formatBudgetMonthYear(startPeriod: string): string {
  const date = new Date(startPeriod);
  return date.toLocaleString("en-US", {
    month: "long",
    year: "numeric",
    timeZone: "UTC",
  });
}

export function getBudgetMonth(startPeriod: string): string {
  const date = new Date(startPeriod);
  return date.toLocaleString("en-US", { month: "long", timeZone: "UTC" });
}

export function getBudgetYear(startPeriod: string): number {
  const date = new Date(startPeriod);
  return date.getUTCFullYear();
}

export function sortBudgetsByRecent(budgets: BudgetResponse[]): BudgetResponse[] {
  return [...budgets].sort(
    (a, b) =>
      new Date(b.startPeriod).getTime() - new Date(a.startPeriod).getTime(),
  );
}

export const RECENT_BUDGETS_LIMIT = 5;

export function formatBudgetPeriod(startPeriod: string, endPeriod: string): string {
  const start = new Date(startPeriod).toLocaleDateString("en-US", {
    month: "short",
    day: "numeric",
    year: "numeric",
    timeZone: "UTC",
  });
  const end = new Date(endPeriod).toLocaleDateString("en-US", {
    month: "short",
    day: "numeric",
    year: "numeric",
    timeZone: "UTC",
  });
  return `${start} – ${end}`;
}

export function getNextBudgetPeriod(sourceStartPeriod: string): {
  name: string;
  startPeriod: string;
  endPeriod: string;
} {
  const source = new Date(sourceStartPeriod);
  const nextMonthStart = new Date(
    Date.UTC(source.getUTCFullYear(), source.getUTCMonth() + 1, 1),
  );
  const nextMonthEnd = new Date(
    Date.UTC(
      nextMonthStart.getUTCFullYear(),
      nextMonthStart.getUTCMonth() + 1,
      0,
    ),
  );

  return {
    name: nextMonthStart.toLocaleString("en-US", {
      month: "long",
      year: "numeric",
      timeZone: "UTC",
    }),
    startPeriod: nextMonthStart.toISOString(),
    endPeriod: nextMonthEnd.toISOString(),
  };
}
