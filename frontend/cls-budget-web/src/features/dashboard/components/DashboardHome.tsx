"use client";

import Link from "next/link";
import { ArrowRight, Banknote, TrendingDown, Wallet } from "lucide-react";
import { TopBar } from "@/components/layout/TopBar";
import { Card } from "@/components/ui/Card";
import { AccountRow } from "@/components/ui/AccountRow";
import { SectionTitle } from "@/components/ui/SectionTitle";
import { useDashboardSummary } from "@/features/dashboard/hooks/useDashboardSummary";
import { paymentTimingLabel } from "@/features/payments/utils/paymentTiming";
import { formatCurrency, formatCurrencyDetailed } from "@/lib/format";

const quickLinks = [
  { href: "/payments", label: "Payments", description: "Upcoming & recent", icon: TrendingDown },
  { href: "/budgets", label: "Budgets", description: "Monthly periods", icon: Wallet },
  { href: "/income", label: "Income", description: "Track earnings", icon: Banknote },
] as const;

function MetricCard({
  label,
  value,
  hint,
}: {
  label: string;
  value: string;
  hint?: string;
}) {
  return (
    <Card className="p-4">
      <p className="text-xs font-medium uppercase tracking-wide text-[var(--muted)]">
        {label}
      </p>
      <p className="mt-2 text-2xl font-bold tabular-nums">{value}</p>
      {hint ? <p className="mt-1 text-xs text-[var(--muted)]">{hint}</p> : null}
    </Card>
  );
}

export function DashboardHome() {
  const {
    accounts,
    currentBudget,
    stats,
    totalBalance,
    monthlyPayments,
    upcomingPayments,
    loading,
    error,
    reload,
  } = useDashboardSummary();

  const preview = accounts.slice(0, 4);

  return (
    <div className="space-y-6">
      <TopBar />

      <div className="gradient-hero relative overflow-hidden rounded-2xl p-6 shadow-lg shadow-[var(--accent)]/25">
        <div className="absolute -right-8 -top-8 h-32 w-32 rounded-full bg-white/10 blur-2xl" />
        <p className="text-sm font-medium text-white/80">Total balance</p>
        {loading ? (
          <div className="mt-2 h-10 w-40 animate-pulse rounded-lg bg-white/20" />
        ) : (
          <p className="mt-1 text-4xl font-bold tracking-tight text-white">
            {error ? "—" : formatCurrency(totalBalance)}
          </p>
        )}
        {!loading && !error ? (
          <p className="mt-3 text-sm text-white/75">
            {formatCurrency(monthlyPayments)}/mo scheduled ·{" "}
            {formatCurrencyDetailed(stats.incomeTotal)} income recorded
            {currentBudget ? ` · ${currentBudget.name}` : ""}
          </p>
        ) : null}
      </div>

      {!loading && !error ? (
        <div className="grid grid-cols-2 gap-3 lg:grid-cols-4">
          <MetricCard
            label="Budgeted"
            value={formatCurrency(stats.totalBudgeted)}
            hint={currentBudget ? "Current budget" : "All payments"}
          />
          <MetricCard
            label="Paid"
            value={formatCurrency(stats.totalPaid)}
            hint={`${stats.budgetUtilization}% utilized`}
          />
          <MetricCard
            label="Overdue"
            value={formatCurrency(stats.totalOverdue)}
            hint={`${stats.overdueCount} payment${stats.overdueCount === 1 ? "" : "s"}`}
          />
          <MetricCard
            label="Due soon"
            value={formatCurrency(stats.totalDueSoon)}
            hint={`${stats.dueSoonCount} within 7 days`}
          />
        </div>
      ) : null}

      {error ? (
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
      ) : null}

      <div className="grid grid-cols-1 gap-3 sm:grid-cols-3">
        {quickLinks.map(({ href, label, description, icon: Icon }) => (
          <Link key={href} href={href}>
            <Card className="flex h-full flex-col p-4 transition hover:border-[var(--link)]/30">
              <Icon className="h-5 w-5 text-[var(--link)]" />
              <p className="mt-3 font-semibold">{label}</p>
              <p className="mt-0.5 text-xs text-[var(--muted)]">{description}</p>
            </Card>
          </Link>
        ))}
      </div>

      <section>
        <SectionTitle
          title="Attention needed"
          action={
            <Link
              href="/payments"
              className="flex items-center gap-0.5 text-xs font-medium text-[var(--link)]"
            >
              All payments
              <ArrowRight className="h-3.5 w-3.5" />
            </Link>
          }
        />
        {loading ? (
          <div className="space-y-2">
            {[1, 2].map((i) => (
              <div key={i} className="h-16 animate-pulse rounded-2xl bg-[var(--card)]" />
            ))}
          </div>
        ) : upcomingPayments.length === 0 ? (
          <Card className="p-6 text-center text-sm text-[var(--muted)]">
            No overdue or due-soon payments.
          </Card>
        ) : (
          <Card className="overflow-hidden py-0">
            {upcomingPayments.map((payment) => (
              <Link
                key={payment.budgetPaymentId}
                href={`/budgets/detail?id=${payment.budgetId}`}
                className="flex items-center justify-between gap-3 border-b border-[var(--border)] px-4 py-3 last:border-b-0 hover:bg-black/[0.03]"
              >
                <div className="min-w-0">
                  <p className="truncate font-medium">{payment.accountName}</p>
                  <p className="text-xs text-[var(--muted)]">
                    {paymentTimingLabel(payment.timing)} ·{" "}
                    {payment.budgetPaymentStatusName}
                  </p>
                </div>
                <p className="shrink-0 font-semibold tabular-nums">
                  {formatCurrencyDetailed(payment.amount)}
                </p>
              </Link>
            ))}
          </Card>
        )}
      </section>

      <section>
        <SectionTitle
          title="Accounts"
          action={
            <Link
              href="/accounts"
              className="flex items-center gap-0.5 text-xs font-medium text-[var(--link)]"
            >
              See all
              <ArrowRight className="h-3.5 w-3.5" />
            </Link>
          }
        />
        {loading ? (
          <div className="space-y-2">
            {[1, 2, 3].map((i) => (
              <div key={i} className="h-16 animate-pulse rounded-2xl bg-[var(--card)]" />
            ))}
          </div>
        ) : preview.length === 0 ? (
          <Card className="p-6 text-center text-sm text-[var(--muted)]">
            No accounts yet.{" "}
            <Link href="/accounts" className="text-[var(--link)]">
              Add one
            </Link>
          </Card>
        ) : (
          <Card className="overflow-hidden py-1">
            {preview.map((account) => (
              <AccountRow key={account.accountId} account={account} showChevron={false} />
            ))}
          </Card>
        )}
      </section>
    </div>
  );
}
