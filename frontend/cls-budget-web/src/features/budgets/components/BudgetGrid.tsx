"use client";

import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { AgGridReact } from "ag-grid-react";
import {
  AllCommunityModule,
  ModuleRegistry,
  type CellValueChangedEvent,
  type ColDef,
  type GridApi,
  type GridReadyEvent,
  type RowClassParams,
  type ValueFormatterParams,
  type ValueGetterParams,
  type ValueParserParams,
  type ValueSetterParams,
} from "ag-grid-community";
import { RefreshCw, RotateCcw, Save, Search } from "lucide-react";
import { accountsApi } from "@/features/accounts/api/accountsApi";
import {
  getAccountCategoryName,
  compareAccountCategoryIds,
  sortRowsByCategory,
} from "@/features/accounts/data/accountCategories";
import {
  formatDateForGrid,
  formatPaymentDay,
  parseGridDate,
  toUpdateAccountRequest,
} from "@/features/accounts/utils/accountMapper";
import type { AccountResponse } from "@/features/accounts/types/account";
import { accountGridTheme } from "@/features/accounts/components/accountGridTheme";
import { BudgetColumnPicker } from "@/features/budgets/components/BudgetColumnPicker";
import { BUDGET_DEFAULT_HIDDEN_COLUMNS } from "@/features/budgets/components/budgetGridColumns";
import {
  restoreBudgetColumnState,
  saveBudgetColumnState,
} from "@/features/budgets/components/budgetGridColumnState";
import { budgetsApi } from "@/features/budgets/api/budgetsApi";
import { formatBudgetPeriod } from "@/features/budgets/utils/budgetFormat";
import {
  summarizePaymentsByHalfMonth,
  type PaymentHalfSummary,
} from "@/features/budgets/utils/budgetPaymentSummary";
import {
  buildBudgetGridRows,
  getBudgetStatusRowClass,
  toUpdatePaymentRequest,
  type BudgetGridRow,
} from "@/features/budgets/utils/budgetGridMapper";
import { paymentsApi } from "@/features/payments/api/paymentsApi";
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

