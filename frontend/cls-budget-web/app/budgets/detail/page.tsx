"use client";

import Link from "next/link";
import { Suspense } from "react";
import { useSearchParams } from "next/navigation";
import { BudgetGrid } from "@/features/budgets/components/BudgetGrid";
import { TopBar } from "@/components/layout/TopBar";

function BudgetDetailContent() {
  const searchParams = useSearchParams();
  const budgetId = Number(searchParams.get("id"));

  if (!Number.isFinite(budgetId) || budgetId <= 0) {
    return (
      <>
        <TopBar title="Budget" />
        <p className="text-sm text-[var(--negative)]">Invalid budget id.</p>
        <Link href="/budgets" className="mt-4 inline-block text-sm text-[var(--link)]">
          ← All budgets
        </Link>
      </>
    );
  }

  return (
    <>
      <TopBar title="Budget" />
      <div className="mb-4 flex items-center gap-4 text-sm">
        <Link href="/budgets" className="text-[var(--link)] hover:underline">
          ← All budgets
        </Link>
      </div>
      <BudgetGrid budgetId={budgetId} />
    </>
  );
}

export default function BudgetDetailPage() {
  return (
    <Suspense
      fallback={
        <div className="p-8 text-sm text-[var(--muted)]">Loading budget…</div>
      }
    >
      <BudgetDetailContent />
    </Suspense>
  );
}
