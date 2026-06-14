"use client";

import { useEffect, useState } from "react";
import { Save, Trash2, X } from "lucide-react";
import type { BudgetResponse } from "@/features/budgets/types/budget";
import { dateInputToIso, toDateInputValue } from "@/features/budgets/utils/budgetFormat";
import { incomesApi } from "@/features/incomes/api/incomesApi";
import type {
  IncomeResponse,
  IncomeSourceResponse,
} from "@/features/incomes/types/income";
import { Card } from "@/components/ui/Card";
import { ApiError } from "@/lib/api/client";

export function EditIncomeDialog({
  income,
  budgets,
  sources,
  onClose,
  onSaved,
  onDeleted,
}: {
  income: IncomeResponse;
  budgets: BudgetResponse[];
  sources: IncomeSourceResponse[];
  onClose: () => void;
  onSaved: () => void;
  onDeleted: () => void;
}) {
  const [budgetId, setBudgetId] = useState(income.budgetId);
  const [incomeSourceId, setIncomeSourceId] = useState(income.incomeSourceId);
  const [amount, setAmount] = useState(String(income.amount));
  const [receivedDate, setReceivedDate] = useState(
    toDateInputValue(income.receivedDate),
  );
  const [notes, setNotes] = useState(income.notes ?? "");
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    function onKeyDown(event: KeyboardEvent) {
      if (event.key === "Escape" && !submitting) onClose();
    }
    document.addEventListener("keydown", onKeyDown);
    return () => document.removeEventListener("keydown", onKeyDown);
  }, [onClose, submitting]);

  async function handleSave(event: React.FormEvent) {
    event.preventDefault();
    const parsedAmount = Number(amount);
    if (!Number.isFinite(parsedAmount) || parsedAmount < 0) {
      setError("Amount must be zero or greater.");
      return;
    }

    setSubmitting(true);
    setError(null);
    try {
      await incomesApi.update(income.budgetIncomeId, {
        budgetId,
        incomeSourceId,
        amount: parsedAmount,
        receivedDate: dateInputToIso(receivedDate),
        notes: notes.trim() || null,
      });
      onSaved();
      onClose();
    } catch (err) {
      const message =
        err instanceof ApiError
          ? err.errors.join(", ") || err.message
          : err instanceof Error
            ? err.message
            : "Failed to update income";
      setError(message);
    } finally {
      setSubmitting(false);
    }
  }

  async function handleDelete() {
    if (!window.confirm("Delete this income entry?")) return;
    setSubmitting(true);
    setError(null);
    try {
      await incomesApi.remove(income.budgetIncomeId);
      onDeleted();
      onClose();
    } catch (err) {
      const message =
        err instanceof ApiError
          ? err.message
          : err instanceof Error
            ? err.message
            : "Failed to delete income";
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
      onClick={submitting ? undefined : onClose}
    >
      <Card className="w-full max-w-md p-5 shadow-xl">
        <div onClick={(event) => event.stopPropagation()}>
          <div className="mb-4 flex items-start justify-between gap-3">
            <h2 className="text-lg font-semibold">Edit income</h2>
            <button type="button" onClick={onClose} aria-label="Close">
              <X size={18} />
            </button>
          </div>

          <form className="space-y-3" onSubmit={(event) => void handleSave(event)}>
            <label className="block text-sm">
              <span className="mb-1 block font-medium">Budget</span>
              <select
                value={budgetId}
                onChange={(e) => setBudgetId(Number(e.target.value))}
                className="w-full rounded-xl border border-[var(--border)] px-3 py-2"
              >
                {budgets.map((budget) => (
                  <option key={budget.budgetId} value={budget.budgetId}>
                    {budget.name}
                  </option>
                ))}
              </select>
            </label>
            <label className="block text-sm">
              <span className="mb-1 block font-medium">Income source</span>
              <select
                value={incomeSourceId}
                onChange={(e) => setIncomeSourceId(Number(e.target.value))}
                className="w-full rounded-xl border border-[var(--border)] px-3 py-2"
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
              <span className="mb-1 block font-medium">Amount</span>
              <input
                type="number"
                min="0"
                step="0.01"
                value={amount}
                onChange={(e) => setAmount(e.target.value)}
                className="w-full rounded-xl border border-[var(--border)] px-3 py-2"
                required
              />
            </label>
            <label className="block text-sm">
              <span className="mb-1 block font-medium">Received date</span>
              <input
                type="date"
                value={receivedDate}
                onChange={(e) => setReceivedDate(e.target.value)}
                className="w-full rounded-xl border border-[var(--border)] px-3 py-2"
                required
              />
            </label>
            <label className="block text-sm">
              <span className="mb-1 block font-medium">Notes</span>
              <textarea
                value={notes}
                onChange={(e) => setNotes(e.target.value)}
                rows={3}
                className="w-full resize-none rounded-xl border border-[var(--border)] px-3 py-2"
              />
            </label>
            {error ? <p className="text-sm text-[var(--negative)]">{error}</p> : null}
            <div className="flex gap-2">
              <button
                type="submit"
                disabled={submitting}
                className="inline-flex flex-1 items-center justify-center gap-2 rounded-full bg-[var(--accent)] px-4 py-2.5 text-sm font-semibold text-white disabled:opacity-50"
              >
                <Save size={16} aria-hidden />
                Save
              </button>
              <button
                type="button"
                onClick={() => void handleDelete()}
                disabled={submitting}
                className="inline-flex items-center justify-center gap-2 rounded-full border border-red-200 px-4 py-2.5 text-sm font-medium text-red-700 disabled:opacity-50"
              >
                <Trash2 size={16} aria-hidden />
                Delete
              </button>
            </div>
          </form>
        </div>
      </Card>
    </div>
  );
}
