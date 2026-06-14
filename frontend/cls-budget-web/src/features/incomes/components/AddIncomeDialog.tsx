"use client";

import { useEffect, useMemo, useState } from "react";
import { Plus, X } from "lucide-react";
import type { BudgetResponse } from "@/features/budgets/types/budget";
import {
  dateInputToIso,
  sortBudgetsByRecent,
  toDateInputValue,
} from "@/features/budgets/utils/budgetFormat";
import type { IncomeSourceResponse } from "@/features/incomes/types/income";
import { incomesApi } from "@/features/incomes/api/incomesApi";
import { Card } from "@/components/ui/Card";
import { ApiError } from "@/lib/api/client";

export function AddIncomeDialog({
  budgets,
  sources,
  initialBudgetId,
  onClose,
  onAdded,
}: {
  budgets: BudgetResponse[];
  sources: IncomeSourceResponse[];
  initialBudgetId?: number | null;
  onClose: () => void;
  onAdded: () => void;
}) {
  const sortedBudgets = useMemo(
    () => sortBudgetsByRecent(budgets),
    [budgets],
  );

  const [budgetId, setBudgetId] = useState<number | "">(
    initialBudgetId ?? sortedBudgets[0]?.budgetId ?? "",
  );
  const [incomeSourceId, setIncomeSourceId] = useState<number | "">(
    sources[0]?.incomeSourceId ?? "",
  );
  const [amount, setAmount] = useState("");
  const [receivedDate, setReceivedDate] = useState("");
  const [notes, setNotes] = useState("");
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const selectedBudget = useMemo(
    () =>
      budgetId === ""
        ? undefined
        : sortedBudgets.find((budget) => budget.budgetId === budgetId),
    [budgetId, sortedBudgets],
  );

  useEffect(() => {
    setBudgetId(initialBudgetId ?? sortedBudgets[0]?.budgetId ?? "");
  }, [initialBudgetId, sortedBudgets]);

  useEffect(() => {
    if (sources.length > 0 && incomeSourceId === "") {
      setIncomeSourceId(sources[0].incomeSourceId);
    }
  }, [sources, incomeSourceId]);

  useEffect(() => {
    if (!selectedBudget) {
      setReceivedDate("");
      return;
    }

    setReceivedDate(toDateInputValue(selectedBudget.startPeriod));
  }, [selectedBudget]);

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
    if (budgetId === "" || incomeSourceId === "" || !receivedDate) return;

    const parsedAmount = Number(amount);
    if (!Number.isFinite(parsedAmount) || parsedAmount < 0) {
      setError("Amount must be zero or greater.");
      return;
    }

    setSubmitting(true);
    setError(null);

    try {
      await incomesApi.create({
        budgetId,
        incomeSourceId,
        amount: parsedAmount,
        receivedDate: dateInputToIso(receivedDate),
        notes: notes.trim() || null,
      });
      onAdded();
      onClose();
    } catch (err) {
      const message =
        err instanceof ApiError
          ? err.errors.length > 0
            ? err.errors.join(" ")
            : err.message
          : err instanceof Error
            ? err.message
            : "Failed to add income";
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
      aria-labelledby="add-income-title"
      onClick={submitting ? undefined : onClose}
    >
      <Card className="w-full max-w-md p-5 shadow-xl">
        <div onClick={(event) => event.stopPropagation()}>
          <div className="mb-4 flex items-start justify-between gap-3">
            <div>
              <h2
                id="add-income-title"
                className="text-lg font-semibold text-[var(--foreground)]"
              >
                Add income
              </h2>
              <p className="mt-1 text-sm text-[var(--muted)]">
                Record income received for a budget period.
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

          {sortedBudgets.length === 0 ? (
            <p className="text-sm text-[var(--muted)]">
              Create a budget before adding income.
            </p>
          ) : sources.length === 0 ? (
            <p className="text-sm text-[var(--muted)]">
              No income sources are available.
            </p>
          ) : (
            <form className="space-y-4" onSubmit={(event) => void handleSubmit(event)}>
              <label className="block text-sm">
                <span className="mb-1 block font-medium text-[var(--foreground)]">
                  Budget
                </span>
                <select
                  value={budgetId}
                  onChange={(event) =>
                    setBudgetId(Number(event.target.value))
                  }
                  disabled={submitting}
                  className="w-full rounded-xl border border-[var(--border)] bg-white px-3 py-2.5 text-sm"
                  required
                >
                  {sortedBudgets.map((budget) => (
                    <option key={budget.budgetId} value={budget.budgetId}>
                      {budget.name}
                    </option>
                  ))}
                </select>
              </label>

              <label className="block text-sm">
                <span className="mb-1 block font-medium text-[var(--foreground)]">
                  Income source
                </span>
                <select
                  value={incomeSourceId}
                  onChange={(event) =>
                    setIncomeSourceId(Number(event.target.value))
                  }
                  disabled={submitting}
                  className="w-full rounded-xl border border-[var(--border)] bg-white px-3 py-2.5 text-sm"
                  required
                >
                  {sources.map((source) => (
                    <option
                      key={source.incomeSourceId}
                      value={source.incomeSourceId}
                    >
                      {source.name}
                    </option>
                  ))}
                </select>
              </label>

              <label className="block text-sm">
                <span className="mb-1 block font-medium text-[var(--foreground)]">
                  Amount
                </span>
                <input
                  type="number"
                  min="0"
                  step="0.01"
                  value={amount}
                  onChange={(event) => setAmount(event.target.value)}
                  disabled={submitting}
                  className="w-full rounded-xl border border-[var(--border)] bg-white px-3 py-2.5 text-sm tabular-nums"
                  placeholder="0.00"
                  required
                />
              </label>

              <label className="block text-sm">
                <span className="mb-1 block font-medium text-[var(--foreground)]">
                  Received date
                </span>
                <input
                  type="date"
                  value={receivedDate}
                  onChange={(event) => setReceivedDate(event.target.value)}
                  disabled={submitting}
                  className="w-full rounded-xl border border-[var(--border)] bg-white px-3 py-2.5 text-sm"
                  required
                />
              </label>

              <label className="block text-sm">
                <span className="mb-1 block font-medium text-[var(--foreground)]">
                  Notes
                  <span className="font-normal text-[var(--muted)]">
                    {" "}
                    (optional)
                  </span>
                </span>
                <textarea
                  value={notes}
                  onChange={(event) => setNotes(event.target.value)}
                  disabled={submitting}
                  rows={3}
                  maxLength={1000}
                  className="w-full resize-none rounded-xl border border-[var(--border)] bg-white px-3 py-2.5 text-sm"
                  placeholder="Paycheck, side gig, refund…"
                />
              </label>

              {error ? (
                <p className="text-sm text-[var(--negative)]">{error}</p>
              ) : null}

              <button
                type="submit"
                disabled={submitting}
                className="inline-flex w-full items-center justify-center gap-2 rounded-full bg-[var(--accent)] px-4 py-2.5 text-sm font-semibold text-white disabled:opacity-50"
              >
                <Plus size={16} aria-hidden />
                {submitting ? "Adding…" : "Add income"}
              </button>
            </form>
          )}
        </div>
      </Card>
    </div>
  );
}
