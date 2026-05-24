"use client";

import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { AgGridReact } from "ag-grid-react";
import {
  AllCommunityModule,
  ModuleRegistry,
  type CellValueChangedEvent,
  type ColDef,
  type GridReadyEvent,
  type GridApi,
  type RowClassParams,
  type ValueFormatterParams,
  type ValueGetterParams,
  type ValueParserParams,
  type ValueSetterParams,
} from "ag-grid-community";
import { RefreshCw, RotateCcw, Save, Search } from "lucide-react";
import { accountsApi } from "@/features/accounts/api/accountsApi";
import {
  ACCOUNT_CATEGORY_NAMES,
  compareAccountCategoryIds,
  getAccountCategoryId,
  getAccountCategoryName,
  sortRowsByCategory,
} from "@/features/accounts/data/accountCategories";
import type { AccountGridRow } from "@/features/accounts/utils/accountMapper";
import {
  formatDateForGrid,
  formatPaymentDay,
  parseGridDate,
  toUpdateAccountRequest,
} from "@/features/accounts/utils/accountMapper";
import { accountGridTheme } from "@/features/accounts/components/accountGridTheme";
import { ColumnPicker } from "@/features/accounts/components/ColumnPicker";
import { DEFAULT_HIDDEN_COLUMNS } from "@/features/accounts/components/gridColumns";
import {
  restoreColumnState,
  saveColumnState,
} from "@/features/accounts/components/gridColumnState";
import { ApiError } from "@/lib/api/client";
import { formatCurrency, formatCurrencyDetailed } from "@/lib/format";

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

function parseOptionalInteger(value: unknown): number | null {
  if (value === "" || value === null || value === undefined) return null;
  const n = Number.parseInt(String(value), 10);
  return Number.isFinite(n) ? n : null;
}

const currencyFormatter = (params: ValueFormatterParams<AccountGridRow>) => {
  if (params.value === null || params.value === undefined) return "";
  return formatCurrencyDetailed(Number(params.value));
};

const currencyCol = {
  valueFormatter: currencyFormatter,
  valueParser: (p: ValueParserParams) => parseNumber(p.newValue),
  cellClass: "ag-cell-currency",
  headerClass: "ag-cell-currency-header",
  filter: "agNumberColumnFilter" as const,
};

