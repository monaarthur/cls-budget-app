"use client";

import { useEffect, useMemo, useState } from "react";
import { Plus, X } from "lucide-react";
import { budgetsApi } from "@/features/budgets/api/budgetsApi";
import { getAccountCategoryName } from "@/features/accounts/data/accountCategories";
import type { AccountResponse } from "@/features/accounts/types/account";
import { Card } from "@/components/ui/Card";
import { ApiError } from "@/lib/api/client";

export function AddBudgetAccountDialog({
  budgetId,
  accounts,
  includedAccountIds,
  onClose,
  onAdded,
}: {
  budgetId: number;
  accounts: AccountResponse[];
  includedAccountIds: Set<number>;
  onClose: () => void;
  onAdded: (accountName: string) => void;
}) {
  const availableAccounts = useMemo(
    () =>
      accounts
        .filter((account) => !includedAccountIds.has(account.accountId))
        .sort((a, b) => a.name.localeCompare(b.name)),
    [accounts, includedAccountIds],
  );

  const [accountId, setAccountId] = useState<number | "">(
    availableAccounts[0]?.accountId ?? "",
  );
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    setAccountId(availableAccounts[0]?.accountId ?? "");
  }, [availableAccounts]);

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
    if (accountId === "") return;

    setSubmitting(true);
    setError(null);

    try {
      await budgetsApi.addAccount(budgetId, accountId);
      const added = accounts.find((account) => account.accountId === accountId);
      onAdded(added?.name ?? "Account");
      onClose();
    } catch (err) {
      const message =
        err instanceof ApiError
          ? err.errors.length > 0
            ? err.errors.join(" ")
            : err.message
          : err instanceof Error
            ? err.message
            : "Failed to add account";
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
      aria-labelledby="add-budget-account-title"
      onClick={submitting ? undefined : onClose}
    >
      <Card className="w-full max-w-md p-5 shadow-xl">
        <div onClick={(event) => event.stopPropagation()}>
        <div className="mb-4 flex items-start justify-between gap-3">
          <div>
            <h2
              id="add-budget-account-title"
              className="text-lg font-semibold text-[var(--foreground)]"
            >
              Add account to budget
            </h2>
            <p className="mt-1 text-sm text-[var(--muted)]">
              Choose an account that is not already in this budget.
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

        {availableAccounts.length === 0 ? (
          <p className="text-sm text-[var(--muted)]">
            All accounts are already included in this budget.
          </p>
        ) : (
          <form onSubmit={(event) => void handleSubmit(event)} className="space-y-4">
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
                {availableAccounts.map((account) => (
                  <option key={account.accountId} value={account.accountId}>
                    {account.name} · {getAccountCategoryName(account.accountCategoryId)}
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
                disabled={submitting || accountId === ""}
                className="inline-flex items-center gap-2 rounded-full bg-[var(--link)] px-4 py-2 text-sm font-semibold text-white disabled:opacity-40"
              >
                <Plus size={15} aria-hidden />
                {submitting ? "Adding…" : "Add account"}
              </button>
            </div>
          </form>
        )}
        </div>
      </Card>
    </div>
  );
}
