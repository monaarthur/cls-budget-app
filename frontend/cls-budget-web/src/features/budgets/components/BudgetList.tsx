"use client";

import { useMemo, useState } from "react";
import { CheckCircle2 } from "lucide-react";
import { useBudgets } from "@/features/budgets/hooks/useBudgets";
import { BudgetRow } from "@/features/budgets/components/BudgetRow";
import { Card } from "@/components/ui/Card";
import { SectionTitle } from "@/components/ui/SectionTitle";
import { groupBudgetsByYear } from "@/features/budgets/utils/budgetFormat";

export function BudgetList() {
  const { budgets, loading, error, reload } = useBudgets();
  const [copySuccess, setCopySuccess] = useState<string | null>(null);

  function handleCopied(name: string) {
    setCopySuccess(`"${name}" was created.`);
    void reload();
  }

  const budgetsByYear = useMemo(() => groupBudgetsByYear(budgets), [budgets]);
  const totalCount = budgets.length;

  if (loading) {
    return (
      <div className="space-y-3">
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
          onClick={() => void reload()}
          className="mt-3 rounded-full bg-[var(--link)] px-4 py-2 text-sm font-medium text-white"
        >
          Try again
        </button>
      </Card>
    );
  }

  if (totalCount === 0) {
    return (
      <Card className="p-8 text-center">
        <p className="text-[var(--muted)]">No budgets yet.</p>
        <p className="mt-1 text-sm text-[var(--muted)]">
          Create a budget to start tracking a period.
        </p>
      </Card>
    );
  }

  return (
    <section>
      <SectionTitle title="Budget List" />

      {copySuccess ? (
        <div className="mb-3 flex items-center gap-2 rounded-2xl border border-[var(--positive)]/20 bg-[var(--positive)]/10 px-4 py-2.5 text-sm text-[var(--positive)]">
          <CheckCircle2 className="h-4 w-4 shrink-0" />
          <span>{copySuccess}</span>
          <button
            type="button"
            onClick={() => setCopySuccess(null)}
            className="ml-auto text-xs font-medium underline"
          >
            Dismiss
          </button>
        </div>
      ) : null}

      <div className="space-y-4">
        {budgetsByYear.map(({ year, budgets: yearBudgets }) => (
          <Card key={year} className="overflow-hidden">
            <div className="flex items-center justify-between border-b border-[var(--border)] bg-[#f8fafc] px-4 py-2.5">
              <h3 className="text-sm font-semibold text-[var(--foreground)]">
                {year}
              </h3>
              <span className="text-xs text-[var(--muted)]">
                {yearBudgets.length}{" "}
                {yearBudgets.length === 1 ? "budget" : "budgets"}
              </span>
            </div>
            <div className="grid grid-cols-[1fr_auto_auto_auto] gap-4 border-b border-[var(--border)] bg-[#f8fafc]/80 px-4 py-2 text-xs font-semibold uppercase tracking-wider text-[var(--muted)]">
              <span>Name</span>
              <span>Month</span>
              <span>Year</span>
              <span className="sr-only">Actions</span>
            </div>
            <ul className="divide-y divide-[var(--border)]">
              {yearBudgets.map((budget) => (
                <li key={budget.budgetId}>
                  <BudgetRow budget={budget} onCopied={handleCopied} />
                </li>
              ))}
            </ul>
          </Card>
        ))}
      </div>
    </section>
  );
}
