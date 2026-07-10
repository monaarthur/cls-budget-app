"use client";

import { useMemo, useState } from "react";
import { AgGridReact } from "ag-grid-react";
import {
  AllCommunityModule,
  ModuleRegistry,
  type ColDef,
  type ValueFormatterParams,
} from "ag-grid-community";
import { Search } from "lucide-react";
import { accountGridTheme } from "@/features/accounts/components/accountGridTheme";
import { formatDateForGrid } from "@/features/accounts/utils/accountMapper";
import { getAccountCategoryName } from "@/features/accounts/data/accountCategories";
import type { ImportedTransaction } from "@/features/transactions/types/transactionImport";
import { formatCurrencyDetailed } from "@/lib/format";

import "@/features/accounts/components/account-grid.css";

ModuleRegistry.registerModules([AllCommunityModule]);

function categoryLabel(row: ImportedTransaction): string {
  if (row.accountCategoryName) return row.accountCategoryName;
  if (row.categoryRaw) return `${row.categoryRaw} (unmatched)`;
  return "";
}

const currencyCol = {
  valueFormatter: (params: ValueFormatterParams<ImportedTransaction>) =>
    params.value === null || params.value === undefined
      ? ""
      : formatCurrencyDetailed(Number(params.value)),
  cellClass: "ag-cell-currency",
  headerClass: "ag-cell-currency-header",
  filter: "agNumberColumnFilter" as const,
};

export function TransactionImportGrid({
  transactions,
  loading,
}: {
  transactions: ImportedTransaction[];
  loading: boolean;
}) {
  const [quickFilter, setQuickFilter] = useState("");

  const columnDefs = useMemo<ColDef<ImportedTransaction>[]>(
    () => [
      {
        colId: "lineNumber",
        field: "lineNumber",
        headerName: "Line",
        width: 72,
        filter: "agNumberColumnFilter",
        sort: "asc",
      },
      {
        colId: "description",
        field: "description",
        headerName: "Description",
        minWidth: 200,
        flex: 1,
        filter: "agTextColumnFilter",
      },
      {
        colId: "category",
        headerName: "Category",
        minWidth: 140,
        filter: "agTextColumnFilter",
        valueGetter: (params) => {
          if (!params.data) return "";
          return params.data.accountCategoryId
            ? getAccountCategoryName(params.data.accountCategoryId)
            : categoryLabel(params.data);
        },
      },
      {
        colId: "transactionDate",
        field: "transactionDate",
        headerName: "Date",
        minWidth: 120,
        filter: "agDateColumnFilter",
        valueFormatter: (params) => formatDateForGrid(params.value ?? null),
      },
      {
        colId: "postingStatusRaw",
        field: "postingStatusRaw",
        headerName: "Posting status",
        minWidth: 130,
        filter: "agTextColumnFilter",
        valueFormatter: (params) => params.value ?? "",
      },
      {
        colId: "budgetPaymentStatusName",
        field: "budgetPaymentStatusName",
        headerName: "Status",
        minWidth: 120,
        filter: "agTextColumnFilter",
        cellClass: (params) => {
          switch (params.value?.trim().toLowerCase()) {
            case "paid":
              return "budget-row-paid";
            case "pending":
              return "budget-row-scheduled";
            default:
              return "budget-row-unassigned";
          }
        },
      },
      {
        colId: "amount",
        field: "amount",
        headerName: "Amount",
        minWidth: 120,
        ...currencyCol,
      },
      {
        colId: "notes",
        field: "notes",
        headerName: "Notes",
        minWidth: 160,
        filter: "agTextColumnFilter",
        hide: true,
      },
    ],
    [],
  );

  const defaultColDef = useMemo<ColDef<ImportedTransaction>>(
    () => ({
      sortable: true,
      resizable: true,
      filter: true,
      editable: false,
    }),
    [],
  );

  if (!loading && transactions.length === 0) {
    return (
      <div className="rounded-xl border border-dashed border-[var(--border)] bg-white p-8 text-center text-sm text-[var(--muted)]">
        No transactions in this import yet.
      </div>
    );
  }

  return (
    <div className="account-grid-shell w-full">
      <div className="account-grid-toolbar">
        <div className="account-grid-search">
          <Search size={16} aria-hidden />
          <input
            type="search"
            value={quickFilter}
            onChange={(event) => setQuickFilter(event.target.value)}
            placeholder="Search all columns…"
            aria-label="Search imported transactions"
          />
        </div>
        <p className="account-grid-meta">
          {transactions.length} transaction
          {transactions.length === 1 ? "" : "s"}
        </p>
      </div>

      <div className="account-grid-viewport">
        <AgGridReact<ImportedTransaction>
          theme={accountGridTheme}
          rowData={transactions}
          getRowId={(params) => String(params.data.importedTransactionId)}
          columnDefs={columnDefs}
          defaultColDef={defaultColDef}
          loading={loading}
          quickFilterText={quickFilter}
          enableCellTextSelection={true}
          ensureDomOrder={true}
          animateRows={true}
          pagination={true}
          paginationPageSize={25}
          paginationPageSizeSelector={[10, 25, 50, 100]}
          suppressDragLeaveHidesColumns={false}
          tooltipShowDelay={400}
        />
      </div>
    </div>
  );
}
