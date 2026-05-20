"use client";

import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { AgGridReact } from "ag-grid-react";
import {
  AllCommunityModule,
  ModuleRegistry,
  type CellValueChangedEvent,
  type ColDef,
  type GridReadyEvent,
  type RowClassParams,
  type ValueFormatterParams,
  type ValueGetterParams,
  type ValueParserParams,
  type ValueSetterParams,
} from "ag-grid-community";
import { accountsApi } from "@/features/accounts/api/accountsApi";
import {
  ACCOUNT_CATEGORY_NAMES,
  getAccountCategoryId,
  getAccountCategoryName,
} from "@/features/accounts/data/accountCategories";
import type { AccountGridRow } from "@/features/accounts/utils/accountMapper";
import {
  formatDateForGrid,
  parseGridDate,
  toUpdateAccountRequest,
} from "@/features/accounts/utils/accountMapper";
import { ApiError } from "@/lib/api/client";
import { formatCurrencyDetailed } from "@/lib/format";

import "ag-grid-community/styles/ag-grid.css";
import "ag-grid-community/styles/ag-theme-quartz.css";
import "@/features/accounts/components/account-grid.css";

ModuleRegistry.registerModules([AllCommunityModule]);

function parseNumber(value: unknown): number {
  if (value === "" || value === null || value === undefined) return 0;
  const n = Number(value);
  return Number.isFinite(n) ? n : 0;
}

function parseOptionalNumber(value: unknown): number | null {
  if (value === "" || value === null || value === undefined) return null;
  const n = Number(value);
  return Number.isFinite(n) ? n : null;
}

const currencyFormatter = (params: ValueFormatterParams<AccountGridRow>) => {
  if (params.value === null || params.value === undefined) return "";
  return formatCurrencyDetailed(Number(params.value));
};

