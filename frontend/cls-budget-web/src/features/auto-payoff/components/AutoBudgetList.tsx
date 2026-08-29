"use client";

import { AppLink as Link } from "@/components/AppLink";
import { autoBudgetHref } from "@/features/auto-payoff/autoBudgetPath";
import type { AutoPayoffConfig } from "@/features/auto-payoff/types";
import { formatCurrencyDetailed } from "@/lib/format";

export function AutoBudgetList({
  configs,
  selectedId = null,
  loading = false,
}: {
  configs: AutoPayoffConfig[];
  selectedId?: number | null;
  loading?: boolean;
}) {
  const named = configs.filter(
    (config) =>
      (config.autoPayoffConfigId ?? 0) > 0 && config.name.trim().length > 0,
  );

  if (loading) {
    return (
      <p className="mt-3 text-sm text-[var(--muted)]">Loading Auto budgets…</p>
    );
  }

  if (named.length === 0) {
    return (
      <p className="mt-3 text-sm text-[var(--muted)]">
        No Auto budgets saved yet. Create one to name a set of extra-payment
        rules you can come back and edit.
      </p>
    );
  }

  return (
    <ul className="mt-3 divide-y divide-[var(--border)] overflow-hidden rounded-xl border border-[var(--border)]">
      {named.map((config) => {
        const id = config.autoPayoffConfigId!;
        const selected = selectedId === id;
        return (
          <li key={id}>
            <Link
              href={autoBudgetHref(id)}
              className={`flex items-center justify-between gap-3 px-3 py-2.5 text-sm transition ${
                selected
                  ? "bg-[var(--accent-soft)] text-[var(--link)]"
                  : "hover:bg-black/[0.03]"
              }`}
            >
              <span className="min-w-0">
                <span className="block truncate font-medium">{config.name}</span>
                <span
                  className={`mt-0.5 block text-xs ${
                    selected ? "text-[var(--link)]" : "text-[var(--muted)]"
                  }`}
                >
                  Extra {formatCurrencyDetailed(config.extraMonthlyAmount ?? 0)}
                </span>
              </span>
              <span className="shrink-0 text-xs font-medium text-[var(--link)]">
                {selected ? "Editing" : "View / edit"}
              </span>
            </Link>
          </li>
        );
      })}
    </ul>
  );
}
