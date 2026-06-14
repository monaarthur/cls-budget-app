"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import { useAccounts } from "@/features/accounts/hooks/useAccounts";
import { useBudgets } from "@/features/budgets/hooks/useBudgets";
import { formatBudgetMonthYear } from "@/features/budgets/utils/budgetFormat";
import { paymentsApi } from "@/features/payments/api/paymentsApi";
import type { PaymentResponse } from "@/features/payments/types/payment";
import {
  getPaymentTiming,
  isPaidPaymentStatus,
  paymentTimingLabel,
  type PaymentTiming,
} from "@/features/payments/utils/paymentTiming";
import { ApiError } from "@/lib/api/client";

export type PaymentListItem = PaymentResponse & {
  accountName: string;
  budgetName: string;
  timing: PaymentTiming;
};

export function usePayments() {
  const { accounts } = useAccounts();
  const { budgets } = useBudgets();
  const [payments, setPayments] = useState<PaymentResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const result = await paymentsApi.getAll();
      setPayments(result.data ?? []);
    } catch (err) {
      const message =
        err instanceof ApiError
          ? err.message
          : err instanceof Error
            ? err.message
            : "Failed to load payments";
      setError(message);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void load();
  }, [load]);

  const accountNameById = useMemo(
    () => new Map(accounts.map((a) => [a.accountId, a.name])),
    [accounts],
  );

  const budgetNameById = useMemo(
    () =>
      new Map(
        budgets.map((b) => [b.budgetId, b.name || formatBudgetMonthYear(b.startPeriod)]),
      ),
    [budgets],
  );

  const items = useMemo((): PaymentListItem[] => {
    return payments
      .map((payment) => ({
        ...payment,
        accountName:
          accountNameById.get(payment.accountId) ??
          `Account ${payment.accountId}`,
        budgetName:
          budgetNameById.get(payment.budgetId) ??
          `Budget ${payment.budgetId}`,
        timing: getPaymentTiming(payment),
      }))
      .sort(
        (a, b) =>
          new Date(b.paymentDate ?? 0).getTime() -
          new Date(a.paymentDate ?? 0).getTime(),
      );
  }, [accountNameById, budgetNameById, payments]);

  return { items, loading, error, reload: load };
}

export function filterPayments(
  items: PaymentListItem[],
  options: {
    statusFilter: string;
    timingFilter: string;
    searchQuery: string;
  },
): PaymentListItem[] {
  const query = options.searchQuery.trim().toLowerCase();

  return items.filter((item) => {
    if (options.statusFilter !== "all") {
      if (
        item.budgetPaymentStatusName.toLowerCase() !==
        options.statusFilter.toLowerCase()
      ) {
        return false;
      }
    }

    if (options.timingFilter !== "all" && item.timing !== options.timingFilter) {
      return false;
    }

    if (query) {
      const haystack = [
        item.accountName,
        item.budgetName,
        item.budgetPaymentStatusName,
        paymentTimingLabel(item.timing),
      ]
        .join(" ")
        .toLowerCase();
      if (!haystack.includes(query)) return false;
    }

    return true;
  });
}

export { isPaidPaymentStatus, paymentTimingLabel };