export function AccountGrid() {
  const gridRef = useRef<AgGridReact<AccountGridRow>>(null);
  const dirtyIds = useRef(new Set<number>());
  const [rowData, setRowData] = useState<AccountGridRow[]>([]);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [pendingCount, setPendingCount] = useState(0);
  const [status, setStatus] = useState<{
    type: "success" | "error";
    message: string;
  } | null>(null);

  const loadAccounts = useCallback(async () => {
    setLoading(true);
    setStatus(null);
    try {
      const result = await accountsApi.getAll();
      setRowData(result.data ?? []);
      dirtyIds.current.clear();
      setPendingCount(0);
    } catch (err) {
      const message =
        err instanceof ApiError
          ? err.message
          : err instanceof Error
            ? err.message
            : "Failed to load accounts";
      setStatus({ type: "error", message });
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void loadAccounts();
  }, [loadAccounts]);

  const columnDefs = useMemo<ColDef<AccountGridRow>[]>(
    () => [
      {
        field: "name",
        headerName: "Name",
        editable: true,
        filter: "agTextColumnFilter",
        minWidth: 160,
        pinned: "left",
      },
      {
        field: "number",
        headerName: "Number",
        editable: true,
        filter: "agTextColumnFilter",
        minWidth: 120,
      },
      {
        field: "balance",
        headerName: "Balance",
        editable: true,
        filter: "agNumberColumnFilter",
        valueFormatter: currencyFormatter,
        valueParser: (p: ValueParserParams) => parseNumber(p.newValue),
        minWidth: 120,
      },
      {
        field: "limit",
        headerName: "Limit",
        editable: true,
        filter: "agNumberColumnFilter",
        valueFormatter: currencyFormatter,
        valueParser: (p: ValueParserParams) => parseNumber(p.newValue),
        minWidth: 110,
      },
      {
        field: "monthlyPayment",
        headerName: "Monthly",
        editable: true,
        filter: "agNumberColumnFilter",
        valueFormatter: currencyFormatter,
        valueParser: (p: ValueParserParams) =>
          parseOptionalNumber(p.newValue),
        minWidth: 110,
      },
      {
        colId: "accountCategoryName",
        headerName: "Category",
        editable: true,
        filter: "agTextColumnFilter",
        minWidth: 140,
        valueGetter: (params: ValueGetterParams<AccountGridRow>) =>
          params.data
            ? getAccountCategoryName(params.data.accountCategoryId)
            : "",
        valueSetter: (params: ValueSetterParams<AccountGridRow>) => {
          if (!params.data) return false;
          const categoryId = getAccountCategoryId(String(params.newValue));
          if (categoryId === undefined) return false;
          params.data.accountCategoryId = categoryId;
          return true;
        },
        cellEditor: "agSelectCellEditor",
        cellEditorParams: {
          values: ACCOUNT_CATEGORY_NAMES,
        },
        filterValueGetter: (params: ValueGetterParams<AccountGridRow>) =>
          params.data
            ? getAccountCategoryName(params.data.accountCategoryId)
            : "",
      },
      {
        field: "isPaidOff",
        headerName: "Paid off",
        editable: true,
        cellEditor: "agCheckboxCellEditor",
        filter: true,
        width: 110,
      },
      {
        field: "accountOpenDate",
        headerName: "Opened",
        editable: true,
        filter: "agDateColumnFilter",
        valueFormatter: (p) => formatDateForGrid(p.value ?? null),
        valueParser: (p) =>
          parseGridDate(String(p.newValue ?? "")) ?? p.oldValue,
        minWidth: 120,
      },
      {
        field: "paidOffDate",
        headerName: "Paid off date",
        editable: true,
        filter: "agDateColumnFilter",
        valueFormatter: (p) => formatDateForGrid(p.value ?? null),
        valueParser: (p) => {
          const parsed = parseGridDate(String(p.newValue ?? ""));
          return parsed;
        },
        minWidth: 130,
      },
      {
        field: "phone",
        headerName: "Phone",
        editable: true,
        filter: "agTextColumnFilter",
        minWidth: 120,
      },
      {
        field: "email",
        headerName: "Email",
        editable: true,
        filter: "agTextColumnFilter",
        minWidth: 180,
      },
      {
        field: "url",
        headerName: "URL",
        editable: true,
        filter: "agTextColumnFilter",
        minWidth: 200,
      },
      {
        field: "username",
        headerName: "Username",
        editable: true,
        filter: "agTextColumnFilter",
        minWidth: 120,
      },
      {
        field: "description",
        headerName: "Description",
        editable: true,
        filter: "agTextColumnFilter",
        minWidth: 180,
      },
      {
        field: "notes",
        headerName: "Notes",
        editable: true,
        filter: "agTextColumnFilter",
        minWidth: 220,
        flex: 1,
      },
    ],
    [],
  );

  const defaultColDef = useMemo<ColDef>(
    () => ({
      sortable: true,
      filter: true,
      floatingFilter: true,
      resizable: true,
      minWidth: 100,
    }),
    [],
  );

  const getRowClass = useCallback(
    (params: RowClassParams<AccountGridRow>) => {
      if (params.data && dirtyIds.current.has(params.data.accountId)) {
        return "account-row-dirty";
      }
      return "";
    },
    [pendingCount],
  );

  const onCellValueChanged = useCallback(
    (event: CellValueChangedEvent<AccountGridRow>) => {
      if (!event.data || event.oldValue === event.newValue) return;
      dirtyIds.current.add(event.data.accountId);
      setPendingCount(dirtyIds.current.size);
      event.api.redrawRows({ rowNodes: [event.node] });
    },
    [],
  );

  const handleSave = async () => {
    const rowsToSave = rowData.filter((row) =>
      dirtyIds.current.has(row.accountId),
    );
    if (rowsToSave.length === 0) return;

    setSaving(true);
    setStatus(null);
    try {
      await Promise.all(
        rowsToSave.map((row) =>
          accountsApi.update(row.accountId, toUpdateAccountRequest(row)),
        ),
      );
      dirtyIds.current.clear();
      setPendingCount(0);
      setStatus({
        type: "success",
        message: `Saved ${rowsToSave.length} account${rowsToSave.length === 1 ? "" : "s"}.`,
      });
      await loadAccounts();
    } catch (err) {
      const message =
        err instanceof ApiError
          ? err.errors.join(", ") || err.message
          : err instanceof Error
            ? err.message
            : "Failed to save changes";
      setStatus({ type: "error", message });
    } finally {
      setSaving(false);
    }
  };

  const handleDiscard = () => {
    void loadAccounts();
  };

  const onGridReady = (event: GridReadyEvent) => {
    event.api.sizeColumnsToFit();
  };

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-center gap-3">
        <button
          type="button"
          onClick={() => void handleSave()}
          disabled={pendingCount === 0 || saving}
          className="rounded-full bg-[var(--link)] px-4 py-2 text-sm font-semibold text-white disabled:cursor-not-allowed disabled:opacity-40"
        >
          {saving
            ? "Saving…"
            : `Save changes${pendingCount > 0 ? ` (${pendingCount})` : ""}`}
        </button>
        <button
          type="button"
          onClick={handleDiscard}
          disabled={pendingCount === 0 || saving || loading}
          className="rounded-full border border-[var(--border)] bg-[var(--card)] px-4 py-2 text-sm font-medium disabled:cursor-not-allowed disabled:opacity-40"
        >
          Discard
        </button>
        <button
          type="button"
          onClick={() => void loadAccounts()}
          disabled={loading || saving}
          className="rounded-full border border-[var(--border)] bg-[var(--card)] px-4 py-2 text-sm font-medium disabled:cursor-not-allowed disabled:opacity-40"
        >
          Refresh
        </button>
        <p className="text-sm text-[var(--muted)]">
          Double-click a cell to edit. Edit multiple rows, then save all at
          once.
        </p>
      </div>

      {status ? (
        <div
          className={`rounded-xl px-4 py-3 text-sm ${
            status.type === "success"
              ? "border border-green-200 bg-green-50 text-green-900"
              : "border border-red-200 bg-red-50 text-red-900"
          }`}
        >
          {status.message}
        </div>
      ) : null}

      <div
        className="ag-theme-quartz account-grid rounded-2xl border border-[var(--border)] overflow-hidden"
        style={{ width: "100%", height: "calc(100vh - 280px)", minHeight: 480 }}
      >
        <AgGridReact<AccountGridRow>
          ref={gridRef}
          rowData={rowData}
          columnDefs={columnDefs}
          defaultColDef={defaultColDef}
          getRowClass={getRowClass}
          onCellValueChanged={onCellValueChanged}
          onGridReady={onGridReady}
          loading={loading}
          singleClickEdit={false}
          stopEditingWhenCellsLoseFocus={true}
          undoRedoCellEditing={true}
          undoRedoCellEditingLimit={20}
          enableCellTextSelection={true}
          ensureDomOrder={true}
          animateRows={true}
          pagination={true}
          paginationPageSize={25}
          paginationPageSizeSelector={[10, 25, 50, 100]}
        />
      </div>
    </div>
  );
}
