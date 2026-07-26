"use client";

import { useEffect, useMemo, useState } from "react";
import { Plus, X } from "lucide-react";
import { Card } from "@/components/ui/Card";
import type { BudgetGridRow } from "@/features/budgets/utils/budgetGridMapper";
import {
  createPaymentLineNote,
  formatPaymentLineNoteDate,
  getActivePaymentLineNotes,
  parsePaymentLineNotes,
  serializePaymentLineNotes,
  softRemovePaymentLineNote,
  type PaymentLineNoteItem,
} from "@/features/budgets/utils/paymentLineNotes";

export function BudgetPaymentNotesModal({
  row,
  onClose,
  onApply,
  disabled = false,
}: {
  row: BudgetGridRow;
  onClose: () => void;
  onApply: (notes: { lineNotes: string | null; accountNotes: string | null }) => void;
  disabled?: boolean;
}) {
  const [lineNoteItems, setLineNoteItems] = useState<PaymentLineNoteItem[]>(() =>
    parsePaymentLineNotes(row.notes),
  );
  const [draftNote, setDraftNote] = useState("");
  const [accountNotes, setAccountNotes] = useState(row.accountNotes ?? "");

  useEffect(() => {
    setLineNoteItems(parsePaymentLineNotes(row.notes));
    setDraftNote("");
    setAccountNotes(row.accountNotes ?? "");
  }, [row]);

  useEffect(() => {
    function onKeyDown(event: KeyboardEvent) {
      if (event.key === "Escape" && !disabled) onClose();
    }
    window.addEventListener("keydown", onKeyDown);
    return () => window.removeEventListener("keydown", onKeyDown);
  }, [disabled, onClose]);

  const activeLineNotes = useMemo(
    () => getActivePaymentLineNotes(lineNoteItems),
    [lineNoteItems],
  );

  function handleAddLineNote(event?: React.FormEvent) {
    event?.preventDefault();
    const trimmed = draftNote.trim();
    if (!trimmed || disabled) return;
    setLineNoteItems((prev) => [...prev, createPaymentLineNote(trimmed)]);
    setDraftNote("");
  }

  function handleRemoveLineNote(noteId: string) {
    if (disabled) return;
    setLineNoteItems((prev) => softRemovePaymentLineNote(prev, noteId));
  }

  function handleApply(event: React.FormEvent) {
    event.preventDefault();
    if (disabled) return;

    // Flush any unfinished draft as a new dated note.
    let items = lineNoteItems;
    const trimmedDraft = draftNote.trim();
    if (trimmedDraft) {
      items = [...items, createPaymentLineNote(trimmedDraft)];
    }

    const trimmedAccount = accountNotes.trim();
    onApply({
      lineNotes: serializePaymentLineNotes(items),
      accountNotes: trimmedAccount.length > 0 ? trimmedAccount : null,
    });
  }

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-black/45 p-4"
      role="dialog"
      aria-modal="true"
      aria-labelledby="budget-payment-notes-title"
      onClick={disabled ? undefined : onClose}
    >
      <Card className="flex max-h-[90vh] w-full max-w-2xl flex-col shadow-xl">
        <div
          className="flex min-h-0 flex-1 flex-col p-5"
          onClick={(event) => event.stopPropagation()}
        >
          <div className="mb-4 flex items-start justify-between gap-3">
            <div className="min-w-0">
              <h2
                id="budget-payment-notes-title"
                className="text-lg font-semibold text-[var(--foreground)]"
              >
                Notes
              </h2>
              <p className="mt-1 truncate text-sm text-[var(--muted)]">
                {row.accountName}
              </p>
            </div>
            <button
              type="button"
              onClick={onClose}
              disabled={disabled}
              className="rounded-lg p-1 text-[var(--muted)] hover:bg-black/[0.04] disabled:opacity-40"
              aria-label="Close"
            >
              <X size={18} />
            </button>
          </div>

          <form
            onSubmit={handleApply}
            className="flex min-h-0 flex-1 flex-col gap-5 overflow-y-auto"
          >
            <section className="flex flex-col gap-3 text-sm">
              <div>
                <p className="font-medium">Line to-dos</p>
                <p className="mt-1 text-xs text-[var(--muted)]">
                  Each item is dated automatically. Removing hides it from this
                  view but keeps the history.
                </p>
              </div>

              {activeLineNotes.length > 0 ? (
                <ul className="space-y-2">
                  {activeLineNotes.map((note) => (
                    <li
                      key={note.id}
                      className="flex items-start gap-3 rounded-xl border border-[var(--border)] bg-white px-3 py-3"
                    >
                      <div className="min-w-0 flex-1">
                        <p className="text-xs font-semibold uppercase tracking-wide text-[var(--muted)]">
                          {formatPaymentLineNoteDate(note.createdAt)}
                        </p>
                        <p className="mt-1 whitespace-pre-wrap break-words text-base leading-relaxed text-[var(--foreground)]">
                          {note.text}
                        </p>
                      </div>
                      <button
                        type="button"
                        onClick={() => handleRemoveLineNote(note.id)}
                        disabled={disabled}
                        className="inline-flex shrink-0 rounded-full p-1.5 text-[var(--muted)] transition hover:bg-black/[0.05] hover:text-[var(--foreground)] disabled:opacity-40"
                        aria-label="Remove note from view"
                        title="Remove from view"
                      >
                        <X size={16} aria-hidden />
                      </button>
                    </li>
                  ))}
                </ul>
              ) : (
                <p className="rounded-xl border border-dashed border-[var(--border)] px-3 py-4 text-sm text-[var(--muted)]">
                  No active to-dos for this payment line.
                </p>
              )}

              <div className="flex flex-col gap-2 sm:flex-row">
                <input
                  type="text"
                  value={draftNote}
                  onChange={(event) => setDraftNote(event.target.value)}
                  disabled={disabled}
                  autoFocus
                  maxLength={500}
                  placeholder="Add a to-do note…"
                  className="min-w-0 flex-1 rounded-xl border border-[var(--border)] bg-white px-3 py-2.5 text-base text-[var(--foreground)]"
                  onKeyDown={(event) => {
                    if (event.key === "Enter") {
                      event.preventDefault();
                      handleAddLineNote();
                    }
                  }}
                />
                <button
                  type="button"
                  onClick={() => handleAddLineNote()}
                  disabled={disabled || draftNote.trim().length === 0}
                  className="inline-flex items-center justify-center gap-2 rounded-full border border-[var(--border)] px-4 py-2.5 text-sm font-medium disabled:opacity-40"
                >
                  <Plus size={15} aria-hidden />
                  Add
                </button>
              </div>
            </section>

            <label className="flex min-h-0 flex-col text-sm">
              <span className="mb-1.5 font-medium">Account notes</span>
              <span className="mb-2 text-xs text-[var(--muted)]">
                Shared across every budget line for this account.
              </span>
              <textarea
                value={accountNotes}
                onChange={(event) => setAccountNotes(event.target.value)}
                disabled={disabled}
                rows={6}
                maxLength={4000}
                placeholder="Standing notes for this account…"
                className="min-h-[8rem] w-full resize-y rounded-xl border border-[var(--border)] bg-white px-3 py-3 text-base leading-relaxed text-[var(--foreground)]"
              />
              <span className="mt-1 text-xs text-[var(--muted)]">
                {accountNotes.length}/4000
              </span>
            </label>

            <div className="flex justify-end gap-2 pt-1">
              <button
                type="button"
                onClick={onClose}
                disabled={disabled}
                className="rounded-full border border-[var(--border)] px-4 py-2 text-sm font-medium disabled:opacity-40"
              >
                Cancel
              </button>
              <button
                type="submit"
                disabled={disabled}
                className="rounded-full bg-[var(--link)] px-4 py-2 text-sm font-semibold text-white disabled:opacity-40"
              >
                Apply notes
              </button>
            </div>
          </form>
        </div>
      </Card>
    </div>
  );
}