export function AccountGrid() {
  const gridRef = useRef<AgGridReact<AccountGridRow>>(null);
  const dirtyIds = useRef(new Set<number>());
  const persistTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const columnStateReadyRef = useRef(false);
  const [rowData, setRowData] = useState<AccountGridRow[]>([]);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [pendingCount, setPendingCount] = useState(0);
  const [quickFilter, setQuickFilter] = useState("");
  const [gridApi, setGridApi] = useState<GridApi | null>(null);
  const [summaryTick, setSummaryTick] = useState(0);
  const [status, setStatus] = useState<{
    type: "success" | "error";
    message: string;
  } | null>(null);

  const loadAccounts = useCallback(async () => {
    setLoading(true);
    setStatus(null);
    try {
      const result = await accountsApi.getAll();
      setRowData(sortRowsByCategory(result.data ?? [], (row) => row.name));
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

  const { totalBalance, totalLimit } = useMemo(() => {
    return {
      totalBalance: rowData.reduce((sum, account) => sum + account.balance, 0),
      totalLimit: rowData.reduce((sum, account) => sum + account.limit, 0),
    };
  }, [rowData, summaryTick]);

  const columnDefs = useMemo<ColDef<AccountGridRow>[]>(
    () => [
      {
        field: "name",
        headerName: "Name",
        editable: true,
        filter: "agTextColumnFilter",
        minWidth: 180,
        pinned: "left",
        cellClass: "ag-cell-name",
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
        minWidth: 130,
        ...currencyCol,
      },
      {
        field: "limit",
        headerName: "Limit",
        editable: true,
        minWidth: 120,
        ...currencyCol,
      },
      {
        field: "monthlyPayment",
        headerName: "Monthly",
        editable: true,
        minWidth: 120,
        ...currencyCol,
        valueParser: (p: ValueParserParams) =>
          parseOptionalNumber(p.newValue),
      },
      {
        field: "paymentDay",
        headerName: "Payment day",
        editable: true,
        minWidth: 110,
        filter: "agNumberColumnFilter",
        cellClass: "ag-cell-center",
        valueFormatter: (p) => formatPaymentDay(p.value),
        valueParser: (p: ValueParserParams) =>
          parseOptionalInteger(p.newValue),
      },
      {
        colId: "accountCategoryName",
        headerName: "Category",
        editable: true,
        filter: "agTextColumnFilter",
        minWidth: 150,
        cellClass: "ag-cell-category",
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
        comparator: (_valueA, _valueB, nodeA, nodeB) => {
          const a = nodeA.data;
          const b = nodeB.data;
          if (!a || !b) return 0;
          return compareAccountCategoryIds(
            a.accountCategoryId,
            b.accountCategoryId,
            a.name,
            b.name,
          );
        },
      },
      {
        field: "isPaidOff",
        headerName: "Paid off",
        editable: true,
        cellEditor: "agCheckboxCellEditor",
        filter: true,
        width: 110,
        cellClass: "ag-cell-center",
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
        valueParser: (p) => parseGridDate(String(p.newValue ?? "")),
        minWidth: 130,
      },
      {
        field: "phone",
        headerName: "Phone",
        editable: true,
        filter: "agTextColumnFilter",
        minWidth: 130,
      },
      {
        field: "email",
        headerName: "Email",
        editable: true,
        filter: "agTextColumnFilter",
        minWidth: 200,
      },
      {
        field: "url",
        headerName: "URL",
        editable: true,
        filter: "agTextColumnFilter",
        minWidth: 220,
      },
      {
        field: "username",
        headerName: "Username",
        editable: true,
        filter: "agTextColumnFilter",
        minWidth: 130,
      },
      {
        field: "description",
        headerName: "Description",
        editable: true,
        filter: "agTextColumnFilter",
        minWidth: 200,
      },
      {
        field: "notes",
        headerName: "Notes",
        editable: true,
        filter: "agTextColumnFilter",
        minWidth: 240,
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
      suppressHeaderMenuButton: false,
      lockVisible: false,
      hide: false,
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
      setSummaryTick((tick) => tick + 1);
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

  const scheduleColumnStateSave = useCallback(() => {
    if (!columnStateReadyRef.current) return;

    if (persistTimerRef.current) {
      clearTimeout(persistTimerRef.current);
    }

    persistTimerRef.current = setTimeout(() => {
      const api = gridRef.current?.api;
      if (api) saveColumnState(api);
    }, 250);
  }, []);

  useEffect(
    () => () => {
      if (persistTimerRef.current) clearTimeout(persistTimerRef.current);
    },
    [],
  );

  const onGridReady = (event: GridReadyEvent) => {
    setGridApi(event.api);

    const restored = restoreColumnState(event.api);
    if (!restored) {
      event.api.setColumnsVisible([...DEFAULT_HIDDEN_COLUMNS], false);
      event.api.autoSizeColumns(
        ["name", "number", "accountCategoryName"],
        false,
      );
    }

    columnStateReadyRef.current = true;
  };

  return (
    <div className="space-y-4">
      <div className="gradient-hero rounded-2xl p-5 shadow-lg shadow-[var(--accent)]/20">
        <p className="text-sm font-medium text-white/80">Total balance</p>
        {loading ? (
          <div className="mt-2 h-9 w-36 animate-pulse rounded-lg bg-white/20" />
        ) : (
          <p className="mt-1 text-3xl font-bold tracking-tight text-white">
            {formatCurrency(totalBalance)}
          </p>
        )}
        {!loading && totalLimit > 0 ? (
          <p className="mt-2 text-xs text-white/70">
            {formatCurrency(totalLimit)} total credit limit · {rowData.length}{" "}
            account{rowData.length === 1 ? "" : "s"}
          </p>
        ) : !loading ? (
          <p className="mt-2 text-xs text-white/70">
            {rowData.length} account{rowData.length === 1 ? "" : "s"}
          </p>
        ) : null}
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

      <div className="account-grid-shell">
        <div className="account-grid-toolbar">
          <div className="account-grid-search">
            <Search size={16} aria-hidden />
            <input
              type="search"
              value={quickFilter}
              onChange={(e) => setQuickFilter(e.target.value)}
              placeholder="Search all columns…"
              aria-label="Search accounts"
            />
          </div>

          <div className="account-grid-toolbar-actions">
            <ColumnPicker gridApi={gridApi} />
            <button
              type="button"
              onClick={() => void handleSave()}
              disabled={pendingCount === 0 || saving}
              className="inline-flex items-center gap-2 rounded-full bg-[var(--link)] px-4 py-2 text-sm font-semibold text-white disabled:cursor-not-allowed disabled:opacity-40"
            >
              <Save size={15} aria-hidden />
              {saving
                ? "Saving…"
                : `Save${pendingCount > 0 ? ` (${pendingCount})` : ""}`}
            </button>
            <button
              type="button"
              onClick={handleDiscard}
              disabled={pendingCount === 0 || saving || loading}
              className="inline-flex items-center gap-2 rounded-full border border-[var(--border)] bg-white px-4 py-2 text-sm font-medium disabled:cursor-not-allowed disabled:opacity-40"
            >
              <RotateCcw size={15} aria-hidden />
              Discard
            </button>
            <button
              type="button"
              onClick={() => void loadAccounts()}
              disabled={loading || saving}
              className="inline-flex items-center gap-2 rounded-full border border-[var(--border)] bg-white px-4 py-2 text-sm font-medium disabled:cursor-not-allowed disabled:opacity-40"
            >
              <RefreshCw size={15} aria-hidden />
              Refresh
            </button>
          </div>

          <p className="account-grid-meta">
            {rowData.length} accounts
            {pendingCount > 0 ? ` · ${pendingCount} unsaved` : ""}
          </p>
        </div>

        <div className="account-grid-viewport">
          <AgGridReact<AccountGridRow>
            ref={gridRef}
            theme={accountGridTheme}
            rowData={rowData}
            columnDefs={columnDefs}
            defaultColDef={defaultColDef}
            getRowClass={getRowClass}
            onCellValueChanged={onCellValueChanged}
            onGridReady={onGridReady}
            onColumnVisible={scheduleColumnStateSave}
            onColumnMoved={scheduleColumnStateSave}
            onColumnResized={scheduleColumnStateSave}
            onColumnPinned={scheduleColumnStateSave}
            onSortChanged={scheduleColumnStateSave}
            loading={loading}
            quickFilterText={quickFilter}
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
            suppressDragLeaveHidesColumns={false}
            tooltipShowDelay={400}
          />
        </div>
      </div>

      <p className="text-center text-xs text-[var(--muted)] opacity-70">
        Double-click to edit · Column layout is saved in this browser · Save when
        done
      </p>
    </div>
  );
}
