import type { AccountResponse } from "@/features/accounts/types/account";
import {
  compareAccountCategoryIds,
  getAccountCategoryName,
} from "@/features/accounts/data/accountCategories";
import type { PaymentResponse } from "@/features/payments/types/payment";

export type YtdSpendingByCategory = {
  accountCategoryId: number;
  categoryName: string;
  totalPaid: number;
  paymentCount: number;
  shareOfTotal: number;
};

function paymentSpendDate(payment: PaymentResponse): Date | null {
  const raw = payment.clearedDate || payment.paymentDate;
  if (!raw?.trim()) return null;
  const date = new Date(raw);
  return Number.isFinite(date.getTime()) ? date : null;
}

function isYearToDate(date: Date, today: Date): boolean {
  const year = today.getUTCFullYear();
  const start = Date.UTC(year, 0, 1);
  const end = Date.UTC(
    today.getUTCFullYear(),
    today.getUTCMonth(),
    today.getUTCDate(),
    23,
    59,
    59,
    999,
  );
  const ms = date.getTime();
  return ms >= start && ms <= end;
}

/**
 * Sums PaymentMade for payments dated in the current calendar year,
 * grouped by the linked account's category.
 */
export function computeYtdSpendingByCategory(
  payments: PaymentResponse[],
  accounts: AccountResponse[],
  today: Date = new Date(),
): { rows: YtdSpendingByCategory[]; totalPaid: number; year: number } {
  const accountById = new Map(
    accounts.map((account) => [account.accountId, account] as const),
  );

  const totals = new Map<
    number,
    { categoryName: string; totalPaid: number; paymentCount: number }
  >();

  for (const payment of payments) {
    if (!(payment.paymentMade > 0)) continue;
    const spendDate = paymentSpendDate(payment);
    if (!spendDate || !isYearToDate(spendDate, today)) continue;

    const account = accountById.get(payment.accountId);
    const categoryId = account?.accountCategoryId ?? 0;
    const categoryName =
      account?.accountCategoryName?.trim() ||
      (categoryId > 0
        ? getAccountCategoryName(categoryId)
        : "Uncategorized");

    const existing = totals.get(categoryId);
    if (existing) {
      existing.totalPaid += payment.paymentMade;
      existing.paymentCount += 1;
    } else {
      totals.set(categoryId, {
        categoryName,
        totalPaid: payment.paymentMade,
        paymentCount: 1,
      });
    }
  }

  const totalPaid = [...totals.values()].reduce(
    (sum, row) => sum + row.totalPaid,
    0,
  );

  const rows = [...totals.entries()]
    .map(([accountCategoryId, row]) => ({
      accountCategoryId,
      categoryName: row.categoryName,
      totalPaid: row.totalPaid,
      paymentCount: row.paymentCount,
      shareOfTotal: totalPaid > 0 ? row.totalPaid / totalPaid : 0,
    }))
    .sort((a, b) => {
      const amountDiff = b.totalPaid - a.totalPaid;
      if (amountDiff !== 0) return amountDiff;
      return compareAccountCategoryIds(
        a.accountCategoryId,
        b.accountCategoryId,
        a.categoryName,
        b.categoryName,
      );
    });

  return {
    rows,
    totalPaid,
    year: today.getUTCFullYear(),
  };
}
