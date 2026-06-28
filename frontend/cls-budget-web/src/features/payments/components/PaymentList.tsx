"use client";

import Link from "next/link";
import { useMemo, useState } from "react";
import { Card } from "@/components/ui/Card";
import { SectionTitle } from "@/components/ui/SectionTitle";
import { AccountSearchField } from "@/features/accounts/components/AccountSearchField";
import {
  filterPayments,
  paymentTimingLabel,
  usePayments,
} from "@/features/payments/hooks/usePayments";
import { formatCurrencyDetailed } from "@/lib/format";

const STATUS_FILTERS = [
  "all",
  "Pending",
  "Scheduled",
  "Paid",
  "Failed",
  "Overdue",
] as const;

const TIMING_FILTERS = [
  { value: "all", label: "All timing" },
  { value: "overdue", label: "Overdue" },
  { value: "due-soon", label: "Due soon" },
  { value: "upcoming", label: "Upcoming" },
  { value: "paid", label: "Paid" },
] as const;

function formatPaymentDate(iso: string | null): string {
  if (!iso) return "No date";
  return new Date(iso).toLocaleDateString("en-US", {
    month: "short",
    day: "numeric",
    year: "numeric",
    timeZone: "UTC",
  });
}

function timingBadgeClass(timing: string): string {
  switch (timing) {
    case "overdue":
      return "bg-red-100 text-red-800";
    case "due-soon":
      return "bg-amber-100 text-amber-900";
    case "paid":
      return "bg-green-100 text-green-800";
    default:
      return "bg-[var(--accent-soft)] text-[var(--link)]";
  }
}

export function PaymentList() {
  const { items, loading, error, reload } = usePayments();
  const [statusFilter, setStatusFilter] = useState<string>("all");
  const [timingFilter, setTimingFilter] = useState<string>("all");
  const [searchQuery, setSearchQuery] = useState("");

  const filtered = useMemo(
    () =>
      filterPayments(items, {
        statusFilter,
        timingFilter,
        searchQuery,
      }),
    [items, searchQuery, statusFilter, timingFilter],
  );

  const totals = useMemo(
    () => ({
      budgeted: filtered.reduce((sum, item) => sum + item.amount, 0),
      paid: filtered.reduce((sum, item) => sum + item.paymentMade, 0),
    }),
    [filtered],
  );

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
        <p className="text-sm font-medium text-white/80">Filtered payments</p>
        <p className="mt-1 text-3xl font-bold tracking-tight text-white">
          {formatCurrencyDetailed(totals.budgeted)}
        </p>
        <p className="mt-2 text-xs text-white/70">
          {formatCurrencyDetailed(totals.paid)} paid · {filtered.length} of{" "}
          {items.length} shown
        </p>
      </div>

      <div className="flex flex-col gap-3 sm:flex-row sm:flex-wrap sm:items-end">
        <AccountSearchField
          value={searchQuery}
          onChange={setSearchQuery}
          placeholder="Search payments…"
          className="sm:min-w-[220px] sm:flex-1"
        />
        <label className="text-sm">
          <span className="mb-1 block text-xs font-medium text-[var(--muted)]">
            Status
          </span>
          <select
            value={statusFilter}
            onChange={(event) => setStatusFilter(event.target.value)}
            className="rounded-xl border border-[var(--border)] bg-white px-3 py-2 text-sm"
          >
            {STATUS_FILTERS.map((status) => (
              <option key={status} value={status}>
                {status === "all" ? "All statuses" : status}
              </option>
            ))}
          </select>
        </label>
        <label className="text-sm">
          <span className="mb-1 block text-xs font-medium text-[var(--muted)]">
            Timing
          </span>
          <select
            value={timingFilter}
            onChange={(event) => setTimingFilter(event.target.value)}
            className="rounded-xl border border-[var(--border)] bg-white px-3 py-2 text-sm"
          >
            {TIMING_FILTERS.map((option) => (
              <option key={option.value} value={option.value}>
                {option.label}
              </option>
            ))}
          </select>
        </label>
      </div>

      <section>
        <SectionTitle title={`${filtered.length} payments`} />
        {filtered.length === 0 ? (
          <Card className="p-8 text-center text-sm text-[var(--muted)]">
            No payments match your filters.
          </Card>
        ) : (
          <Card className="overflow-hidden py-0">
            {filtered.map((payment) => (
              <Link
                key={payment.budgetPaymentId}
                href={`/budgets/detail?id=${payment.budgetId}`}
                className="flex items-start justify-between gap-3 border-b border-[var(--border)] px-4 py-3 transition last:border-b-0 hover:bg-black/[0.03]"
              >
                <div className="min-w-0">
                  <p className="font-medium">{payment.accountName}</p>
                  <p className="mt-0.5 text-xs text-[var(--muted)]">
                    {payment.budgetName} · {formatPaymentDate(payment.paymentDate)}
                  </p>
                  <div className="mt-2 flex flex-wrap gap-2">
                    <span className="rounded-full bg-black/[0.06] px-2 py-0.5 text-[10px] font-medium uppercase tracking-wide text-[var(--muted)]">
                      {payment.budgetPaymentStatusName}
                    </span>
                    <span
                      className={`rounded-full px-2 py-0.5 text-[10px] font-medium ${timingBadgeClass(payment.timing)}`}
                    >
                      {paymentTimingLabel(payment.timing)}
                    </span>
                  </div>
                </div>
                <div className="shrink-0 text-right">
                  <p className="font-semibold tabular-nums">
                    {formatCurrencyDetailed(payment.amount)}
                  </p>
                  <p className="text-xs tabular-nums text-[var(--muted)]">
                    {formatCurrencyDetailed(payment.paymentMade)} paid
                  </p>
                </div>
              </Link>
            ))}
          </Card>
        )}
      </section>
    </div>
  );
}
