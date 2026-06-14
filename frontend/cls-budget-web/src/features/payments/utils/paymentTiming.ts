import type { PaymentResponse } from "@/features/payments/types/payment";

export type PaymentTiming = "paid" | "overdue" | "due-soon" | "upcoming" | "none";

const PAID_STATUSES = new Set(["paid"]);
const ACTIVE_STATUSES = new Set(["pending", "scheduled", "failed", "overdue"]);

export function isPaidPaymentStatus(statusName: string): boolean {
  return PAID_STATUSES.has(statusName.trim().toLowerCase());
}

export function isActivePaymentStatus(statusName: string): boolean {
  const normalized = statusName.trim().toLowerCase();
  return ACTIVE_STATUSES.has(normalized) || !isPaidPaymentStatus(normalized);
}

function startOfUtcDay(date: Date): Date {
  return new Date(
    Date.UTC(date.getUTCFullYear(), date.getUTCMonth(), date.getUTCDate()),
  );
}

export function parsePaymentDueDate(
  paymentDate: string | null | undefined,
): Date | null {
  if (paymentDate == null || paymentDate.trim() === "") return null;
  const ms = new Date(paymentDate).getTime();
  return Number.isFinite(ms) ? startOfUtcDay(new Date(paymentDate)) : null;
}

export function getPaymentTiming(
  payment: Pick<PaymentResponse, "paymentDate" | "budgetPaymentStatusName">,
  today: Date = new Date(),
): PaymentTiming {
  if (isPaidPaymentStatus(payment.budgetPaymentStatusName)) {
    return "paid";
  }

  const dueDate = parsePaymentDueDate(payment.paymentDate);
  if (!dueDate) return "none";

  const todayUtc = startOfUtcDay(today);
  const diffDays = Math.floor(
    (dueDate.getTime() - todayUtc.getTime()) / (24 * 60 * 60 * 1000),
  );

  if (diffDays < 0) return "overdue";
  if (diffDays <= 7) return "due-soon";
  return "upcoming";
}

export function paymentTimingLabel(timing: PaymentTiming): string {
  switch (timing) {
    case "overdue":
      return "Overdue";
    case "due-soon":
      return "Due soon";
    case "upcoming":
      return "Upcoming";
    case "paid":
      return "Paid";
    default:
      return "No date";
  }
}

export function getPaymentTimingRowClass(timing: PaymentTiming): string {
  switch (timing) {
    case "overdue":
      return "budget-row-overdue";
    case "due-soon":
      return "budget-row-due-soon";
    default:
      return "";
  }
}

export interface PaymentDashboardStats {
  totalBudgeted: number;
  totalPaid: number;
  totalOverdue: number;
  totalDueSoon: number;
  overdueCount: number;
  dueSoonCount: number;
  incomeTotal: number;
  budgetUtilization: number;
  remainingBudget: number;
}

export function computePaymentDashboardStats(
  payments: PaymentResponse[],
  incomeTotal: number,
): PaymentDashboardStats {
  let totalBudgeted = 0;
  let totalPaid = 0;
  let totalOverdue = 0;
  let totalDueSoon = 0;
  let overdueCount = 0;
  let dueSoonCount = 0;

  for (const payment of payments) {
    totalBudgeted += payment.amount;
    totalPaid += payment.paymentMade;

    const timing = getPaymentTiming(payment);
    if (timing === "overdue") {
      overdueCount += 1;
      totalOverdue += payment.amount;
    } else if (timing === "due-soon") {
      dueSoonCount += 1;
      totalDueSoon += payment.amount;
    }
  }

  const budgetUtilization =
    totalBudgeted > 0 ? Math.round((totalPaid / totalBudgeted) * 100) : 0;

  return {
    totalBudgeted,
    totalPaid,
    totalOverdue,
    totalDueSoon,
    overdueCount,
    dueSoonCount,
    incomeTotal,
    budgetUtilization,
    remainingBudget: Math.max(0, totalBudgeted - totalPaid),
  };
}
