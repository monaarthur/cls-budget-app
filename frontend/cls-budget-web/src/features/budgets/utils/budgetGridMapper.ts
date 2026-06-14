import { compareAccountCategoryIds } from "@/features/accounts/data/accountCategories";
import { isPaymentDateSelected } from "@/features/budgets/utils/budgetPaymentSummary";
import type { AccountResponse } from "@/features/accounts/types/account";
import type { PaymentResponse, UpdatePaymentRequest } from "@/features/payments/types/payment";
import {
  getPaymentTiming,
  getPaymentTimingRowClass,
} from "@/features/payments/utils/paymentTiming";

export interface BudgetGridRow extends PaymentResponse {
  accountName: string;
  accountNumber: string;
  accountCategoryId: number;
  accountBalance: number;
  accountMonthlyPayment: number | null;
  accountPaymentDay: number | null;
}

export function buildBudgetGridRows(
  payments: PaymentResponse[],
  accountsById: Map<number, AccountResponse>,
): BudgetGridRow[] {
  return sortBudgetPaymentRows(
    payments.map((payment) => {
      const account = accountsById.get(payment.accountId);
      return {
        ...payment,
        accountName: account?.name ?? `Account ${payment.accountId}`,
        accountNumber: account?.number ?? "",
        accountCategoryId: account?.accountCategoryId ?? 0,
        accountBalance: account?.balance ?? 0,
        accountMonthlyPayment: account?.monthlyPayment ?? null,
        accountPaymentDay: account?.paymentDay ?? null,
      };
    }),
  );
}

export function sortBudgetPaymentRows(rows: BudgetGridRow[]): BudgetGridRow[] {
  return [...rows].sort((a, b) => {
    const aHasDate = isPaymentDateSelected(a.paymentDate);
    const bHasDate = isPaymentDateSelected(b.paymentDate);
    if (aHasDate !== bHasDate) return aHasDate ? -1 : 1;

    if (aHasDate && bHasDate) {
      const dateA = new Date(a.paymentDate!).getTime();
      const dateB = new Date(b.paymentDate!).getTime();
      if (dateA !== dateB) return dateA - dateB;
    }

    const categoryCompare = compareAccountCategoryIds(
      a.accountCategoryId,
      b.accountCategoryId,
      a.accountName,
      b.accountName,
    );
    if (categoryCompare !== 0) return categoryCompare;

    return a.budgetPaymentId - b.budgetPaymentId;
  });
}

export function defaultPaymentDateForAccount(
  account: AccountResponse,
  budgetStartPeriod: string,
): string {
  const start = new Date(budgetStartPeriod);
  const year = start.getUTCFullYear();
  const month = start.getUTCMonth();
  const day = account.paymentDay ?? start.getUTCDate();
  const lastDayOfMonth = new Date(Date.UTC(year, month + 1, 0)).getUTCDate();
  const clampedDay = Math.min(Math.max(day, 1), lastDayOfMonth);
  return new Date(Date.UTC(year, month, clampedDay)).toISOString();
}

export function isFirstPaymentRowForAccount(
  row: BudgetGridRow,
  rows: BudgetGridRow[],
): boolean {
  let earliest: BudgetGridRow | null = null;

  for (const candidate of rows) {
    if (candidate.accountId !== row.accountId) continue;

    const candidateTime = candidate.paymentDate
      ? new Date(candidate.paymentDate).getTime()
      : Number.POSITIVE_INFINITY;
    const earliestTime = earliest?.paymentDate
      ? new Date(earliest.paymentDate).getTime()
      : Number.POSITIVE_INFINITY;

    if (
      !earliest ||
      candidateTime < earliestTime ||
      (candidate.paymentDate === earliest.paymentDate &&
        candidate.budgetPaymentId < earliest.budgetPaymentId)
    ) {
      earliest = candidate;
    }
  }

  return earliest?.budgetPaymentId === row.budgetPaymentId;
}

export function calculateOwed(
  amount: number | null | undefined,
  paymentMade: number | null | undefined,
): number {
  return (Number(amount) || 0) - (Number(paymentMade) || 0);
}

export function needsPaymentSourceUpdate(
  row: Pick<BudgetGridRow, "paymentMade" | "paymentSourceId"> | null | undefined,
): boolean {
  if (!row) return false;
  return row.paymentMade > 0 && row.paymentSourceId == null;
}

export function toUpdatePaymentRequest(row: BudgetGridRow): UpdatePaymentRequest {
  return {
    budgetId: row.budgetId,
    accountId: row.accountId,
    paymentMade: row.paymentMade,
    amount: row.amount,
    budgetPaymentStatusId: row.budgetPaymentStatusId,
    isCleared: row.isCleared,
    paymentDate: row.paymentDate,
    clearedDate: row.clearedDate,
    paymentSourceId: row.paymentSourceId,
    incomeSourceId: row.incomeSourceId ?? null,
  };
}

export function getBudgetPaymentRowClass(row: BudgetGridRow): string {
  const timingClass = getPaymentTimingRowClass(getPaymentTiming(row));
  if (timingClass) return timingClass;
  return getBudgetStatusRowClass(row.budgetPaymentStatusName);
}

export function getBudgetStatusRowClass(statusName: string): string {
  switch (statusName.trim().toLowerCase()) {
    case "pending":
    case "scheduled":
      return "budget-row-scheduled";
    case "paid":
      return "budget-row-paid";
    default:
      return "budget-row-alert";
  }
}