const currencyFormatter = (params: ValueFormatterParams<BudgetGridRow>) => {
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

function PaymentHalfPanel({
  title,
  summary,
}: {
  title: string;
  summary: PaymentHalfSummary;
}) {
  return (
    <div className="rounded-xl bg-white/10 p-4 backdrop-blur-sm">
      <p className="text-sm font-semibold text-white">{title}</p>
      <dl className="mt-3 space-y-2 text-sm">
        <div className="flex items-center justify-between gap-4">
          <dt className="text-white/75">Pending</dt>
          <dd className="font-semibold tabular-nums text-white">
            {formatCurrency(summary.pending)}
          </dd>
        </div>
        <div className="flex items-center justify-between gap-4">
          <dt className="text-white/75">Paid</dt>
          <dd className="font-semibold tabular-nums text-white">
            {formatCurrency(summary.paid)}
          </dd>
        </div>
        <div className="flex items-center justify-between gap-4">
          <dt className="text-white/75">Scheduled</dt>
          <dd className="font-semibold tabular-nums text-white">
            {formatCurrency(summary.scheduled)}
          </dd>
        </div>
      </dl>
    </div>
  );
}

export function BudgetGrid({ budgetId }: { budgetId: number }) {
  const gridRef = useRef<AgGridReact<BudgetGridRow>>(null);
  const dirtyIds = useRef(new Set<number>());
  const dirtyAccountIds = useRef(new Set<number>());
  const accountsByIdRef = useRef<Map<number, AccountResponse>>(new Map());
  const persistTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const columnStateReadyRef = useRef(false);
  const [rowData, setRowData] = useState<BudgetGridRow[]>([]);
  const [budgetName, setBudgetName] = useState("");
  const [budgetPeriod, setBudgetPeriod] = useState("");
  const [budgetStartPeriod, setBudgetStartPeriod] = useState("");
  const [statusNames, setStatusNames] = useState<string[]>([]);
  const [statusByName, setStatusByName] = useState<Map<string, number>>(
    new Map(),
  );
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

  const loadData = useCallback(async () => {
    setLoading(true);
    setStatus(null);
    try {
      const [budgetResult, paymentsResult, accountsResult, statusesResult] =
        await Promise.all([
          budgetsApi.getById(budgetId),
          paymentsApi.getAll(),
          accountsApi.getAll(),
          paymentsApi.getStatuses(),
        ]);

      const budget = budgetResult.data;
      if (!budget) {
        throw new ApiError(404, "Budget not found");
      }

      const accountsById = new Map(
        (accountsResult.data ?? []).map((account) => [
          account.accountId,
          account,
        ]),
      );
      accountsByIdRef.current = accountsById;
      const payments = (paymentsResult.data ?? []).filter(
        (payment) => payment.budgetId === budgetId,
      );

      const statuses = statusesResult.data ?? [];
      setStatusNames(statuses.map((s) => s.name));
      setStatusByName(
        new Map(statuses.map((s) => [s.name, s.budgetPaymentStatusId])),
      );

      setBudgetName(budget.name);
      setBudgetStartPeriod(budget.startPeriod);
      setBudgetPeriod(
        formatBudgetPeriod(budget.startPeriod, budget.endPeriod),
      );
      setRowData(
        sortRowsByCategory(
          buildBudgetGridRows(payments, accountsById),
          (row) => row.accountName,
        ),
      );
      dirtyIds.current.clear();
      dirtyAccountIds.current.clear();
      setPendingCount(0);
    } catch (err) {
      const message =
        err instanceof ApiError
          ? err.message
          : err instanceof Error
            ? err.message
            : "Failed to load budget";
      setStatus({ type: "error", message });
    } finally {
      setLoading(false);
    }
  }, [budgetId]);

  useEffect(() => {
    void loadData();
  }, [loadData]);

  const { totalBudgeted, totalPaid, clearedCount, paymentHalves } = useMemo(() => {
    const halves = budgetStartPeriod
      ? summarizePaymentsByHalfMonth(rowData, budgetStartPeriod)
      : null;

    return {
      totalBudgeted: rowData.reduce((sum, row) => sum + row.amount, 0),
      totalPaid: rowData.reduce((sum, row) => sum + row.paymentMade, 0),
      clearedCount: rowData.filter((row) => row.isCleared).length,
      paymentHalves: halves,
    };
  }, [rowData, summaryTick, budgetStartPeriod]);

  const columnDefs = useMemo<ColDef<BudgetGridRow>[]>(
    () => [
      {
        colId: "accountName",
        field: "accountName",
        headerName: "Account",
        filter: "agTextColumnFilter",
        minWidth: 180,
        pinned: "left",
        cellClass: "ag-cell-name",
        editable: false,
      },
      {
        colId: "accountCategoryName",
        headerName: "Category",
        filter: "agTextColumnFilter",
        minWidth: 150,
        cellClass: "ag-cell-category",
        editable: false,
        valueGetter: (params: ValueGetterParams<BudgetGridRow>) =>
          params.data
            ? getAccountCategoryName(params.data.accountCategoryId)
            : "",
        filterValueGetter: (params: ValueGetterParams<BudgetGridRow>) =>
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
            a.accountName,
            b.accountName,
          );
        },
      },
      {
        colId: "accountPaymentDay",
        field: "accountPaymentDay",
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
        colId: "amount",
        field: "amount",
        headerName: "Budgeted",
        editable: true,
        minWidth: 120,
        ...currencyCol,
      },
      {
        colId: "paymentMade",
        field: "paymentMade",
        headerName: "Paid",
        editable: true,
        minWidth: 120,
        ...currencyCol,
      },
      {
        colId: "budgetPaymentStatusName",
        headerName: "Status",
        editable: true,
        filter: "agTextColumnFilter",
        minWidth: 130,
        cellClass: "ag-cell-category",
        valueGetter: (params: ValueGetterParams<BudgetGridRow>) =>
          params.data?.budgetPaymentStatusName ?? "",
        valueSetter: (params: ValueSetterParams<BudgetGridRow>) => {
          if (!params.data) return false;
          const id = statusByName.get(String(params.newValue));
          if (id === undefined) return false;
          params.data.budgetPaymentStatusId = id;
          params.data.budgetPaymentStatusName = String(params.newValue);
          return true;
        },
        cellEditor: "agSelectCellEditor",
        cellEditorParams: {
          values: statusNames,
        },
      },
      {
        colId: "isCleared",
        field: "isCleared",
        headerName: "Cleared",
        editable: true,
        cellEditor: "agCheckboxCellEditor",
        filter: true,
        width: 110,
        cellClass: "ag-cell-center",
      },
      {
        colId: "paymentDate",
        field: "paymentDate",
        headerName: "Payment date",
        editable: true,
        filter: "agDateColumnFilter",
        minWidth: 130,
        valueFormatter: (p) => formatDateForGrid(p.value ?? null),
        valueParser: (p) =>
          parseGridDate(String(p.newValue ?? "")) ?? p.oldValue,
      },
      {
        colId: "clearedDate",
        field: "clearedDate",
        headerName: "Cleared date",
        editable: true,
        filter: "agDateColumnFilter",
        minWidth: 130,
        valueFormatter: (p) => formatDateForGrid(p.value ?? null),
        valueParser: (p) => parseGridDate(String(p.newValue ?? "")),
      },
      {
        colId: "accountNumber",
        field: "accountNumber",
        headerName: "Account number",
        filter: "agTextColumnFilter",
        minWidth: 120,
        editable: false,
      },
      {
        colId: "accountBalance",
        field: "accountBalance",
        headerName: "Balance",
        minWidth: 120,
        editable: false,
        ...currencyCol,
      },
      {
        colId: "accountMonthlyPayment",
        field: "accountMonthlyPayment",
        headerName: "Monthly (account)",
        minWidth: 130,
        editable: false,
        ...currencyCol,
        valueFormatter: (p) =>
          p.value === null || p.value === undefined
            ? ""
            : formatCurrencyDetailed(Number(p.value)),
        valueParser: (p: ValueParserParams) =>
          parseOptionalNumber(p.newValue),
      },
      {
        colId: "paymentSourceId",
        field: "paymentSourceId",
        headerName: "Payment source",
        editable: true,
        filter: "agNumberColumnFilter",
        minWidth: 120,
        valueParser: (p: ValueParserParams) =>
          parseOptionalNumber(p.newValue),
      },
    ],
    [statusNames, statusByName],
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
    (params: RowClassParams<BudgetGridRow>) => {
      if (!params.data) return "";

      if (dirtyIds.current.has(params.data.budgetPaymentId)) {
        return "account-row-dirty";
      }

      if (dirtyAccountIds.current.has(params.data.accountId)) {
        return "account-row-dirty";
      }

      return getBudgetStatusRowClass(params.data.budgetPaymentStatusName);
    },
    [pendingCount],
  );

  const onCellValueChanged = useCallback(
    (event: CellValueChangedEvent<BudgetGridRow>) => {
      if (!event.data || event.oldValue === event.newValue) return;

      if (event.column.getColId() === "accountPaymentDay") {
        dirtyAccountIds.current.add(event.data.accountId);
      } else {
        dirtyIds.current.add(event.data.budgetPaymentId);
      }

      setPendingCount(
        dirtyIds.current.size + dirtyAccountIds.current.size,
      );
      setSummaryTick((tick) => tick + 1);
      event.api.redrawRows({ rowNodes: [event.node] });
    },
    [],
  );

  const handleSave = async () => {
    const rowsToSave = rowData.filter((row) =>
      dirtyIds.current.has(row.budgetPaymentId),
    );
    const accountIdsToSave = [...dirtyAccountIds.current];
    if (rowsToSave.length === 0 && accountIdsToSave.length === 0) return;

    setSaving(true);
    setStatus(null);
    try {
      await Promise.all([
        ...rowsToSave.map((row) =>
          paymentsApi.update(row.budgetPaymentId, toUpdatePaymentRequest(row)),
        ),
        ...accountIdsToSave.map(async (accountId) => {
          const account = accountsByIdRef.current.get(accountId);
          const row = rowData.find((r) => r.accountId === accountId);
          if (!account || !row) return;

          await accountsApi.update(
            accountId,
            toUpdateAccountRequest({
              ...account,
              paymentDay: row.accountPaymentDay,
            }),
          );
        }),
      ]);
      dirtyIds.current.clear();
      dirtyAccountIds.current.clear();
      setPendingCount(0);

      const parts: string[] = [];
      if (rowsToSave.length > 0) {
        parts.push(
          `${rowsToSave.length} payment${rowsToSave.length === 1 ? "" : "s"}`,
        );
      }
      if (accountIdsToSave.length > 0) {
        parts.push(
          `${accountIdsToSave.length} account${accountIdsToSave.length === 1 ? "" : "s"}`,
        );
      }
      setStatus({
        type: "success",
        message: `Saved ${parts.join(" and ")}.`,
      });
      await loadData();
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
    void loadData();
  };

  const scheduleColumnStateSave = useCallback(() => {
    if (!columnStateReadyRef.current) return;

    if (persistTimerRef.current) {
      clearTimeout(persistTimerRef.current);
    }

    persistTimerRef.current = setTimeout(() => {
      const api = gridRef.current?.api;
      if (api) saveBudgetColumnState(api);
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

    const restored = restoreBudgetColumnState(event.api);
    if (!restored) {
      event.api.setColumnsVisible([...BUDGET_DEFAULT_HIDDEN_COLUMNS], false);
      event.api.autoSizeColumns(["accountName", "accountCategoryName"], false);
    }

    columnStateReadyRef.current = true;
  };

  return (
    <div className="space-y-4">
      <div className="gradient-hero rounded-2xl p-5 shadow-lg shadow-[var(--accent)]/20">
        <div className="flex flex-wrap items-start justify-between gap-4">
          <div>
            <p className="text-sm font-medium text-white/80">
              {budgetName || "Budget"}
            </p>
            {loading ? (
              <div className="mt-2 h-9 w-36 animate-pulse rounded-lg bg-white/20" />
            ) : (
              <p className="mt-1 text-3xl font-bold tracking-tight text-white">
                {formatCurrency(totalBudgeted)}
              </p>
            )}
            {!loading ? (
              <p className="mt-2 text-xs text-white/70">
                {formatCurrency(totalPaid)} paid · {clearedCount} of{" "}
                {rowData.length} cleared
                {budgetPeriod ? ` · ${budgetPeriod}` : ""}
              </p>
            ) : null}
          </div>
        </div>

        {!loading && paymentHalves ? (
          <div className="mt-5 grid gap-4 sm:grid-cols-2">
            <PaymentHalfPanel
              title={`Before ${paymentHalves.cutoffLabel}`}
              summary={paymentHalves.before}
            />
            <PaymentHalfPanel
              title={`After ${paymentHalves.cutoffLabel}`}
              summary={paymentHalves.after}
            />
          </div>
        ) : loading ? (
          <div className="mt-5 grid gap-4 sm:grid-cols-2">
            <div className="h-28 animate-pulse rounded-xl bg-white/10" />
            <div className="h-28 animate-pulse rounded-xl bg-white/10" />
          </div>
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
              aria-label="Search budget payments"
            />
          </div>

          <div className="account-grid-toolbar-actions">
            <BudgetColumnPicker gridApi={gridApi} />
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
              onClick={() => void loadData()}
              disabled={loading || saving}
              className="inline-flex items-center gap-2 rounded-full border border-[var(--border)] bg-white px-4 py-2 text-sm font-medium disabled:cursor-not-allowed disabled:opacity-40"
            >
              <RefreshCw size={15} aria-hidden />
              Refresh
            </button>
          </div>

          <p className="account-grid-meta">
            {rowData.length} payment{rowData.length === 1 ? "" : "s"}
            {pendingCount > 0 ? ` · ${pendingCount} unsaved` : ""}
          </p>
        </div>

        <div className="account-grid-viewport">
          <AgGridReact<BudgetGridRow>
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
