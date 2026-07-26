"use client";

import type { ICellRendererParams } from "ag-grid-community";
import { Maximize2 } from "lucide-react";
import type { BudgetGridRow } from "@/features/budgets/utils/budgetGridMapper";
import { formatPaymentLineNotesPreview } from "@/features/budgets/utils/paymentLineNotes";

export interface NotesPreviewCellContext {
  onOpenNotes?: (row: BudgetGridRow) => void;
}

export function NotesPreviewCellRenderer(
  params: ICellRendererParams<BudgetGridRow, unknown, NotesPreviewCellContext>,
) {
  if (!params.data) return null;

  const colId = params.column?.getColId();
  const text =
    colId === "notes"
      ? formatPaymentLineNotesPreview(params.data.notes)
      : String(params.value ?? "").trim();
  const openNotes = params.context?.onOpenNotes;

  return (
    <button
      type="button"
      className="budget-notes-preview-cell"
      onClick={(event) => {
        event.stopPropagation();
        openNotes?.(params.data!);
      }}
      title={text || "Open notes"}
    >
      <span className="budget-notes-preview-text">
        {text.length > 0 ? text : "Add notes…"}
      </span>
      <Maximize2 size={13} className="budget-notes-preview-icon" aria-hidden />
    </button>
  );
}
