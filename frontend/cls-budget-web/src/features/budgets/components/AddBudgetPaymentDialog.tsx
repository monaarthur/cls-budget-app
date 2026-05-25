"use client";

import { useEffect, useMemo, useState } from "react";
import { Plus, X } from "lucide-react";
import { getAccountCategoryName } from "@/features/accounts/data/accountCategories";
import type { AccountResponse } from "@/features/accounts/types/account";
import { defaultPaymentDateForAccount } from "@/features/budgets/utils/budgetGridMapper";
import { dateInputToIso, toDateInputValue } from "@/features/budgets/utils/budgetFormat";
import { paymentsApi } from "@/features/payments/api/paymentsApi";
import type { BudgetPaymentStatusResponse } from "@/features/payments/types/payment";
import { Card } from "@/components/ui/Card";
import { ApiError } from "@/lib/api/client";

const DEFAULT_STATUS_NAME = "Scheduled";

export function AddBudgetPaymentDialog({
  budgetId,
  budgetStartPeriod,
  accounts,
  statuses,
  initialAccountId,
  onClose,
  onAdded,
}: {
  budgetId: number;
  budgetStartPeriod: string;
  accounts: AccountResponse[];
  statuses: BudgetPaymentStatusResponse[];
  initialAccountId?: number;
  onClose: () => void;
  onAdded: (accountName: string) => void;
}) {
  const sortedAccounts = useMemo(
    () => [...accounts].sort((a, b) => a.name.localeCompare(b.name)),
    [accounts],
  );

  const defaultStatusId = useMemo(() => {
    const scheduled = statuses.find(
      (status) => status.name.toLowerCase() === DEFAULT_STATUS_NAME.toLowerCase(),
    );
    return scheduled?.budgetPaymentStatusId ?? statuses[0]?.budgetPaymentStatusId ?? 0;
  }, [statuses]);

  const [accountId, setAccountId] = useState<number | "">(
    initialAccountId ?? sortedAccounts[0]?.accountId ?? "",
  );
  const [paymentDate, setPaymentDate] = useState("");
  const [amount, setAmount] = useState("");
  const [paymentMade, setPaymentMade] = useState("0");
  const [budgetPaymentStatusId, setBudgetPaymentStatusId] = useState<number>(
    defaultStatusId,
  );
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const selectedAccount = useMemo(
    () =>
      accountId === ""
        ? undefined
        : sortedAccounts.find((account) => account.accountId === accountId),
    [accountId, sortedAccounts],
  );

  useEffect(() => {
    setAccountId(initialAccountId ?? sortedAccounts[0]?.accountId ?? "");
  }, [initialAccountId, sortedAccounts]);

  useEffect(() => {
    setBudgetPaymentStatusId(defaultStatusId);
  }, [defaultStatusId]);

  useEffect(() => {
    if (!selectedAccount || !budgetStartPeriod) {
      setPaymentDate("");
      return;
    }

    setPaymentDate(
      toDateInputValue(
        defaultPaymentDateForAccount(selectedAccount, budgetStartPeriod),
      ),
    );
    setAmount(
      selectedAccount.monthlyPayment != null
        ? String(selectedAccount.monthlyPayment)
        : "0",
    );
  }, [selectedAccount, budgetStartPeriod]);

  useEffect(() => {
    function onKeyDown(event: KeyboardEvent) {
      if (event.key === "Escape" && !submitting) {
        onClose();
      }
    }

    document.addEventListener("keydown", onKeyDown);
    return () => document.removeEventListener("keydown", onKeyDown);
  }, [onClose, submitting]);

  async function handleSubmit(event: React.FormEvent) {
    event.preventDefault();
    if (accountId === "" || !paymentDate || budgetPaymentStatusId <= 0) return;

    const parsedAmount = Number(amount);
    const parsedPaymentMade = Number(paymentMade);
    if (!Number.isFinite(parsedAmount) || parsedAmount < 0) {
      setError("Budgeted amount must be zero or greater.");
      return;
    }
    if (!Number.isFinite(parsedPaymentMade) || parsedPaymentMade < 0) {
      setError("Paid amount must be zero or greater.");
      return;
    }

    setSubmitting(true);
    setError(null);

    try {
      await paymentsApi.create({
        budgetId,
        accountId,
        amount: parsedAmount,
        paymentMade: parsedPaymentMade,
        budgetPaymentStatusId,
        isCleared: false,
        paymentDate: dateInputToIso(paymentDate),
      });
      onAdded(selectedAccount?.name ?? "Account");
      onClose();
    } catch (err) {
      const message =
        err instanceof ApiError
          ? err.errors.length > 0
            ? err.errors.join(" ")
            : err.message
          : err instanceof Error
            ? err.message
            : "Failed to add payment";
      setError(message);
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4"
      role="dialog"
      aria-modal="true"
      aria-labelledby="add-budget-payment-title"
      onClick={submitting ? undefined : onClose}
    >
      <Card className="w-full max-w-md p-5 shadow-xl">
        <div onClick={(event) => event.stopPropagation()}>
          <div className="mb-4 flex items-start justify-between gap-3">
            <div>
              <h2
                id="add-budget-payment-title"
                className="text-lg font-semibold text-[var(--foreground)]"
              >
                Add budget payment
              </h2>
              <p className="mt-1 text-sm text-[var(--muted)]">
                Create another payment for an account already in this budget.
              </p>
            </div>
            <button
              type="button"
              onClick={onClose}
              disabled={submitting}
              className="rounded-lg p-1 text-[var(--muted)] hover:bg-black/[0.04] disabled:opacity-40"
              aria-label="Close"
            >
              <X size={18} />
            </button>
          </div>

          {sortedAccounts.length === 0 ? (
            <p className="text-sm text-[var(--muted)]">
              Add an account to this budget before creating payments.
            </p>
          ) : (
            <form
              onSubmit={(event) => void handleSubmit(event)}
              className="space-y-4"
            >
              <label className="block text-sm">
                <span className="mb-1.5 block font-medium">Account</span>
                <select
                  value={accountId}
                  onChange={(event) =>
                    setAccountId(
                      event.target.value === ""
                        ? ""
                        : Number(event.target.value),
                    )
                  }
                  disabled={submitting}
                  className="w-full rounded-xl border border-[var(--border)] bg-white px-3 py-2.5 text-sm"
                >
                  {sortedAccounts.map((account) => (
                    <option key={account.accountId} value={account.accountId}>
                      {account.name} ·{" "}
                      {getAccountCategoryName(account.accountCategoryId)}
                    </option>
                  ))}
                </select>
              </label>

              <label className="block text-sm">
                <span className="mb-1.5 block font-medium">Payment date</span>
                <input
                  type="date"
                  value={paymentDate}
                  onChange={(event) => setPaymentDate(event.target.value)}
                  disabled={submitting}
                  required
                  className="w-full rounded-xl border border-[var(--border)] bg-white px-3 py-2.5 text-sm"
                />
              </label>

              <div className="grid gap-4 sm:grid-cols-2">
                <label className="block text-sm">
                  <span className="mb-1.5 block font-medium">Budgeted</span>
                  <input
                    type="number"
                    min="0"
                    step="0.01"
                    value={amount}
                    onChange={(event) => setAmount(event.target.value)}
                    disabled={submitting}
                    required
                    className="w-full rounded-xl border border-[var(--border)] bg-white px-3 py-2.5 text-sm"
                  />
                </label>

                <label className="block text-sm">
                  <span className="mb-1.5 block font-medium">Paid</span>
                  <input
                    type="number"
                    min="0"
                    step="0.01"
                    value={paymentMade}
                    onChange={(event) => setPaymentMade(event.target.value)}
                    disabled={submitting}
                    required
                    className="w-full rounded-xl border border-[var(--border)] bg-white px-3 py-2.5 text-sm"
                  />
                </label>
              </div>

              <label className="block text-sm">
                <span className="mb-1.5 block font-medium">Status</span>
                <select
                  value={budgetPaymentStatusId}
                  onChange={(event) =>
                    setBudgetPaymentStatusId(Number(event.target.value))
                  }
                  disabled={submitting}
                  className="w-full rounded-xl border border-[var(--border)] bg-white px-3 py-2.5 text-sm"
                >
                  {statuses.map((status) => (
                    <option
                      key={status.budgetPaymentStatusId}
                      value={status.budgetPaymentStatusId}
                    >
                      {status.name}
                    </option>
                  ))}
                </select>
              </label>

              {error ? (
                <p className="rounded-xl border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-900">
                  {error}
                </p>
              ) : null}

              <div className="flex justify-end gap-2">
                <button
                  type="button"
                  onClick={onClose}
                  disabled={submitting}
                  className="rounded-full border border-[var(--border)] px-4 py-2 text-sm font-medium disabled:opacity-40"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  disabled={
                    submitting ||
                    accountId === "" ||
                    !paymentDate ||
                    budgetPaymentStatusId <= 0
                  }
                  className="inline-flex items-center gap-2 rounded-full bg-[var(--link)] px-4 py-2 text-sm font-semibold text-white disabled:opacity-40"
                >
                  <Plus size={15} aria-hidden />
                  {submitting ? "Adding…" : "Add payment"}
                </button>
              </div>
            </form>
          )}
        </div>
      </Card>
    </div>
  );
}
