"use client";

import type { ICellRendererParams } from "ag-grid-community";
import { StickyNote, TriangleAlert } from "lucide-react";
import {
  needsPaymentSourceUpdate,
  type BudgetGridRow,
} from "@/features/budgets/utils/budgetGridMapper";

const PAYMENT_SOURCE_TOOLTIP_LINES = ["Update Payment Type"];

export function AccountNameCellRenderer(
  params: ICellRendererParams<BudgetGridRow>,
) {
  if (!params.data) return null;

  const showWarning = needsPaymentSourceUpdate(params.data);
  const accountNotes = params.data.accountNotes?.trim() ?? "";
  const lineNotes = params.data.notes?.trim() ?? "";
  const hasAccountNotes = accountNotes.length > 0;
  const hasLineNotes = lineNotes.length > 0;

  return (
    <div className="flex min-w-0 items-center gap-1.5">
      <span className="truncate">{params.data.accountName}</span>
      {hasAccountNotes || hasLineNotes ? (
        <span
          className="budget-notes-indicator shrink-0"
          tabIndex={0}
          aria-label={
            hasAccountNotes
              ? `Account notes: ${accountNotes}`
              : `Line notes: ${lineNotes}`
          }
        >
          <StickyNote
            size={14}
            strokeWidth={2.25}
            className="text-sky-300"
            aria-hidden
          />
          <span className="budget-notes-indicator-tooltip" role="tooltip">
            {hasAccountNotes ? (
              <>
                <span className="budget-notes-indicator-tooltip-label">
                  Account notes
                </span>
                <span className="budget-notes-indicator-tooltip-line">
                  {accountNotes}
                </span>
              </>
            ) : null}
            {hasLineNotes ? (
              <>
                <span className="budget-notes-indicator-tooltip-label">
                  Line notes
                </span>
                <span className="budget-notes-indicator-tooltip-line">
                  {lineNotes}
                </span>
              </>
            ) : null}
          </span>
        </span>
      ) : null}
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
              <span key={line} className="budget-payment-source-warning-tooltip-line">
                {line}
              </span>
            ))}
          </span>
        </span>
      ) : null}
    </div>
  );
}
