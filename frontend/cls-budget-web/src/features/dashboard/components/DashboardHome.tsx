"use client";

import Link from "next/link";
import { ArrowRight, TrendingDown, Wallet } from "lucide-react";
import { useAccounts } from "@/features/accounts/hooks/useAccounts";
import { TopBar } from "@/components/layout/TopBar";
import { Card } from "@/components/ui/Card";
import { AccountRow } from "@/components/ui/AccountRow";
import { SectionTitle } from "@/components/ui/SectionTitle";
import { formatCurrency } from "@/lib/format";

const quickLinks = [
  {
    href: "/payments",
    label: "Payments",
    description: "Upcoming & recent",
    icon: TrendingDown,
  },
  {
    href: "/budgets",
    label: "Budgets",
    description: "Monthly periods",
    icon: Wallet,
  },
] as const;

export function DashboardHome() {
  const { accounts, loading, error } = useAccounts();

  const totalBalance = accounts.reduce((sum, a) => sum + a.balance, 0);
  const monthlyPayments = accounts.reduce(
    (sum, a) => sum + (a.monthlyPayment ?? 0),
    0,
  );
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
        {!loading && !error && monthlyPayments > 0 ? (
          <p className="mt-3 text-sm text-white/75">
            {formatCurrency(monthlyPayments)}/mo in scheduled payments
          </p>
        ) : null}
      </div>

      <div className="grid grid-cols-2 gap-3">
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
              <div
                key={i}
                className="h-16 animate-pulse rounded-2xl bg-[var(--card)]"
              />
            ))}
          </div>
        ) : error ? (
          <Card className="p-4 text-sm text-[var(--muted)]">
            Connect to the API to see accounts.
          </Card>
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
              <AccountRow
                key={account.accountId}
                account={account}
                showChevron={false}
              />
            ))}
          </Card>
        )}
      </section>
    </div>
  );
}
