"use client";

import { useState } from "react";
import { Download, FileSpreadsheet, FileText } from "lucide-react";
import type { AccountResponse } from "@/features/accounts/types/account";
import { exportAccounts } from "@/features/accounts/utils/accountExport";

export function AccountExportButtons({
  accounts,
  creditCardOnly = false,
  compact = false,
}: {
  accounts: AccountResponse[];
  creditCardOnly?: boolean;
  compact?: boolean;
}) {
  const [error, setError] = useState<string | null>(null);
  const disabled = accounts.length === 0;
  const label = creditCardOnly ? "credit cards" : "accounts";

  const handleExport = (format: "excel" | "pdf") => {
    setError(null);
    try {
      exportAccounts(format, accounts, creditCardOnly);
    } catch {
      setError(`Could not download ${format === "excel" ? "Excel" : "PDF"}.`);
    }
  };

  const buttonClass = compact
    ? "inline-flex items-center gap-1.5 rounded-full border border-[var(--border)] bg-white px-3 py-1.5 text-xs font-medium disabled:cursor-not-allowed disabled:opacity-40"
    : "inline-flex items-center gap-2 rounded-full border border-[var(--border)] bg-white px-4 py-2 text-sm font-medium disabled:cursor-not-allowed disabled:opacity-40";

  return (
    <div className="flex flex-wrap items-center gap-2">
      <button
        type="button"
        onClick={() => handleExport("excel")}
        disabled={disabled}
        className={buttonClass}
        aria-label={`Download ${label} as Excel`}
      >
        {compact ? <Download size={13} aria-hidden /> : <FileSpreadsheet size={15} aria-hidden />}
        Excel
      </button>
      <button
        type="button"
        onClick={() => handleExport("pdf")}
        disabled={disabled}
        className={buttonClass}
        aria-label={`Download ${label} as PDF`}
      >
        {compact ? <FileText size={13} aria-hidden /> : <FileText size={15} aria-hidden />}
        PDF
      </button>
      {error ? (
        <p className="text-xs text-[var(--negative)]">{error}</p>
      ) : null}
    </div>
  );
}
