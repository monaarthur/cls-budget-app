"use client";

import type { ICellRendererParams } from "ag-grid-community";
import { StickyNote, TriangleAlert } from "lucide-react";
import {
  needsPaymentSourceUpdate,
  type BudgetGridRow,
} from "@/features/budgets/utils/budgetGridMapper";
import { hasActivePaymentLineNotes } from "@/features/budgets/utils/paymentLineNotes";

const PAYMENT_SOURCE_TOOLTIP_LINES = ["Update Payment Type"];

export interface AccountNameCellContext {
  onOpenNotes?: (row: BudgetGridRow) => void;
}

export function AccountNameCellRenderer(
  params: ICellRendererParams<BudgetGridRow, unknown, AccountNameCellContext>,
) {
  if (!params.data) return null;

  const showWarning = needsPaymentSourceUpdate(params.data);
  const accountNotes = params.data.accountNotes?.trim() ?? "";
  const hasNotes =
    accountNotes.length > 0 || hasActivePaymentLineNotes(params.data.notes);
  const openNotes = params.context?.onOpenNotes;

  return (
    <div className="flex min-w-0 items-center gap-1.5">
      <span className="truncate">{params.data.accountName}</span>
      <button
        type="button"
        className={`budget-notes-indicator shrink-0 ${
          hasNotes ? "budget-notes-indicator--active" : ""
        }`}
        aria-label={
          hasNotes
            ? "Open notes for this payment"
            : "Add notes for this payment"
        }
        title={hasNotes ? "Open notes" : "Add notes"}
        onClick={(event) => {
          event.stopPropagation();
          openNotes?.(params.data!);
        }}
      >
        <StickyNote
          size={14}
          strokeWidth={2.25}
          className={hasNotes ? "text-sky-300" : "text-white/45"}
          aria-hidden
        />
      </button>
      {showWarning ? (
        <span
          className="budget-payment-source-warning shrink-0"
          tabIndex={0}
          aria-label={PAYMENT_SOURCE_TOOLTIP_LINES.join(". ")}
        >
          <TriangleAlert
            size={14}
            strokeWidth={2.25}
            className="text-amber-500"
            aria-hidden
          />
          <span className="budget-payment-source-warning-tooltip" role="tooltip">
            {PAYMENT_SOURCE_TOOLTIP_LINES.map((line) => (
              <span
                key={line}
                className="budget-payment-source-warning-tooltip-line"
              >
                {line}
              </span>
            ))}
          </span>
        </span>
      ) : null}
    </div>
  );
}
