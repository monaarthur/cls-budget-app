"use client";

import Link from "next/link";
import { useState } from "react";
import { Copy } from "lucide-react";
import { CopyBudgetDialog } from "@/features/budgets/components/CopyBudgetDialog";
import {
  getBudgetMonth,
  getBudgetYear,
} from "@/features/budgets/utils/budgetFormat";
import type { BudgetResponse } from "@/features/budgets/types/budget";

export function BudgetRow({
  budget,
  onCopied,
}: {
  budget: BudgetResponse;
  onCopied: (name: string) => void;
}) {
  const [dialogOpen, setDialogOpen] = useState(false);
  const month = getBudgetMonth(budget.startPeriod);
  const year = getBudgetYear(budget.startPeriod);

  return (
    <>
      <div className="grid grid-cols-[1fr_auto_auto_auto] items-center gap-4 px-4 py-3">
        <Link
          href={`/budgets/detail?id=${budget.budgetId}`}
          className="min-w-0 truncate font-medium text-[var(--link)] hover:underline"
        >
          {budget.name}
        </Link>
        <p className="text-sm text-[var(--muted)]">{month}</p>
        <p className="text-sm tabular-nums text-[var(--muted)]">{year}</p>
        <button
          type="button"
          onClick={() => setDialogOpen(true)}
          className="inline-flex items-center gap-1 rounded-full border border-[var(--border)] px-3 py-1.5 text-xs font-medium text-[var(--link)] hover:bg-[var(--accent-soft)]"
          aria-label={`Copy ${budget.name}`}
        >
          <Copy className="h-3.5 w-3.5" />
          Copy
        </button>
      </div>

      {dialogOpen ? (
        <CopyBudgetDialog
          budget={budget}
          onClose={() => setDialogOpen(false)}
          onCopied={onCopied}
        />
      ) : null}
    </>
  );
}
