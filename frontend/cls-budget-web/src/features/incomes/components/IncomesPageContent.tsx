"use client";

import { useMemo, useState } from "react";
import { Plus } from "lucide-react";
import { TopBar } from "@/components/layout/TopBar";
import { useBudgets } from "@/features/budgets/hooks/useBudgets";
import { sortBudgetsByRecent } from "@/features/budgets/utils/budgetFormat";
import { AddIncomeDialog } from "@/features/incomes/components/AddIncomeDialog";
import { EditIncomeDialog } from "@/features/incomes/components/EditIncomeDialog";
import { IncomeList } from "@/features/incomes/components/IncomeList";
import { useIncomeSources } from "@/features/incomes/hooks/useIncomeSources";
import { useIncomes } from "@/features/incomes/hooks/useIncomes";
import type { IncomeResponse } from "@/features/incomes/types/income";

export function IncomesPageContent() {
  const [budgetFilterId, setBudgetFilterId] = useState<number | "all">("all");
  const [addOpen, setAddOpen] = useState(false);
  const [editingIncome, setEditingIncome] = useState<IncomeResponse | null>(
    null,
  );

  const { budgets, loading: budgetsLoading } = useBudgets();
  const sortedBudgets = useMemo(
    () => sortBudgetsByRecent(budgets),
    [budgets],
  );

  const selectedBudgetId =
    budgetFilterId === "all" ? null : budgetFilterId;

  const { incomes, loading, error, reload } = useIncomes(selectedBudgetId);
  const {
    sources,
    loading: sourcesLoading,
    error: sourcesError,
  } = useIncomeSources();

  const initialBudgetId =
    budgetFilterId === "all"
      ? sortedBudgets[0]?.budgetId
      : budgetFilterId;

  return (
    <>
      <TopBar title="Income" />
      <div className="mb-4 flex flex-wrap items-center justify-between gap-3">
        <label className="text-sm">
          <span className="mb-1 block text-xs font-medium text-[var(--muted)]">
            Budget filter
          </span>
          <select
            value={budgetFilterId}
            onChange={(event) => {
              const value = event.target.value;
              setBudgetFilterId(
                value === "all" ? "all" : Number(value),
              );
            }}
            disabled={budgetsLoading || sortedBudgets.length === 0}
            className="rounded-xl border border-[var(--border)] bg-white px-3 py-2 text-sm"
          >
            <option value="all">All budgets</option>
            {sortedBudgets.map((budget) => (
              <option key={budget.budgetId} value={budget.budgetId}>
                {budget.name}
              </option>
            ))}
          </select>
        </label>

        <button
          type="button"
          onClick={() => setAddOpen(true)}
          disabled={
            budgetsLoading ||
            sourcesLoading ||
            sortedBudgets.length === 0 ||
            sources.length === 0
          }
          className="inline-flex items-center gap-2 rounded-full bg-[var(--accent)] px-4 py-2.5 text-sm font-semibold text-white disabled:opacity-50"
        >
          <Plus size={16} aria-hidden />
          Add income
        </button>
      </div>

      {sourcesError ? (
        <p className="mb-4 text-sm text-[var(--negative)]">{sourcesError}</p>
      ) : null}

      <IncomeList
        incomes={incomes}
        budgets={sortedBudgets}
        loading={loading || budgetsLoading}
        error={error}
        onRetry={() => void reload()}
        onEdit={setEditingIncome}
      />

      {addOpen ? (
        <AddIncomeDialog
          budgets={sortedBudgets}
          sources={sources}
          initialBudgetId={initialBudgetId}
          onClose={() => setAddOpen(false)}
          onAdded={() => void reload()}
        />
      ) : null}

      {editingIncome ? (
        <EditIncomeDialog
          income={editingIncome}
          budgets={sortedBudgets}
          sources={sources}
          onClose={() => setEditingIncome(null)}
          onSaved={() => void reload()}
          onDeleted={() => void reload()}
        />
      ) : null}
    </>
  );
}
