"use client";

import { forwardRef, useImperativeHandle } from "react";
import type { ICellRendererParams } from "ag-grid-community";
import { CompanyLogo } from "@/components/ui/CompanyLogo";
import { isPinnedTotalRow } from "@/features/accounts/components/gridPinnedTotals";
import type { AccountGridRow } from "@/features/accounts/utils/accountMapper";

export const CreditCardNameCellRenderer = forwardRef<
  { refresh: () => boolean },
  ICellRendererParams<AccountGridRow> & { logoVersion?: number }
>((params, ref) => {
  useImperativeHandle(ref, () => ({
    refresh: () => true,
  }));

  if (!params.data) return null;

  const isPinnedTotal = isPinnedTotalRow(params.node);
  const accountId = params.data.accountId;
  const showLogo =
    !isPinnedTotal &&
    typeof accountId === "number" &&
    Number.isFinite(accountId);

  return (
    <div className="flex h-full min-w-0 items-center gap-2">
      {showLogo ? (
        <CompanyLogo
          key={`${accountId}-${params.logoVersion ?? 0}`}
          name={params.data.name}
          accountId={accountId}
          size={28}
        />
      ) : null}
      <span className="truncate">{params.data.name}</span>
    </div>
  );
});

CreditCardNameCellRenderer.displayName = "CreditCardNameCellRenderer";
