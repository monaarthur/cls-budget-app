"use client";

import { useState } from "react";
import Link from "next/link";
import { TopBar } from "@/components/layout/TopBar";
import { AccountList } from "@/features/accounts/components/AccountList";
import { AccountSearchField } from "@/features/accounts/components/AccountSearchField";

export function AccountsPageContent() {
  const [searchQuery, setSearchQuery] = useState("");

  return (
    <>
      <TopBar title="Accounts" />
      <div className="mb-4 flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <Link
          href="/accounts/grid"
          className="text-sm font-medium text-[var(--link)] hover:underline"
        >
          Open editable grid →
        </Link>
      </div>
      <AccountSearchField
        value={searchQuery}
        onChange={setSearchQuery}
        className="mb-4"
      />
      <AccountList searchQuery={searchQuery} onSearchQueryChange={setSearchQuery} />
    </>
  );
}
