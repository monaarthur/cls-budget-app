"use client";

import { useMemo, useState } from "react";
import { useAccounts } from "@/features/accounts/hooks/useAccounts";
import { useCreditCards } from "@/features/accounts/hooks/useCreditCards";
import { getAccountCategoryName } from "@/features/accounts/data/accountCategories";
import { AddAccountDialog } from "@/features/accounts/components/AddAccountDialog";
import { AccountRow } from "@/components/ui/AccountRow";
import { CreditCardRow } from "@/components/ui/CreditCardRow";
import { SyncAccountLogosButton } from "@/features/accounts/components/SyncAccountLogosButton";
import { AccountSearchField } from "@/features/accounts/components/AccountSearchField";
import { Card } from "@/components/ui/Card";
import { SectionTitle } from "@/components/ui/SectionTitle";
import { formatCurrency } from "@/lib/format";
import type { AccountResponse } from "@/features/accounts/types/account";

function accountMatchesSearch(
  account: AccountResponse,
  query: string,
): boolean {
  const normalizedQuery = query.trim().toLowerCase();
  if (!normalizedQuery) return true;

  const haystack = [
    account.name,
    account.number,
    account.description,
    getAccountCategoryName(account.accountCategoryId),
    account.phone,
    account.email,
    account.notes,
  ]
    .filter((value): value is string => value != null && value !== "")
    .join(" ")
    .toLowerCase();

  return haystack.includes(normalizedQuery);
}

export function AccountList({
  creditCardOnly = false,
  searchQuery: controlledSearchQuery,
  onSearchQueryChange,
}: {
  creditCardOnly?: boolean;
  searchQuery?: string;
  onSearchQueryChange?: (value: string) => void;
}) {
  const [logoVersion, setLogoVersion] = useState(0);
  const [addOpen, setAddOpen] = useState(false);
  const [internalSearchQuery, setInternalSearchQuery] = useState("");
  const searchControlled = onSearchQueryChange != null;
  const searchQuery = searchControlled
    ? (controlledSearchQuery ?? "")
    : internalSearchQuery;
  const setSearchQuery = searchControlled
    ? onSearchQueryChange
    : setInternalSearchQuery;
  const allAccounts = useAccounts();
  const creditCards = useCreditCards();
  const { accounts, loading, error, reload } = creditCardOnly
    ? creditCards
    : allAccounts;

  const entityLabel = creditCardOnly ? "credit card" : "account";
  const entityLabelPlural = creditCardOnly ? "credit cards" : "accounts";
  const emptyMessage = creditCardOnly
    ? "No credit cards linked yet."
    : "No accounts linked yet.";
  const addLabel = creditCardOnly ? "Add credit card" : "Add account";
  const heroLabel = creditCardOnly ? "Total card balance" : "Total balance";
  const searchPlaceholder = creditCardOnly
    ? "Search credit cards…"
    : "Search accounts…";

  const filteredAccounts = useMemo(
    () => accounts.filter((account) => accountMatchesSearch(account, searchQuery)),
    [accounts, searchQuery],
  );

  const isFiltering = searchQuery.trim().length > 0;
  const displayAccounts = isFiltering ? filteredAccounts : accounts;

  const totalBalance = displayAccounts.reduce((sum, a) => sum + a.balance, 0);
  const totalLimit = displayAccounts.reduce((sum, a) => sum + a.limit, 0);

  const sectionTitle = isFiltering
    ? `${filteredAccounts.length} of ${accounts.length} ${accounts.length === 1 ? entityLabel : entityLabelPlural}`
    : `${accounts.length} ${accounts.length === 1 ? entityLabel : entityLabelPlural}`;

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
      <>
        <Card className="p-8 text-center">
          <p className="text-[var(--muted)]">{emptyMessage}</p>
          <button
            type="button"
            onClick={() => setAddOpen(true)}
            className="mt-4 rounded-full bg-[var(--accent)] px-5 py-2.5 text-sm font-semibold text-white"
          >
            {addLabel}
          </button>
        </Card>
        {addOpen ? (
          <AddAccountDialog
            creditCardOnly={creditCardOnly}
            onClose={() => setAddOpen(false)}
            onAdded={() => void reload()}
          />
        ) : null}
      </>
    );
  }

  return (
    <div className="space-y-6">
      <div className="gradient-hero rounded-2xl p-5 shadow-lg shadow-[var(--accent)]/20">
        <p className="text-sm font-medium text-white/80">{heroLabel}</p>
        <p className="mt-1 text-3xl font-bold tracking-tight text-white">
          {formatCurrency(totalBalance)}
        </p>
        {totalLimit > 0 ? (
          <p className="mt-2 text-xs text-white/70">
            {formatCurrency(totalLimit)} total credit limit
          </p>
        ) : null}
      </div>

      <section className="space-y-3">
        {!searchControlled ? (
          <AccountSearchField
            value={searchQuery}
            onChange={setSearchQuery}
            placeholder={searchPlaceholder}
          />
        ) : null}

        <SectionTitle
          title={sectionTitle}
          action={
            <div className="flex items-center gap-2">
              <button
                type="button"
                onClick={() => setAddOpen(true)}
                className="rounded-full bg-[var(--accent)] px-4 py-1.5 text-xs font-semibold text-white"
              >
                {addLabel}
              </button>
              {creditCardOnly ? (
                <SyncAccountLogosButton
                  onSynced={() => setLogoVersion((v) => v + 1)}
                />
              ) : null}
            </div>
          }
        />
        <Card className="overflow-hidden py-1">
          {filteredAccounts.length === 0 ? (
            <p className="px-4 py-8 text-center text-sm text-[var(--muted)]">
              No {entityLabelPlural} match your search.
            </p>
          ) : (
            filteredAccounts.map((account) =>
              creditCardOnly ? (
                <CreditCardRow
                  key={`${account.accountId}-${logoVersion}`}
                  account={account}
                />
              ) : (
                <AccountRow key={account.accountId} account={account} />
              ),
            )
          )}
        </Card>
      </section>

      {addOpen ? (
        <AddAccountDialog
          creditCardOnly={creditCardOnly}
          onClose={() => setAddOpen(false)}
          onAdded={() => void reload()}
        />
      ) : null}
    </div>
  );
}
