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
import { RefreshCw, RotateCcw, Save, Search, Tags } from "lucide-react";
import { accountsApi } from "@/features/accounts/api/accountsApi";
import { AddCategoryDialog } from "@/features/accounts/components/AddCategoryDialog";
import {
  compareAccountCategoryIds,
  getAccountCategoryId,
  getAccountCategoryName,
  getAccountSubCategoryId,
  getAccountSubCategoryName,
  getCategoryNames,
  getSubCategoryNames,
  sortRowsByCategory,
} from "@/features/accounts/data/accountCategories";
import { useAccountCategories } from "@/features/accounts/hooks/useAccountCategories";
import type { AccountGridRow } from "@/features/accounts/utils/accountMapper";
import {
  formatDateForGrid,
  formatPaymentDay,
  calculateGraceDay,
  isCreditCardAccount,
  parseGridDate,
  toUpdateAccountRequest,
} from "@/features/accounts/utils/accountMapper";
import { accountGridTheme } from "@/features/accounts/components/accountGridTheme";
import { ColumnPicker } from "@/features/accounts/components/ColumnPicker";
import { CreditCardNameCellRenderer } from "@/features/accounts/components/CreditCardNameCellRenderer";
import { SyncAccountLogosButton } from "@/features/accounts/components/SyncAccountLogosButton";
import {
  ACCOUNT_EXCLUDED_COLUMNS,
  CREDIT_CARD_EXCLUDED_COLUMNS,
} from "@/features/accounts/components/gridColumns";
import {
  defaultHiddenColumns,
  filterExistingColIds,
  restoreColumnState,
  saveColumnState,
} from "@/features/accounts/components/gridColumnState";
import {
  editableUnlessPinned,
  isPinnedTotalRow,
  recalculatePinnedBottomRowData,
  type PinnedTotalsConfig,
} from "@/features/accounts/components/gridPinnedTotals";
import { GridActiveFilters } from "@/features/accounts/components/GridActiveFilters";
import { ApiError } from "@/lib/api/client";
import {
  formatCurrency,
  formatCurrencyDetailed,
  parseMoneyInput,
  parseMoneyInputOrZero,
} from "@/lib/format";

import "@/features/accounts/components/account-grid.css";

ModuleRegistry.registerModules([AllCommunityModule]);

const ACCOUNT_PINNED_TOTALS: PinnedTotalsConfig = {
  labelField: "name",
  sumFields: ["balance", "limit", "monthlyPayment"],
};

function parseNumber(value: unknown): number {
  return parseMoneyInputOrZero(value);
}

function parseOptionalNumber(value: unknown): number | null {
  if (value === "" || value === null || value === undefined) return null;
  return parseMoneyInput(value);
}

function parseOptionalInteger(value: unknown): number | null {
  if (value === "" || value === null || value === undefined) return null;
  const n = Number.parseInt(String(value), 10);
  return Number.isFinite(n) ? n : null;
}

function withHeaderTooltip<T>(def: ColDef<T>): ColDef<T> {
  if (def.headerTooltip) return def;
  if (typeof def.headerName !== "string" || !def.headerName) return def;
  return { ...def, headerTooltip: def.headerName };
}

