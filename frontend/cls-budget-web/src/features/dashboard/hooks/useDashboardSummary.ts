"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import { accountsApi } from "@/features/accounts/api/accountsApi";
import type { AccountResponse } from "@/features/accounts/types/account";
import { budgetsApi } from "@/features/budgets/api/budgetsApi";
import type { BudgetResponse } from "@/features/budgets/types/budget";
import { sortBudgetsByRecent } from "@/features/budgets/utils/budgetFormat";
import { incomesApi } from "@/features/incomes/api/incomesApi";
import { paymentsApi } from "@/features/payments/api/paymentsApi";
import {
  computePaymentDashboardStats,
  getPaymentTiming,
  type PaymentTiming,
} from "@/features/payments/utils/paymentTiming";
import type { PaymentResponse } from "@/features/payments/types/payment";
import { ApiError } from "@/lib/api/client";

export type DashboardPaymentPreview = PaymentResponse & {
  accountName: string;
  timing: PaymentTiming;
};

export function useDashboardSummary() {
  const [accounts, setAccounts] = useState<AccountResponse[]>([]);
  const [budgets, setBudgets] = useState<BudgetResponse[]>([]);
  const [payments, setPayments] = useState<PaymentResponse[]>([]);
  const [incomeTotal, setIncomeTotal] = useState(0);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);

    try {
      const [accountsResult, budgetsResult, paymentsResult, incomesResult] =
        await Promise.all([
          accountsApi.getAll(),
          budgetsApi.getAll(),
          paymentsApi.getAll(),
          incomesApi.getAll(),
        ]);

      const loadedAccounts = accountsResult.data ?? [];
      const loadedBudgets = sortBudgetsByRecent(budgetsResult.data ?? []);
      const loadedPayments = paymentsResult.data ?? [];
      const loadedIncomes = incomesResult.data ?? [];

      setAccounts(loadedAccounts);
      setBudgets(loadedBudgets);
      setPayments(loadedPayments);
      setIncomeTotal(
        loadedIncomes.reduce((sum, income) => sum + income.amount, 0),
      );
    } catch (err) {
      const message =
        err instanceof ApiError
          ? err.message
          : err instanceof Error
            ? err.message
            : "Failed to load dashboard";
      setError(message);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void load();
  }, [load]);

  const currentBudget = budgets[0] ?? null;

  const currentBudgetPayments = useMemo(
    () =>
      currentBudget
        ? payments.filter((payment) => payment.budgetId === currentBudget.budgetId)
        : payments,
    [currentBudget, payments],
  );

  const stats = useMemo(
    () => computePaymentDashboardStats(currentBudgetPayments, incomeTotal),
    [currentBudgetPayments, incomeTotal],
  );

  const accountNameById = useMemo(
    () => new Map(accounts.map((account) => [account.accountId, account.name])),
    [accounts],
  );

  const upcomingPayments = useMemo((): DashboardPaymentPreview[] => {
    return payments
      .map((payment) => ({
        ...payment,
        accountName:
          accountNameById.get(payment.accountId) ??
          `Account ${payment.accountId}`,
        timing: getPaymentTiming(payment),
      }))
      .filter(
        (payment) =>
          payment.timing === "overdue" || payment.timing === "due-soon",
      )
      .sort((a, b) => {
        const priority = (timing: PaymentTiming) =>
          timing === "overdue" ? 0 : 1;
        const priorityDiff = priority(a.timing) - priority(b.timing);
        if (priorityDiff !== 0) return priorityDiff;
        return (
          new Date(a.paymentDate ?? 0).getTime() -
          new Date(b.paymentDate ?? 0).getTime()
        );
      })
      .slice(0, 6);
  }, [accountNameById, payments]);

  const totalBalance = useMemo(
    () => accounts.reduce((sum, account) => sum + account.balance, 0),
    [accounts],
  );

  const monthlyPayments = useMemo(
    () =>
      accounts.reduce(
        (sum, account) => sum + (account.monthlyPayment ?? 0),
        0,
      ),
    [accounts],
  );

  return {
    accounts,
    budgets,
    currentBudget,
    stats,
    totalBalance,
    monthlyPayments,
    upcomingPayments,
    loading,
    error,
    reload: load,
  };
}
