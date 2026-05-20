"use client";

import { useAccounts } from "@/features/accounts/hooks/useAccounts";
import { AccountRow } from "@/components/ui/AccountRow";
import { Card } from "@/components/ui/Card";
import { SectionTitle } from "@/components/ui/SectionTitle";
import { formatCurrency } from "@/lib/format";

export function AccountList() {
  const { accounts, loading, error, reload } = useAccounts();

  const totalBalance = accounts.reduce((sum, a) => sum + a.balance, 0);
  const totalLimit = accounts.reduce((sum, a) => sum + a.limit, 0);

  if (loading) {
    return (
      <div className="space-y-3">
        {[1, 2, 3].map((i) => (
          <div
            key={i}
            className="h-20 animate-pulse rounded-2xl bg-[var(--card)]"
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

  if (accounts.length === 0) {
    return (
      <Card className="p-8 text-center">
        <p className="text-[var(--muted)]">No accounts linked yet.</p>
        <button
          type="button"
          className="mt-4 rounded-full bg-[var(--accent)] px-5 py-2.5 text-sm font-semibold text-white"
        >
          Add account
        </button>
      </Card>
    );
  }

  return (
    <div className="space-y-6">
      <div className="gradient-hero rounded-2xl p-5 shadow-lg shadow-[var(--accent)]/20">
        <p className="text-sm font-medium text-white/80">Total balance</p>
        <p className="mt-1 text-3xl font-bold tracking-tight text-white">
          {formatCurrency(totalBalance)}
        </p>
        {totalLimit > 0 ? (
          <p className="mt-2 text-xs text-white/70">
            {formatCurrency(totalLimit)} total credit limit
          </p>
        ) : null}
      </div>

      <section>
        <SectionTitle
          title={`${accounts.length} account${accounts.length === 1 ? "" : "s"}`}
        />
        <Card className="overflow-hidden py-1">
          {accounts.map((account) => (
            <AccountRow key={account.accountId} account={account} />
          ))}
        </Card>
      </section>
    </div>
  );
}
