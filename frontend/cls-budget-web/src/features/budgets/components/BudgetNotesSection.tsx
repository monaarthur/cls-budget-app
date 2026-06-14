"use client";

import { useState } from "react";
import { Plus, X } from "lucide-react";
import { budgetsApi } from "@/features/budgets/api/budgetsApi";
import type { UpdateBudgetRequest } from "@/features/budgets/types/budget";
import { serializeBudgetNotes } from "@/features/budgets/utils/budgetNotes";
import { ApiError } from "@/lib/api/client";

export function BudgetNotesSection({
  budgetId,
  notes,
  onNotesChange,
  buildUpdateRequest,
  disabled = false,
  onStatus,
}: {
  budgetId: number;
  notes: string[];
  onNotesChange: (notes: string[]) => void;
  buildUpdateRequest: (notes: string[]) => UpdateBudgetRequest;
  disabled?: boolean;
  onStatus?: (status: { type: "success" | "error"; message: string } | null) => void;
}) {
  const [draft, setDraft] = useState("");
  const [saving, setSaving] = useState(false);

  async function persistNotes(nextNotes: string[]) {
    setSaving(true);
    onStatus?.(null);

    try {
      await budgetsApi.update(budgetId, buildUpdateRequest(nextNotes));
      onNotesChange(nextNotes);
      onStatus?.({
        type: "success",
        message:
          nextNotes.length === 0
            ? "Notes cleared."
            : "Budget notes saved.",
      });
    } catch (err) {
      const message =
        err instanceof ApiError
          ? err.errors.join(", ") || err.message
          : err instanceof Error
            ? err.message
            : "Failed to save notes";
      onStatus?.({ type: "error", message });
    } finally {
      setSaving(false);
    }
  }

  async function handleAdd(event: React.FormEvent) {
    event.preventDefault();
    const trimmed = draft.trim();
    if (!trimmed || disabled || saving) return;

    const nextNotes = [...notes, trimmed];
    setDraft("");
    await persistNotes(nextNotes);
  }

  async function handleRemove(index: number) {
    if (disabled || saving) return;
    const nextNotes = notes.filter((_, itemIndex) => itemIndex !== index);
    await persistNotes(nextNotes);
  }

  return (
    <div className="mt-4 rounded-xl border border-white/15 bg-white/10 p-4 backdrop-blur-sm">
      <p className="text-sm font-semibold text-white">Notes</p>

      {notes.length > 0 ? (
        <ul className="mt-3 space-y-2">
          {notes.map((note, index) => (
            <li
              key={`${index}-${note}`}
              className="flex items-start gap-2 rounded-lg bg-white/10 px-3 py-2 text-sm text-white"
            >
              <span className="min-w-0 flex-1 whitespace-pre-wrap break-words">
                {note}
              </span>
              <button
                type="button"
                onClick={() => void handleRemove(index)}
                disabled={disabled || saving}
                className="inline-flex shrink-0 rounded-full p-1 text-white/70 transition hover:bg-white/15 hover:text-white disabled:opacity-40"
                aria-label={`Remove note ${index + 1}`}
              >
                <X size={14} aria-hidden />
              </button>
            </li>
          ))}
        </ul>
      ) : (
        <p className="mt-2 text-xs text-white/65">
          No notes yet. Add reminders or context for this budget.
        </p>
      )}

      <form
        className="mt-3 flex flex-col gap-2 sm:flex-row"
        onSubmit={(event) => void handleAdd(event)}
      >
        <input
          type="text"
          value={draft}
          onChange={(event) => setDraft(event.target.value)}
          disabled={disabled || saving}
          placeholder="Add a note…"
          maxLength={500}
          className="min-w-0 flex-1 rounded-xl border border-white/20 bg-white/10 px-3 py-2 text-sm text-white placeholder:text-white/50 backdrop-blur-sm outline-none focus:border-white/40"
        />
        <button
          type="submit"
          disabled={disabled || saving || draft.trim().length === 0}
          className="inline-flex items-center justify-center gap-2 rounded-full border border-white/25 bg-white/10 px-4 py-2 text-sm font-medium text-white backdrop-blur-sm disabled:opacity-40"
        >
          <Plus size={14} aria-hidden />
          {saving ? "Saving…" : "Add note"}
        </button>
      </form>
    </div>
  );
}
