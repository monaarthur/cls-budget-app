"use client";

import { useEffect, useState } from "react";
import { Copy, X } from "lucide-react";
import { budgetsApi } from "@/features/budgets/api/budgetsApi";
import { getNextBudgetPeriod, toDateInputValue, dateInputToIso } from "@/features/budgets/utils/budgetFormat";
import type { BudgetResponse } from "@/features/budgets/types/budget";
import { Card } from "@/components/ui/Card";
import { ApiError } from "@/lib/api/client";

export function CopyBudgetDialog({
  budget,
  onClose,
  onCopied,
}: {
  budget: BudgetResponse;
  onClose: () => void;
  onCopied: (name: string) => void;
}) {
  const defaults = getNextBudgetPeriod(budget.startPeriod);
  const [name, setName] = useState(defaults.name);
  const [startDate, setStartDate] = useState(
    toDateInputValue(defaults.startPeriod),
  );
  const [endDate, setEndDate] = useState(toDateInputValue(defaults.endPeriod));
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

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
    const trimmedName = name.trim();

    if (endDate < startDate) {
      setError("End date must be on or after the start date.");
      return;
    }

    setSubmitting(true);
    setError(null);

    try {
      await budgetsApi.copy(budget.budgetId, {
        name: trimmedName,
        startPeriod: dateInputToIso(startDate),
        endPeriod: dateInputToIso(endDate),
      });
      onCopied(trimmedName);
      onClose();
    } catch (err) {
      const message =
        err instanceof ApiError
          ? err.errors.length > 0
            ? err.errors.join(" ")
            : err.message
          : err instanceof Error
            ? err.message
            : "Failed to copy budget";
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
      aria-labelledby="copy-budget-title"
      onClick={(event) => {
        if (event.target === event.currentTarget && !submitting) {
          onClose();
        }
      }}
    >
      <Card className="w-full max-w-md p-6 shadow-xl">
        <div className="flex items-start justify-between gap-3">
          <div>
            <h2 id="copy-budget-title" className="text-lg font-semibold">
              Copy budget
            </h2>
            <p className="mt-1 text-sm text-[var(--muted)]">
              Create a new budget from &ldquo;{budget.name}&rdquo; with its
              accounts and payment amounts.
            </p>
          </div>
          <button
            type="button"
            onClick={onClose}
            className="rounded-full p-1 text-[var(--muted)] hover:bg-[var(--border)]"
            aria-label="Close"
          >
            <X className="h-5 w-5" />
          </button>
        </div>

        <form onSubmit={(e) => void handleSubmit(e)} className="mt-5 space-y-4">
          <label className="block">
            <span className="text-sm font-medium">Name</span>
            <input
              type="text"
              required
              value={name}
              onChange={(e) => setName(e.target.value)}
              className="mt-1 w-full rounded-xl border border-[var(--border)] bg-white px-3 py-2 text-sm outline-none focus:border-[var(--link)]"
            />
          </label>

          <div className="grid grid-cols-2 gap-3">
            <label className="block">
              <span className="text-sm font-medium">Start</span>
              <input
                type="date"
                required
                value={startDate}
                onChange={(e) => setStartDate(e.target.value)}
                className="mt-1 w-full rounded-xl border border-[var(--border)] bg-white px-3 py-2 text-sm outline-none focus:border-[var(--link)]"
              />
            </label>
            <label className="block">
              <span className="text-sm font-medium">End</span>
              <input
                type="date"
                required
                value={endDate}
                onChange={(e) => setEndDate(e.target.value)}
                className="mt-1 w-full rounded-xl border border-[var(--border)] bg-white px-3 py-2 text-sm outline-none focus:border-[var(--link)]"
              />
            </label>
          </div>

          {error ? (
            <p className="text-sm text-[var(--negative)]">{error}</p>
          ) : null}

          <div className="flex justify-end gap-2 pt-1">
            <button
              type="button"
              onClick={onClose}
              disabled={submitting}
              className="rounded-full px-4 py-2 text-sm font-medium text-[var(--muted)] hover:bg-[var(--border)]"
            >
              Cancel
            </button>
            <button
              type="submit"
              disabled={submitting || !name.trim()}
              className="inline-flex items-center gap-2 rounded-full bg-[var(--link)] px-4 py-2 text-sm font-medium text-white disabled:opacity-50"
            >
              <Copy className="h-4 w-4" />
              {submitting ? "Copying…" : "Copy budget"}
            </button>
          </div>
        </form>
      </Card>
    </div>
  );
}