function formatApr(value: unknown): string {
  if (value === null || value === undefined || value === "") return "";
  const n = Number(value);
  if (!Number.isFinite(n)) return "";
  return `${n.toFixed(2)}%`;
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

type AccountGridProps = {
  creditCardOnly?: boolean;
};

export function AccountGrid({ creditCardOnly = false }: AccountGridProps) {
  const columnStateNamespace = creditCardOnly
    ? "credit-cards-grid"
    : "accounts-grid";
  const entityLabel = creditCardOnly ? "credit card" : "account";
  const entityLabelPlural = creditCardOnly ? "credit cards" : "accounts";
  const gridRef = useRef<AgGridReact<AccountGridRow>>(null);
  const dirtyIds = useRef(new Set<number>());
  const persistTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const columnStateReadyRef = useRef(false);
  const [rowData, setRowData] = useState<AccountGridRow[]>([]);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [pendingCount, setPendingCount] = useState(0);
  const [quickFilter, setQuickFilter] = useState("");
  const [filterRevision, setFilterRevision] = useState(0);
  const [gridApi, setGridApi] = useState<GridApi | null>(null);
  const [pinnedBottomRowData, setPinnedBottomRowData] = useState<
    Record<string, unknown>[]
  >([]);
  const [summaryTick, setSummaryTick] = useState(0);
  const [logoVersion, setLogoVersion] = useState(0);
  const [addCategoryOpen, setAddCategoryOpen] = useState(false);
  const { categories, reload: reloadCategories } = useAccountCategories();
  const [status, setStatus] = useState<{
    type: "success" | "error";
    message: string;
  } | null>(null);

  const categoryNames = useMemo(
    () => getCategoryNames(categories),
    [categories],
  );

  const loadAccounts = useCallback(async () => {
    setLoading(true);
    setStatus(null);
    try {
      const result = await accountsApi.getAll();
      const accounts = (result.data ?? []).filter(
        (account) => !creditCardOnly || isCreditCardAccount(account),
      );
      setRowData(sortRowsByCategory(accounts, (row) => row.name, categories));
      dirtyIds.current.clear();
      setPendingCount(0);
    } catch (err) {
      const message =
        err instanceof ApiError
          ? err.message
          : err instanceof Error
            ? err.message
            : creditCardOnly
              ? "Failed to load credit cards"
              : "Failed to load accounts";
      setStatus({ type: "error", message });
    } finally {
      setLoading(false);
    }
  }, [categories, creditCardOnly]);

  useEffect(() => {
    void loadAccounts();
  }, [loadAccounts]);

  const { totalBalance, totalLimit } = useMemo(() => {
    return {
      totalBalance: rowData.reduce((sum, account) => sum + account.balance, 0),
      totalLimit: rowData.reduce((sum, account) => sum + account.limit, 0),
    };
  }, [rowData, summaryTick]);

  const columnDefs = useMemo<ColDef<AccountGridRow>[]>(() => {
    const defs: ColDef<AccountGridRow>[] = [
      {
        field: "name",
        headerName: "Name",
        editable: editableUnlessPinned(),
        filter: "agTextColumnFilter",
        minWidth: 180,
        pinned: "left",
        cellClass: "ag-cell-name",
        ...(creditCardOnly
          ? {
              cellRenderer: CreditCardNameCellRenderer,
              cellRendererParams: { logoVersion },
            }
          : {}),
      },
      {
        field: "number",
        headerName: "Number",
        editable: editableUnlessPinned(),
        filter: "agTextColumnFilter",
        minWidth: 120,
      },
      {
        field: "balance",
        headerName: "Balance",
        editable: editableUnlessPinned(),
        minWidth: 130,
        ...currencyCol,
      },
      {
        field: "limit",
        headerName: "Limit",
        editable: editableUnlessPinned(),
        minWidth: 120,
        ...currencyCol,
      },
      {
        field: "interestRate",
        headerName: "APR",
        editable: editableUnlessPinned(),
        width: 90,
        minWidth: 80,
        maxWidth: 110,
        filter: "agNumberColumnFilter",
        cellClass: "ag-cell-center",
        cellEditor: "agNumberCellEditor",
        cellEditorParams: {
          min: 0,
          max: 100,
          precision: 2,
          showStepperButtons: false,
        },
        valueFormatter: (p) => formatApr(p.value),
        valueParser: (p: ValueParserParams) =>
          parseOptionalNumber(p.newValue),
      },
      {
        field: "cashOutInterestRate",
        headerName: "Cash advance APR %",
        headerTooltip:
          "Optional. Only cards with a cash-advance APR or fee % set can receive cash-advance balance moves in payoff plans.",
        editable: editableUnlessPinned(),
        width: 150,
        minWidth: 130,
        maxWidth: 180,
        filter: "agNumberColumnFilter",
        cellClass: "ag-cell-center",
        cellEditor: "agNumberCellEditor",
        cellEditorParams: {
          min: 0,
          max: 100,
          precision: 2,
          showStepperButtons: false,
        },
        valueFormatter: (p) => formatApr(p.value),
        valueParser: (p: ValueParserParams) =>
          parseOptionalNumber(p.newValue),
      },
      {
        field: "cashAdvanceFeePercentage",
        headerName: "Cash advance fee %",
        headerTooltip:
          "Optional fee on cash advances. If cash-advance APR is blank, this percentage is also used as the cash-advance rate for payoff transfers. Leave both blank if the card cannot receive cash-advance moves.",
        editable: editableUnlessPinned(),
        width: 150,
        minWidth: 130,
        maxWidth: 180,
        filter: "agNumberColumnFilter",
        cellClass: "ag-cell-center",
        cellEditor: "agNumberCellEditor",
        cellEditorParams: {
          min: 0,
          max: 100,
          precision: 2,
          showStepperButtons: false,
        },
        valueFormatter: (p) => formatApr(p.value),
        valueParser: (p: ValueParserParams) =>
          parseOptionalNumber(p.newValue),
      },
      {
        field: "monthlyPayment",
        headerName: "Monthly",
        editable: editableUnlessPinned(),
        minWidth: 120,
        ...currencyCol,
        valueParser: (p: ValueParserParams) =>
          parseOptionalNumber(p.newValue),
      },
      {
        field: "paymentDay",
        headerName: "Payment day",
        editable: editableUnlessPinned(),
        minWidth: 110,
        filter: "agNumberColumnFilter",
        cellClass: "ag-cell-center",
        valueFormatter: (p) => formatPaymentDay(p.value),
        valueParser: (p: ValueParserParams) =>
          parseOptionalInteger(p.newValue),
        valueSetter: (params: ValueSetterParams<AccountGridRow>) => {
          if (!params.data) return false;
          params.data.paymentDay = parseOptionalInteger(params.newValue);
          params.data.graceDay = calculateGraceDay(
            params.data.paymentDay,
            params.data.gracePeriod,
          );
          return true;
        },
      },
      {
        field: "gracePeriod",
        headerName: "Grace",
        headerTooltip: "Grace period in days after the payment due date",
        editable: editableUnlessPinned(),
        width: 78,
        minWidth: 70,
        maxWidth: 90,
        filter: "agNumberColumnFilter",
        cellClass: "ag-cell-center",
        valueParser: (p: ValueParserParams) =>
          parseOptionalInteger(p.newValue),
        valueSetter: (params: ValueSetterParams<AccountGridRow>) => {
          if (!params.data) return false;
          params.data.gracePeriod = parseOptionalInteger(params.newValue);
          params.data.graceDay = calculateGraceDay(
            params.data.paymentDay,
            params.data.gracePeriod,
          );
          return true;
        },
      },
      {
        field: "graceDay",
        headerName: "Grace day",
        headerTooltip: "Auto-calculated from payment day + grace period",
        editable: false,
        width: 90,
        minWidth: 80,
        maxWidth: 110,
        filter: "agNumberColumnFilter",
        cellClass: "ag-cell-center",
        valueFormatter: (p) => formatPaymentDay(p.value),
      },
      {
        colId: "accountCategoryName",
        headerName: "Category",
        editable: editableUnlessPinned(),
        filter: "agTextColumnFilter",
        minWidth: 150,
        cellClass: "ag-cell-category",
        valueGetter: (params: ValueGetterParams<AccountGridRow>) =>
          params.data
            ? params.data.accountCategoryName ??
              getAccountCategoryName(params.data.accountCategoryId, categories)
            : "",
        valueSetter: (params: ValueSetterParams<AccountGridRow>) => {
          if (!params.data) return false;
          const categoryId = getAccountCategoryId(
            String(params.newValue),
            categories,
          );
          if (categoryId === undefined) return false;
          params.data.accountCategoryId = categoryId;
          params.data.accountCategoryName = String(params.newValue);
          const currentSubName = getAccountSubCategoryName(
            categoryId,
            params.data.accountSubCategoryId,
            categories,
          );
          if (!currentSubName) {
            params.data.accountSubCategoryId = null;
            params.data.accountSubCategoryName = null;
          }
          return true;
        },
        cellEditor: "agSelectCellEditor",
        cellEditorParams: {
          values: categoryNames,
        },
        filterValueGetter: (params: ValueGetterParams<AccountGridRow>) =>
          params.data
            ? params.data.accountCategoryName ??
              getAccountCategoryName(params.data.accountCategoryId, categories)
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
            categories,
          );
        },
      },
      {
        colId: "accountSubCategoryName",
        headerName: "Subcategory",
        editable: editableUnlessPinned(),
        filter: "agTextColumnFilter",
        minWidth: 150,
        valueGetter: (params: ValueGetterParams<AccountGridRow>) =>
          params.data
            ? params.data.accountSubCategoryName ??
              getAccountSubCategoryName(
                params.data.accountCategoryId,
                params.data.accountSubCategoryId,
                categories,
              )
            : "",
        valueSetter: (params: ValueSetterParams<AccountGridRow>) => {
          if (!params.data) return false;
          const raw = String(params.newValue ?? "").trim();
          if (!raw) {
            params.data.accountSubCategoryId = null;
            params.data.accountSubCategoryName = null;
            return true;
          }
          const subCategoryId = getAccountSubCategoryId(
            params.data.accountCategoryId,
            raw,
            categories,
          );
          if (subCategoryId === undefined) return false;
          params.data.accountSubCategoryId = subCategoryId;
          params.data.accountSubCategoryName = raw;
          return true;
        },
        cellEditor: "agSelectCellEditor",
        cellEditorParams: (params: { data?: AccountGridRow }) => ({
          values: [
            "",
            ...getSubCategoryNames(
              params.data?.accountCategoryId ?? 0,
              categories,
            ),
          ],
        }),
      },
      {
        colId: "excludeFromPayoff",
        headerName: "Exclude payoff",
        headerTooltip:
          "Exclude this card from avalanche/snowball payoff analysis (e.g. creditor payment plan).",
        editable: editableUnlessPinned(),
        cellEditor: "agCheckboxCellEditor",
        filter: true,
        width: 130,
        cellClass: "ag-cell-center",
        valueGetter: (params: ValueGetterParams<AccountGridRow>) =>
          params.data
            ? !(params.data.includeInPayoffAnalysis ?? true)
            : false,
        valueSetter: (params: ValueSetterParams<AccountGridRow>) => {
          if (!params.data) return false;
          params.data.includeInPayoffAnalysis = !Boolean(params.newValue);
          return true;
        },
      },
      {
        field: "isPaidOff",
        headerName: "Paid off",
        editable: editableUnlessPinned(),
        cellEditor: "agCheckboxCellEditor",
        filter: true,
        width: 110,
        cellClass: "ag-cell-center",
      },
      {
        field: "accountOpenDate",
        headerName: "Opened",
        editable: editableUnlessPinned(),
        filter: "agDateColumnFilter",
        valueFormatter: (p) => formatDateForGrid(p.value ?? null),
        valueParser: (p) =>
          parseGridDate(String(p.newValue ?? "")) ?? p.oldValue,
        minWidth: 120,
      },
      {
        field: "paidOffDate",
        headerName: "Paid off date",
        editable: editableUnlessPinned(),
        filter: "agDateColumnFilter",
        valueFormatter: (p) => formatDateForGrid(p.value ?? null),
        valueParser: (p) => parseGridDate(String(p.newValue ?? "")),
        minWidth: 130,
      },
      {
        field: "phone",
        headerName: "Phone",
        editable: editableUnlessPinned(),
        filter: "agTextColumnFilter",
        minWidth: 130,
      },
      {
        field: "email",
        headerName: "Email",
        editable: editableUnlessPinned(),
        filter: "agTextColumnFilter",
        minWidth: 200,
      },
      {
        field: "url",
        headerName: "URL",
        editable: editableUnlessPinned(),
        filter: "agTextColumnFilter",
        minWidth: 220,
      },
      {
        field: "username",
        headerName: "Username",
        editable: editableUnlessPinned(),
        filter: "agTextColumnFilter",
        minWidth: 130,
      },
      {
        field: "description",
        headerName: "Description",
        editable: editableUnlessPinned(),
        filter: "agTextColumnFilter",
        minWidth: 200,
      },
      {
        field: "notes",
        headerName: "Notes",
        editable: editableUnlessPinned(),
        filter: "agTextColumnFilter",
        minWidth: 240,
        flex: 1,
      },
    ];

    if (!creditCardOnly) {
      return defs
        .filter((column) => {
          const colId = column.colId ?? column.field;
          return !ACCOUNT_EXCLUDED_COLUMNS.has(String(colId));
        })
        .map(withHeaderTooltip);
    }

    return defs
      .filter((column) => {
        const colId = column.colId ?? column.field;
        return !CREDIT_CARD_EXCLUDED_COLUMNS.has(String(colId));
      })
      .map(withHeaderTooltip);
  }, [categories, categoryNames, creditCardOnly, logoVersion]);

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

  const refreshPinnedTotals = useCallback((api?: GridApi | null) => {
    const targetApi = api ?? gridRef.current?.api;
    if (!targetApi) return;
    setPinnedBottomRowData(
      recalculatePinnedBottomRowData(targetApi, ACCOUNT_PINNED_TOTALS),
    );
  }, []);

  const getRowClass = useCallback(
    (params: RowClassParams<AccountGridRow>) => {
      if (isPinnedTotalRow(params.node)) {
        return "account-grid-total-row";
      }
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
      refreshPinnedTotals(event.api);
    },
    [refreshPinnedTotals],
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
        message: `Saved ${rowsToSave.length} ${rowsToSave.length === 1 ? entityLabel : entityLabelPlural}.`,
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
      if (api) saveColumnState(api, columnStateNamespace);
    }, 250);
  }, [columnStateNamespace]);

  useEffect(
    () => () => {
      if (persistTimerRef.current) clearTimeout(persistTimerRef.current);
    },
    [],
  );

  useEffect(() => {
    refreshPinnedTotals();
  }, [rowData, summaryTick, refreshPinnedTotals]);

  const onGridReady = (event: GridReadyEvent) => {
    setGridApi(event.api);

    const restored = restoreColumnState(event.api, columnStateNamespace);
    if (!restored) {
      const hiddenColumns = filterExistingColIds(
        event.api,
        defaultHiddenColumns(columnStateNamespace),
      );
      if (hiddenColumns.length > 0) {
        event.api.setColumnsVisible(hiddenColumns, false);
      }
      event.api.autoSizeColumns(
        creditCardOnly ? ["name", "number"] : ["name", "number", "accountCategoryName"],
        false,
      );
    }

    columnStateReadyRef.current = true;
    refreshPinnedTotals(event.api);
  };

  return (
    <div className="space-y-4">
      {creditCardOnly ? (
        <SyncAccountLogosButton
          onSynced={() => {
            setLogoVersion((value) => value + 1);
            gridRef.current?.api?.refreshCells({ columns: ["name"], force: true });
          }}
        />
      ) : null}
      <div className="gradient-hero rounded-2xl p-5 shadow-lg shadow-[var(--accent)]/20">
        <p className="text-sm font-medium text-white/80">
          {creditCardOnly ? "Total card balance" : "Total balance"}
        </p>
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
            {rowData.length === 1 ? entityLabel : entityLabelPlural}
          </p>
        ) : !loading ? (
          <p className="mt-2 text-xs text-white/70">
            {rowData.length}{" "}
            {rowData.length === 1 ? entityLabel : entityLabelPlural}
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
              placeholder={`Search all columns…`}
              aria-label={`Search ${entityLabelPlural}`}
            />
          </div>

          <div className="account-grid-toolbar-actions">
            {!creditCardOnly ? (
              <button
                type="button"
                onClick={() => setAddCategoryOpen(true)}
                className="inline-flex items-center gap-2 rounded-full border border-[var(--border)] bg-white px-4 py-2 text-sm font-medium"
              >
                <Tags size={15} aria-hidden />
                Categories
              </button>
            ) : null}
            <ColumnPicker
              gridApi={gridApi}
              columnStateNamespace={columnStateNamespace}
              creditCardOnly={creditCardOnly}
            />
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
            {rowData.length} {entityLabelPlural}
            {pendingCount > 0 ? ` · ${pendingCount} unsaved` : ""}
          </p>
        </div>

        <GridActiveFilters
          gridApi={gridApi}
          quickFilter={quickFilter}
          onQuickFilterChange={setQuickFilter}
          filterRevision={filterRevision}
          onFiltersCleared={() => {
            setFilterRevision((revision) => revision + 1);
            refreshPinnedTotals();
          }}
        />

        <div className="account-grid-viewport">
          <AgGridReact<AccountGridRow>
            ref={gridRef}
            theme={accountGridTheme}
            rowData={rowData}
            pinnedBottomRowData={pinnedBottomRowData}
            columnDefs={columnDefs}
            defaultColDef={defaultColDef}
            getRowClass={getRowClass}
            onCellValueChanged={onCellValueChanged}
            onGridReady={onGridReady}
            onFilterChanged={() => {
              setFilterRevision((revision) => revision + 1);
              refreshPinnedTotals();
            }}
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
      {addCategoryOpen ? (
        <AddCategoryDialog
          categories={categories}
          onClose={() => setAddCategoryOpen(false)}
          onSaved={async () => {
            await reloadCategories();
          }}
        />
      ) : null}
    </div>
  );
}
