"use client";

import { useEffect, useState } from "react";
import { AppLink as Link } from "@/components/AppLink";
import { Card } from "@/components/ui/Card";
import { autoPayoffApi } from "@/features/auto-payoff/api/autoPayoffApi";
import { AUTO_BUDGET_PATH } from "@/features/auto-payoff/autoBudgetPath";
import { AutoBudgetList } from "@/features/auto-payoff/components/AutoBudgetList";
import type { AutoPayoffConfig } from "@/features/auto-payoff/types";
import { ApiError } from "@/lib/api/client";

export function ConfigHub() {
  const [configs, setConfigs] = useState<AutoPayoffConfig[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;

    async function load() {
      setLoading(true);
      setError(null);
      try {
        const result = await autoPayoffApi.getAll();
        if (cancelled) return;
        setConfigs(result.data ?? []);
      } catch (err) {
        if (cancelled) return;
        setError(
          err instanceof ApiError
            ? err.message
            : err instanceof Error
              ? err.message
              : "Failed to load Auto budgets",
        );
      } finally {
        if (!cancelled) setLoading(false);
      }
    }

    void load();
    return () => {
      cancelled = true;
    };
  }, []);

  return (
    <div className="space-y-4">
      <p className="text-sm text-[var(--muted)]">
        Rules that apply across accounts, payments, and payoff planning.
      </p>

      <Card className="p-5">
        <h2 className="text-base font-semibold text-[var(--foreground)]">
          Auto Budget
        </h2>
        <p className="mt-1 text-sm text-[var(--muted)]">
          Choose which bills extra money should skip, which categories to hit
          first, and whether remaining debts are ordered by payoff date. Select
          a saved Auto Budget below to view or edit it.
        </p>
        {error ? (
          <p className="mt-3 rounded-xl border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-900">
            {error}
          </p>
        ) : null}
        <AutoBudgetList configs={configs} loading={loading} />
        <Link
          href={AUTO_BUDGET_PATH}
          className="mt-3 inline-block text-sm font-medium text-[var(--link)] hover:underline"
        >
          New Auto Budget →
        </Link>
      </Card>
    </div>
  );
}
