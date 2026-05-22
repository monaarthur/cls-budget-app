"use client";

import { useState } from "react";
import { CheckCircle2 } from "lucide-react";
import { AddBudgetForm } from "@/features/budgets/components/AddBudgetForm";
import { BudgetList } from "@/features/budgets/components/BudgetList";
import { SectionTitle } from "@/components/ui/SectionTitle";

export function BudgetsView() {
  const [successMessage, setSuccessMessage] = useState<string | null>(null);
  const [listKey, setListKey] = useState(0);

  function handleBudgetChanged(name: string) {
    setSuccessMessage(`"${name}" was created.`);
    setListKey((key) => key + 1);
  }

  return (
    <div className="space-y-8">
      <section>
        <SectionTitle title="Add budget" />
        <AddBudgetForm onCreated={handleBudgetChanged} />
      </section>

      {successMessage ? (
        <div className="flex items-center gap-2 rounded-2xl border border-[var(--positive)]/20 bg-[var(--positive)]/10 px-4 py-2.5 text-sm text-[var(--positive)]">
          <CheckCircle2 className="h-4 w-4 shrink-0" />
          <span>{successMessage}</span>
          <button
            type="button"
            onClick={() => setSuccessMessage(null)}
            className="ml-auto text-xs font-medium underline"
          >
            Dismiss
          </button>
        </div>
      ) : null}

      <BudgetList key={listKey} />
    </div>
  );
}
