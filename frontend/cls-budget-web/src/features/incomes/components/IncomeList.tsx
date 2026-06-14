"use client";

import { useMemo } from "react";
import { Pencil } from "lucide-react";
import type { BudgetResponse } from "@/features/budgets/types/budget";
import { formatBudgetMonthYear } from "@/features/budgets/utils/budgetFormat";
import type { IncomeResponse } from "@/features/incomes/types/income";
import { Card } from "@/components/ui/Card";
import { SectionTitle } from "@/components/ui/SectionTitle";
import { formatCurrencyDetailed } from "@/lib/format";

function formatReceivedDate(iso: string): string {
  return new Date(iso).toLocaleDateString("en-US", {
    month: "short",
    day: "numeric",
    year: "numeric",
    timeZone: "UTC",
  });
}

function IncomeRow({
  income,
  budgetName,
  onEdit,
}: {
  income: IncomeResponse;
  budgetName: string;
  onEdit: (income: IncomeResponse) => void;
}) {
  return (
    <div className="flex items-start justify-between gap-3 border-b border-[var(--border)] px-4 py-3 last:border-b-0">
      <div className="min-w-0 flex-1">
        <p className="font-medium">{income.incomeSourceName}</p>
        <p className="mt-0.5 text-xs text-[var(--muted)]">
          {budgetName} · {formatReceivedDate(income.receivedDate)}
        </p>
        {income.notes ? (
          <p className="mt-1 truncate text-xs text-[var(--muted)]">
            {income.notes}
          </p>
        ) : null}
      </div>
      <div className="flex shrink-0 items-center gap-2">
        <p className="font-semibold tabular-nums text-[var(--positive)]">
          {formatCurrencyDetailed(income.amount)}
        </p>
        <button
          type="button"
          onClick={() => onEdit(income)}
          className="rounded-lg p-1.5 text-[var(--muted)] hover:bg-black/[0.04]"
          aria-label={`Edit ${income.incomeSourceName}`}
        >
          <Pencil size={15} />
        </button>
      </div>
    </div>
  );
}

export function IncomeList({
  incomes,
  budgets,
  loading,
  error,
  onRetry,
  onEdit,
}: {
  incomes: IncomeResponse[];
  budgets: BudgetResponse[];
  loading: boolean;
  error: string | null;
  onRetry: () => void;
  onEdit: (income: IncomeResponse) => void;
}) {
  const budgetNameById = useMemo(
    () => new Map(budgets.map((budget) => [budget.budgetId, budget.name])),
    [budgets],
  );

  const sortedIncomes = useMemo(
    () =>
      [...incomes].sort(
        (a, b) =>
          new Date(b.receivedDate).getTime() -
          new Date(a.receivedDate).getTime(),
      ),
    [incomes],
  );

  const totalAmount = useMemo(
    () => sortedIncomes.reduce((sum, income) => sum + income.amount, 0),
    [sortedIncomes],
  );

  if (loading) {
    return (
      <div className="space-y-3">
        <div className="h-28 animate-pulse rounded-2xl bg-[var(--card)]" />
        {[1, 2, 3].map((i) => (
          <div
            key={i}
            className="h-16 animate-pulse rounded-2xl bg-[var(--card)]"
          />
        ))}
      </div>
    );
  }

  if (error) {
    return (
      <Card className="p-4">
        <p className="text-sm text-[var(--negative)]">{error}</p>
        <button
          type="button"
          onClick={onRetry}
          className="mt-3 rounded-full bg-[var(--accent)] px-4 py-2 text-sm font-medium text-white"
        >
          Try again
        </button>
      </Card>
    );
  }

  return (
    <div className="space-y-6">
      <div className="gradient-hero rounded-2xl p-5 shadow-lg shadow-[var(--accent)]/20">
        <p className="text-sm font-medium text-white/80">Total income</p>
        <p className="mt-1 text-3xl font-bold tracking-tight text-white">
          {formatCurrencyDetailed(totalAmount)}
        </p>
        <p className="mt-2 text-xs text-white/70">
          {sortedIncomes.length}{" "}
          {sortedIncomes.length === 1 ? "entry" : "entries"}
        </p>
      </div>

      <section>
        <SectionTitle
          title={`${sortedIncomes.length} ${
            sortedIncomes.length === 1 ? "income" : "incomes"
          }`}
        />
        {sortedIncomes.length === 0 ? (
          <Card className="p-8 text-center">
            <p className="text-sm text-[var(--muted)]">
              No income recorded yet. Add your first entry to get started.
            </p>
          </Card>
        ) : (
          <Card className="overflow-hidden py-0">
            {sortedIncomes.map((income) => (
              <IncomeRow
                key={income.budgetIncomeId}
                income={income}
                budgetName={
                  budgetNameById.get(income.budgetId) ??
                  formatBudgetMonthYear(
                    budgets.find((b) => b.budgetId === income.budgetId)
                      ?.startPeriod ?? income.receivedDate,
                  )
                }
                onEdit={onEdit}
              />
            ))}
          </Card>
        )}
      </section>
    </div>
  );
}
