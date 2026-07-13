"use client";

import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { AgGridReact } from "ag-grid-react";
import {
  AllCommunityModule,
  ModuleRegistry,
  type CellEditingStartedEvent,
  type CellEditingStoppedEvent,
  type CellValueChangedEvent,
  type ColDef,
  type GridApi,
  type GridReadyEvent,
  type ICellRendererParams,
  type IRowNode,
  type RowClassParams,
  type ValueFormatterParams,
  type ValueGetterParams,
  type ValueParserParams,
  type ValueSetterParams,
} from "ag-grid-community";
import { Plus, RefreshCw, RotateCcw, Save, Search, Settings2, X } from "lucide-react";
import { accountsApi } from "@/features/accounts/api/accountsApi";
import {
  getAccountCategoryName,
  compareAccountCategoryIds,
} from "@/features/accounts/data/accountCategories";
import {
  formatDateForGrid,
  formatPaymentDay,
  normalizeGridDateIso,
  parseAndNormalizeGridDate,
  toUpdateAccountRequest,
} from "@/features/accounts/utils/accountMapper";
import { createEditableDateColDef } from "@/features/accounts/components/gridDateColumn";
import type { AccountResponse } from "@/features/accounts/types/account";
import { accountGridTheme } from "@/features/accounts/components/accountGridTheme";
import {
  editableUnlessPinned,
  isPinnedTotalRow,
  recalculatePinnedBottomRowData,
  type PinnedTotalsConfig,
} from "@/features/accounts/components/gridPinnedTotals";
import { GridActiveFilters } from "@/features/accounts/components/GridActiveFilters";
import { BudgetColumnPicker } from "@/features/budgets/components/BudgetColumnPicker";
import { BudgetNotesSection } from "@/features/budgets/components/BudgetNotesSection";
import { AddBudgetAccountDialog } from "@/features/budgets/components/AddBudgetAccountDialog";
import { AddBudgetPaymentDialog } from "@/features/budgets/components/AddBudgetPaymentDialog";
import { AddIncomeDialog } from "@/features/incomes/components/AddIncomeDialog";
import { AccountNameCellRenderer } from "@/features/budgets/components/AccountNameCellRenderer";
import { BUDGET_GRID_MODE_LABELS, type BudgetGridMode } from "@/features/budgets/components/budgetGridColumns";
import {
  applyBudgetGridMode,
  applyDefaultBudgetColumnVisibility,
  saveBudgetColumnState,
  saveBudgetGridMode,
} from "@/features/budgets/components/budgetGridColumnState";
import { budgetsApi } from "@/features/budgets/api/budgetsApi";
import type { UpdateBudgetRequest } from "@/features/budgets/types/budget";
import { formatBudgetPeriod } from "@/features/budgets/utils/budgetFormat";
import { parseBudgetNotes, serializeBudgetNotes } from "@/features/budgets/utils/budgetNotes";
import {
  summarizePaymentsByPayPeriod,
  getPaymentHalfSummaryTotal,
  rowMatchesPayPeriodFilter,
  type PayPeriodFilter,
  type PaymentClearedSummary,
  type PaymentHalfSummary,
} from "@/features/budgets/utils/budgetPaymentSummary";
import { PayScheduleSettingsDialog } from "@/features/pay-schedules/components/PayScheduleSettingsDialog";
import { paySchedulesApi } from "@/features/pay-schedules/api/paySchedulesApi";
import { toScheduleConfig } from "@/features/pay-schedules/utils/payDateCalculator";
import type { PayScheduleResponse } from "@/features/pay-schedules/types/paySchedule";
import {
  buildBudgetGridRows,
  calculateOwed,
  getBudgetPaymentRowClass,
  getDayOfMonthFromIso,
  isFirstPaymentRowForAccount,
  parseDayOfMonthInput,
  paymentDateFromDayOfMonth,
  toUpdatePaymentRequest,
  type BudgetGridRow,
} from "@/features/budgets/utils/budgetGridMapper";
import { incomesApi } from "@/features/incomes/api/incomesApi";
import { incomeSourcesApi } from "@/features/incomes/api/incomeSourcesApi";
import type { IncomeSummaryResponse } from "@/features/incomes/types/income";
import { paymentsApi } from "@/features/payments/api/paymentsApi";
import type { BudgetPaymentStatusResponse } from "@/features/payments/types/payment";
import { paymentSourcesApi } from "@/features/payments/api/paymentSourcesApi";
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

function withHeaderTooltip<T>(def: ColDef<T>): ColDef<T> {
  if (def.headerTooltip) return def;
  if (typeof def.headerName !== "string" || !def.headerName) return def;
  return { ...def, headerTooltip: def.headerName };
}

function autoSizeVisibleBudgetColumns(api: GridApi) {
  const ids =
    api
      .getColumns()
      ?.filter((column) => column.isVisible() && column.getColId() !== "actions")
      .map((column) => column.getColId()) ?? [];
  if (ids.length > 0) {
    api.autoSizeColumns(ids, false);
  }
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

const PAYMENT_SOURCE_NONE = "(None)";
const INCOME_SOURCE_NONE = "(None)";

const BUDGET_PINNED_TOTALS: PinnedTotalsConfig = {
  labelField: "accountName",
  sumFields: ["amount", "paymentMade", "accountBalance", "accountMonthlyPayment"],
};

interface BudgetGridContext {
  rowData: BudgetGridRow[];
  budgetAccountCount: number;
  accountMutating: boolean;
  paymentMutating: boolean;
  onRemoveAccount: (accountId: number, accountName: string) => void;
  onDeletePayment: (budgetPaymentId: number) => void;
  onAddPaymentForAccount: (accountId: number) => void;
}

function PaymentHalfPanel({
  title,
  summary,
  cleared,
  isActive = false,
  onSelect,
}: {
  title: string;
  summary: PaymentHalfSummary;
  cleared: PaymentClearedSummary;
  isActive?: boolean;
  onSelect?: () => void;
}) {
  const total = getPaymentHalfSummaryTotal(summary);

  return (
    <button
      type="button"
      onClick={onSelect}
      className={`w-full rounded-xl bg-white/10 p-4 text-left backdrop-blur-sm transition ${
        onSelect ? "cursor-pointer hover:bg-white/15" : ""
      } ${
        isActive
          ? "ring-2 ring-white/80 ring-offset-2 ring-offset-[var(--link)]"
          : ""
      }`}
      aria-pressed={isActive}
    >
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
        <div className="flex items-center justify-between gap-4 border-t border-white/20 pt-2">
          <dt className="font-semibold text-white">Total</dt>
          <dd className="font-semibold tabular-nums text-white">
            {formatCurrency(total)}
          </dd>
        </div>
      </dl>
      <div className="mt-3 rounded-lg border border-emerald-300/35 bg-emerald-500/25 px-3 py-2.5">
        <div className="flex items-center justify-between gap-4 text-sm">
          <p className="font-semibold text-emerald-50">Cleared</p>
          <p className="font-semibold tabular-nums text-white">
            {formatCurrency(cleared.amount)}
          </p>
        </div>
        <p className="mt-1 text-xs text-emerald-100/85">
          {cleared.count} of {cleared.total}{" "}
          {cleared.total === 1 ? "payment" : "payments"}
        </p>
      </div>
    </button>
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
  const [budgetEndPeriod, setBudgetEndPeriod] = useState("");
  const [budgetTemplateId, setBudgetTemplateId] = useState(1);
  const [budgetNotes, setBudgetNotes] = useState<string[]>([]);
  const [paySchedule, setPaySchedule] = useState<PayScheduleResponse | null>(
    null,
  );
  const [paySchedules, setPaySchedules] = useState<PayScheduleResponse[]>([]);
  const [payScheduleSettingsOpen, setPayScheduleSettingsOpen] = useState(false);
  const [scheduleMutating, setScheduleMutating] = useState(false);
  const [statusNames, setStatusNames] = useState<string[]>([]);
  const [statusByName, setStatusByName] = useState<Map<string, number>>(
    new Map(),
  );
  const [paymentSourceNames, setPaymentSourceNames] = useState<string[]>([]);
  const [paymentSourceByName, setPaymentSourceByName] = useState<
    Map<string, number>
  >(new Map());
  const [incomeSourceNames, setIncomeSourceNames] = useState<string[]>([]);
  const [incomeSourceByName, setIncomeSourceByName] = useState<
    Map<string, number>
  >(new Map());
  const [incomeSummary, setIncomeSummary] = useState<IncomeSummaryResponse | null>(
    null,
  );
  const [budgetAccountIds, setBudgetAccountIds] = useState<number[]>([]);
  const [paymentStatuses, setPaymentStatuses] = useState<
    BudgetPaymentStatusResponse[]
  >([]);
  const [allAccounts, setAllAccounts] = useState<AccountResponse[]>([]);
  const [addAccountOpen, setAddAccountOpen] = useState(false);
  const [addPaymentOpen, setAddPaymentOpen] = useState(false);
  const [addIncomeOpen, setAddIncomeOpen] = useState(false);
  const [addPaymentAccountId, setAddPaymentAccountId] = useState<
    number | undefined
  >(undefined);
  const [accountMutating, setAccountMutating] = useState(false);
  const [paymentMutating, setPaymentMutating] = useState(false);
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
  const [payPeriodFilter, setPayPeriodFilter] = useState<PayPeriodFilter | null>(
    null,
  );
  const payPeriodFilterRef = useRef<PayPeriodFilter | null>(null);
  const dateEditSnapshotRef = useRef<Map<string, string | null>>(new Map());
  const [dirtyRevision, setDirtyRevision] = useState(0);
  const [gridMode, setGridMode] = useState<BudgetGridMode | null>(null);
  const [status, setStatus] = useState<{
    type: "success" | "error";
    message: string;
  } | null>(null);

  const applyMode = useCallback(
    (mode: BudgetGridMode, api?: GridApi | null) => {
      const targetApi = api ?? gridApi ?? gridRef.current?.api;
      if (!targetApi) return;
      applyBudgetGridMode(targetApi, mode);
      autoSizeVisibleBudgetColumns(targetApi);
      saveBudgetGridMode(mode);
      setGridMode(mode);
      saveBudgetColumnState(targetApi);
    },
    [gridApi],
  );

  const loadData = useCallback(async () => {
    setLoading(true);
    setStatus(null);
    try {
      const [
        budgetResult,
        paymentsResult,
        accountsResult,
        statusesResult,
        paymentSourcesResult,
        paySchedulesResult,
        incomeSourcesResult,
        incomeSummaryResult,
      ] = await Promise.all([
        budgetsApi.getById(budgetId),
        paymentsApi.getAll(),
        accountsApi.getAll(),
        paymentsApi.getStatuses(),
        paymentSourcesApi.getAll().catch(() => null),
        paySchedulesApi.getAll().catch(() => null),
        incomeSourcesApi.getAll().catch(() => null),
        incomesApi.getSummaryByBudget(budgetId).catch(() => null),
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
      setAllAccounts(accountsResult.data ?? []);
      const payments = (paymentsResult.data ?? []).filter(
        (payment) => payment.budgetId === budgetId,
      );

      const statuses = statusesResult.data ?? [];
      setPaymentStatuses(statuses);
      setStatusNames(statuses.map((s) => s.name));
      setStatusByName(
        new Map(statuses.map((s) => [s.name, s.budgetPaymentStatusId])),
      );

      const sources = paymentSourcesResult?.data ?? [];
      setPaymentSourceNames(sources.map((source) => source.name));
      setPaymentSourceByName(
        new Map(sources.map((source) => [source.name, source.paymentSourceId])),
      );

      const incomeSources = (incomeSourcesResult?.data ?? []).filter(
        (source) => source.isActive,
      );
      setIncomeSourceNames(incomeSources.map((source) => source.name));
      setIncomeSourceByName(
        new Map(
          incomeSources.map((source) => [source.name, source.incomeSourceId]),
        ),
      );
      setIncomeSummary(incomeSummaryResult?.data ?? null);

      setBudgetName(budget.name);
      setBudgetStartPeriod(budget.startPeriod);
      setBudgetEndPeriod(budget.endPeriod);
      setBudgetTemplateId(budget.budgetTemplateId);
      setBudgetAccountIds(budget.accountIds ?? []);
      setBudgetNotes(parseBudgetNotes(budget.notes));
      setBudgetPeriod(
        formatBudgetPeriod(budget.startPeriod, budget.endPeriod),
      );

      const schedules = paySchedulesResult?.data ?? [];
      setPaySchedules(schedules);
      const resolvedSchedule =
        schedules.find(
          (item) => item.payScheduleId === budget.payScheduleId,
        ) ??
        schedules.find((item) => item.isDefault) ??
        schedules[0] ??
        null;
      setPaySchedule(resolvedSchedule);

      setRowData(buildBudgetGridRows(payments, accountsById));
      dirtyIds.current.clear();
      dirtyAccountIds.current.clear();
      setPendingCount(0);
      setPayPeriodFilter(null);
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

  useEffect(() => {
    if (loading || !gridApi) return;
    autoSizeVisibleBudgetColumns(gridApi);
  }, [loading, rowData.length, gridApi, gridMode]);

  const { totalBudgeted, totalPaid, clearedCount, paymentPeriodSummaries } =
    useMemo(() => {
      const periods =
        paySchedule && budgetStartPeriod && budgetEndPeriod
          ? summarizePaymentsByPayPeriod(
              rowData,
              toScheduleConfig(paySchedule),
              budgetStartPeriod,
              budgetEndPeriod,
            )
          : null;

      return {
        totalBudgeted: rowData.reduce((sum, row) => sum + row.amount, 0),
        totalPaid: rowData.reduce((sum, row) => sum + row.paymentMade, 0),
        clearedCount: rowData.filter((row) => row.isCleared).length,
        paymentPeriodSummaries: periods,
      };
    }, [rowData, summaryTick, budgetStartPeriod, budgetEndPeriod, paySchedule]);

  const payPeriodPeriodsRef = useRef<
    ReadonlyArray<{ periodStart: string; periodEnd: string }>
  >([]);
  const budgetStartPeriodRef = useRef(budgetStartPeriod);

  useEffect(() => {
    payPeriodFilterRef.current = payPeriodFilter;
    budgetStartPeriodRef.current = budgetStartPeriod;
  }, [payPeriodFilter, budgetStartPeriod]);

  useEffect(() => {
    payPeriodPeriodsRef.current = paymentPeriodSummaries?.periods ?? [];
  }, [paymentPeriodSummaries]);

  useEffect(() => {
    gridApi?.onFilterChanged();
  }, [payPeriodFilter, gridApi]);

  const payPeriodFilterLabel = useMemo(() => {
    if (!payPeriodFilter || !paymentPeriodSummaries) return null;
    if (payPeriodFilter.type === "no-date") return "No Payment";
    return paymentPeriodSummaries.periods[payPeriodFilter.index]?.label ?? null;
  }, [payPeriodFilter, paymentPeriodSummaries]);

  const handlePayPeriodFilterSelect = useCallback((filter: PayPeriodFilter) => {
    setPayPeriodFilter((current) => {
      if (
        current?.type === filter.type &&
        (filter.type === "no-date" ||
          (current.type === "period" &&
            filter.type === "period" &&
            current.index === filter.index))
      ) {
        return null;
      }
      return filter;
    });
    setFilterRevision((revision) => revision + 1);
  }, []);

  const isExternalFilterPresent = useCallback(
    () => payPeriodFilterRef.current != null,
    [],
  );

  const doesExternalFilterPass = useCallback((node: IRowNode<BudgetGridRow>) => {
    if (!node.data || isPinnedTotalRow(node)) return true;
    return rowMatchesPayPeriodFilter(
      node.data,
      payPeriodFilterRef.current,
      payPeriodPeriodsRef.current,
      budgetStartPeriodRef.current,
    );
  }, []);

  const includedAccountIds = useMemo(
    () => new Set(budgetAccountIds),
    [budgetAccountIds],
  );

  const budgetAccounts = useMemo(
    () =>
      budgetAccountIds
        .map((accountId) => accountsByIdRef.current.get(accountId))
        .filter((account): account is AccountResponse => account != null)
        .sort((a, b) => a.name.localeCompare(b.name)),
    [budgetAccountIds, rowData],
  );

  const ensureAccountMutationAllowed = useCallback(() => {
    if (pendingCount > 0) {
      setStatus({
        type: "error",
        message:
          "Save or discard unsaved changes before adding or removing accounts.",
      });
      return false;
    }
    return true;
  }, [pendingCount]);

  const buildBudgetUpdateRequest = useCallback(
    (notes: string[]): UpdateBudgetRequest => ({
      name: budgetName,
      startPeriod: budgetStartPeriod,
      endPeriod: budgetEndPeriod,
      budgetTemplateId,
      notes: serializeBudgetNotes(notes),
      accountIds: budgetAccountIds,
      payScheduleId: paySchedule?.payScheduleId ?? null,
    }),
    [
      budgetAccountIds,
      budgetEndPeriod,
      budgetName,
      budgetStartPeriod,
      budgetTemplateId,
      paySchedule,
    ],
  );

  const handleBudgetPayScheduleChange = useCallback(
    async (payScheduleId: number) => {
      if (!ensureAccountMutationAllowed()) return;

      const selected = paySchedules.find(
        (item) => item.payScheduleId === payScheduleId,
      );
      if (!selected) return;

      setScheduleMutating(true);
      setStatus(null);
      try {
        await budgetsApi.update(budgetId, {
          ...buildBudgetUpdateRequest(budgetNotes),
          payScheduleId,
        });
        setPaySchedule(selected);
        setStatus({
          type: "success",
          message: `Using pay schedule "${selected.name}".`,
        });
      } catch (err) {
        const message =
          err instanceof ApiError
            ? err.errors.join(", ") || err.message
            : err instanceof Error
              ? err.message
              : "Failed to update pay schedule";
        setStatus({ type: "error", message });
      } finally {
        setScheduleMutating(false);
      }
    },
    [
      budgetId,
      budgetNotes,
      buildBudgetUpdateRequest,
      ensureAccountMutationAllowed,
      paySchedules,
    ],
  );

  const handleRemoveAccount = useCallback(
    async (accountId: number, accountName: string) => {
      if (!ensureAccountMutationAllowed()) return;
      if (budgetAccountIds.length <= 1) return;
      if (
        !window.confirm(`Remove "${accountName}" from this budget?`)
      ) {
        return;
      }

      setAccountMutating(true);
      setStatus(null);
      try {
        await budgetsApi.removeAccount(budgetId, accountId);
        setStatus({
          type: "success",
          message: `Removed ${accountName} from the budget.`,
        });
        await loadData();
      } catch (err) {
        const message =
          err instanceof ApiError
            ? err.errors.join(", ") || err.message
            : err instanceof Error
              ? err.message
              : "Failed to remove account";
        setStatus({ type: "error", message });
      } finally {
        setAccountMutating(false);
      }
    },
    [budgetId, budgetAccountIds.length, ensureAccountMutationAllowed, loadData],
  );

  const handleDeletePayment = useCallback(
    async (budgetPaymentId: number) => {
      if (!ensureAccountMutationAllowed()) return;

      const payment = rowData.find(
        (row) => row.budgetPaymentId === budgetPaymentId,
      );
      if (!payment) return;

      const accountPaymentCount = rowData.filter(
        (row) => row.accountId === payment.accountId,
      ).length;
      if (accountPaymentCount <= 1 && budgetAccountIds.length <= 1) {
        setStatus({
          type: "error",
          message:
            "Cannot delete the only payment when this is the only account in the budget.",
        });
        return;
      }

      const confirmMessage =
        accountPaymentCount <= 1
          ? `Delete the only payment for "${payment.accountName}"? The account will remain in this budget with no payments until you add one.`
          : `Delete this payment for "${payment.accountName}" on ${formatDateForGrid(payment.paymentDate)}?`;

      if (!window.confirm(confirmMessage)) return;

      setPaymentMutating(true);
      setStatus(null);
      try {
        await paymentsApi.remove(budgetPaymentId);
        setStatus({
          type: "success",
          message: `Deleted payment for ${payment.accountName}.`,
        });
        await loadData();
      } catch (err) {
        const message =
          err instanceof ApiError
            ? err.errors.join(", ") || err.message
            : err instanceof Error
              ? err.message
              : "Failed to delete payment";
        setStatus({ type: "error", message });
      } finally {
        setPaymentMutating(false);
      }
    },
    [
      budgetAccountIds.length,
      ensureAccountMutationAllowed,
      loadData,
      rowData,
    ],
  );

  const handleAddPaymentForAccount = useCallback(
    (accountId: number) => {
      if (!ensureAccountMutationAllowed()) return;
      setAddPaymentAccountId(accountId);
      setAddPaymentOpen(true);
    },
    [ensureAccountMutationAllowed],
  );

  const gridContext = useMemo<BudgetGridContext>(
    () => ({
      rowData,
      budgetAccountCount: budgetAccountIds.length,
      accountMutating,
      paymentMutating,
      onRemoveAccount: handleRemoveAccount,
      onDeletePayment: handleDeletePayment,
      onAddPaymentForAccount: handleAddPaymentForAccount,
    }),
    [
      accountMutating,
      budgetAccountIds.length,
      handleAddPaymentForAccount,
      handleDeletePayment,
      handleRemoveAccount,
      paymentMutating,
      rowData,
    ],
  );

  const paymentSourceNameById = useMemo(() => {
    const byId = new Map<number, string>();
    for (const [name, id] of paymentSourceByName) {
      byId.set(id, name);
    }
    return byId;
  }, [paymentSourceByName]);

  const paymentSourceOptions = useMemo(
    () => [PAYMENT_SOURCE_NONE, ...paymentSourceNames],
    [paymentSourceNames],
  );

  const incomeSourceNameById = useMemo(() => {
    const byId = new Map<number, string>();
    for (const [name, id] of incomeSourceByName) {
      byId.set(id, name);
    }
    return byId;
  }, [incomeSourceByName]);

  const incomeSourceOptions = useMemo(
    () => [INCOME_SOURCE_NONE, ...incomeSourceNames],
    [incomeSourceNames],
  );

  const incomeTotal = incomeSummary?.total ?? 0;

  const incomeAllocated = useMemo(() => {
    return rowData.reduce((sum, row) => {
      if (!row.incomeSourceId) return sum;
      const status = row.budgetPaymentStatusName.trim().toLowerCase();
      if (status === "paid") return sum + row.paymentMade;
      if (
        status === "pending" ||
        status === "scheduled" ||
        status === "overdue" ||
        status === "failed"
      ) {
        return sum + row.amount;
      }
      return sum;
    }, 0);
  }, [rowData, summaryTick]);

  const incomeRemaining = Math.max(0, incomeTotal - incomeAllocated);

  const columnDefs = useMemo<ColDef<BudgetGridRow>[]>(
    () => [
      {
        colId: "accountName",
        field: "accountName",
        headerName: "Account",
        filter: "agTextColumnFilter",
        width: 160,
        minWidth: 120,
        maxWidth: 260,
        pinned: "left",
        cellClass: "ag-cell-name",
        editable: false,
        cellRenderer: AccountNameCellRenderer,
      },
      {
        colId: "accountCategoryName",
        headerName: "Category",
        filter: "agTextColumnFilter",
        width: 110,
        minWidth: 90,
        maxWidth: 160,
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
        comparator: (
          _valueA: string | null | undefined,
          _valueB: string | null | undefined,
          nodeA,
          nodeB,
        ) => {
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
        headerName: "Acct payment day",
        editable: editableUnlessPinned(),
        width: 90,
        minWidth: 70,
        maxWidth: 110,
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
        editable: editableUnlessPinned(),
        width: 95,
        minWidth: 80,
        maxWidth: 120,
        ...currencyCol,
      },
      {
        colId: "paymentMade",
        field: "paymentMade",
        headerName: "Paid",
        editable: editableUnlessPinned(),
        width: 85,
        minWidth: 70,
        maxWidth: 110,
        ...currencyCol,
      },
      {
        colId: "owed",
        headerName: "Owed",
        editable: false,
        width: 85,
        minWidth: 70,
        maxWidth: 110,
        filter: "agNumberColumnFilter",
        cellClass: "ag-cell-currency",
        headerClass: "ag-cell-currency-header",
        valueGetter: (params: ValueGetterParams<BudgetGridRow>) =>
          params.data
            ? calculateOwed(params.data.amount, params.data.paymentMade)
            : null,
        valueFormatter: currencyFormatter,
        filterValueGetter: (params: ValueGetterParams<BudgetGridRow>) =>
          params.data
            ? calculateOwed(params.data.amount, params.data.paymentMade)
            : null,
      },
      {
        colId: "budgetPaymentStatusName",
        headerName: "Status",
        editable: editableUnlessPinned(),
        filter: "agTextColumnFilter",
        width: 100,
        minWidth: 90,
        maxWidth: 120,
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
        editable: editableUnlessPinned(),
        cellEditor: "agCheckboxCellEditor",
        filter: true,
        width: 85,
        minWidth: 75,
        maxWidth: 100,
        cellClass: "ag-cell-center",
      },
      {
        colId: "paymentDate",
        headerName: "Payment day",
        editable: editableUnlessPinned(),
        width: 90,
        minWidth: 70,
        maxWidth: 110,
        filter: "agNumberColumnFilter",
        cellClass: "ag-cell-center",
        cellEditor: "agNumberCellEditor",
        cellEditorParams: (() => {
          const start = budgetStartPeriod ? new Date(budgetStartPeriod) : null;
          const lastDay =
            start && Number.isFinite(start.getTime())
              ? new Date(
                  Date.UTC(start.getUTCFullYear(), start.getUTCMonth() + 1, 0),
                ).getUTCDate()
              : 31;
          return {
            min: 1,
            max: lastDay,
            precision: 0,
            showStepperButtons: false,
          };
        })(),
        tooltipValueGetter: () =>
          "Enter the day of the month (numbers only, valid for this budget month)",
        valueGetter: (params: ValueGetterParams<BudgetGridRow>) =>
          getDayOfMonthFromIso(params.data?.paymentDate),
        valueFormatter: (params) => formatPaymentDay(params.value),
        valueParser: (params: ValueParserParams<BudgetGridRow>) =>
          parseDayOfMonthInput(params.newValue),
        valueSetter: (params: ValueSetterParams<BudgetGridRow>) => {
          if (!params.data || !budgetStartPeriod) return false;

          const day = parseDayOfMonthInput(params.newValue);
          if (day == null) return false;

          const next = paymentDateFromDayOfMonth(budgetStartPeriod, day);
          if (!next) return false;

          if (normalizeGridDateIso(params.data.paymentDate) === next) {
            return false;
          }

          params.data.paymentDate = next;
          return true;
        },
        filterValueGetter: (params: ValueGetterParams<BudgetGridRow>) =>
          getDayOfMonthFromIso(params.data?.paymentDate),
      },
      createEditableDateColDef<BudgetGridRow>("clearedDate", "Cleared date", {
        editable: editableUnlessPinned(),
        width: 110,
        minWidth: 100,
        maxWidth: 130,
      }),
      {
        colId: "accountNumber",
        field: "accountNumber",
        headerName: "Account number",
        filter: "agTextColumnFilter",
        width: 110,
        minWidth: 90,
        maxWidth: 140,
        editable: false,
      },
      {
        colId: "accountBalance",
        field: "accountBalance",
        headerName: "Balance",
        width: 95,
        minWidth: 80,
        maxWidth: 120,
        editable: false,
        ...currencyCol,
      },
      {
        colId: "accountMonthlyPayment",
        field: "accountMonthlyPayment",
        headerName: "Monthly (account)",
        width: 120,
        minWidth: 100,
        maxWidth: 150,
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
        headerName: "Payment source",
        editable: editableUnlessPinned(),
        filter: "agTextColumnFilter",
        width: 130,
        minWidth: 110,
        maxWidth: 180,
        cellClass: "ag-cell-category",
        valueGetter: (params: ValueGetterParams<BudgetGridRow>) => {
          if (!params.data?.paymentSourceId) return PAYMENT_SOURCE_NONE;
          return (
            paymentSourceNameById.get(params.data.paymentSourceId) ??
            String(params.data.paymentSourceId)
          );
        },
        valueSetter: (params: ValueSetterParams<BudgetGridRow>) => {
          if (!params.data) return false;

          if (paymentSourceNames.length === 0) {
            params.data.paymentSourceId = parseOptionalNumber(params.newValue);
            return true;
          }

          const selected = String(params.newValue);
          if (selected === PAYMENT_SOURCE_NONE) {
            params.data.paymentSourceId = null;
            return true;
          }

          const id = paymentSourceByName.get(selected);
          if (id === undefined) return false;
          params.data.paymentSourceId = id;
          return true;
        },
        ...(paymentSourceNames.length > 0
          ? {
              cellEditor: "agSelectCellEditor" as const,
              cellEditorParams: {
                values: paymentSourceOptions,
              },
            }
          : {
              valueParser: (p: ValueParserParams) =>
                parseOptionalNumber(p.newValue),
            }),
        filterValueGetter: (params: ValueGetterParams<BudgetGridRow>) => {
          if (!params.data?.paymentSourceId) return PAYMENT_SOURCE_NONE;
          return (
            paymentSourceNameById.get(params.data.paymentSourceId) ??
            String(params.data.paymentSourceId)
          );
        },
      },
      {
        colId: "incomeSourceId",
        headerName: "Income source",
        editable: editableUnlessPinned(),
        filter: "agTextColumnFilter",
        width: 120,
        minWidth: 100,
        maxWidth: 180,
        cellClass: "ag-cell-category",
        valueGetter: (params: ValueGetterParams<BudgetGridRow>) => {
          if (!params.data?.incomeSourceId) return INCOME_SOURCE_NONE;
          return (
            incomeSourceNameById.get(params.data.incomeSourceId) ??
            params.data.incomeSourceName ??
            String(params.data.incomeSourceId)
          );
        },
        valueSetter: (params: ValueSetterParams<BudgetGridRow>) => {
          if (!params.data) return false;

          const selected = String(params.newValue);
          if (selected === INCOME_SOURCE_NONE) {
            params.data.incomeSourceId = null;
            return true;
          }

          const id = incomeSourceByName.get(selected);
          if (id === undefined) return false;
          params.data.incomeSourceId = id;
          return true;
        },
        ...(incomeSourceNames.length > 0
          ? {
              cellEditor: "agSelectCellEditor" as const,
              cellEditorParams: {
                values: incomeSourceOptions,
              },
            }
          : {}),
        filterValueGetter: (params: ValueGetterParams<BudgetGridRow>) => {
          if (!params.data?.incomeSourceId) return INCOME_SOURCE_NONE;
          return (
            incomeSourceNameById.get(params.data.incomeSourceId) ??
            INCOME_SOURCE_NONE
          );
        },
      },
      {
        colId: "actions",
        headerName: "",
        pinned: "right",
        width: 200,
        minWidth: 180,
        maxWidth: 240,
        sortable: false,
        filter: false,
        floatingFilter: false,
        editable: false,
        suppressHeaderMenuButton: true,
        cellClass: "ag-cell-center",
        cellRenderer: (
          params: ICellRendererParams<BudgetGridRow, unknown, BudgetGridContext>,
        ) => {
          if (!params.data) return null;
          const rows = params.context?.rowData ?? [];
          const canRemoveAccount =
            (params.context?.budgetAccountCount ?? 0) > 1;
          const showRemoveAccount = isFirstPaymentRowForAccount(
            params.data,
            rows,
          );
          const mutating =
            (params.context?.accountMutating ?? false) ||
            (params.context?.paymentMutating ?? false);

          return (
            <div className="flex items-center justify-center gap-1">
              <button
                type="button"
                disabled={mutating}
                onClick={() =>
                  params.context?.onAddPaymentForAccount(params.data!.accountId)
                }
                className="rounded-full border border-[var(--border)] px-2 py-1 text-xs font-medium hover:bg-black/[0.03] disabled:cursor-not-allowed disabled:opacity-40"
                title="Add another payment for this account"
              >
                Add
              </button>
              <button
                type="button"
                disabled={mutating}
                onClick={() =>
                  void params.context?.onDeletePayment(
                    params.data!.budgetPaymentId,
                  )
                }
                className="rounded-full border border-red-200 px-2 py-1 text-xs font-medium text-red-700 hover:bg-red-50 disabled:cursor-not-allowed disabled:opacity-40"
                title="Delete Payment"
              >
                Delete
              </button>
              {showRemoveAccount ? (
                <button
                  type="button"
                  disabled={!canRemoveAccount || mutating}
                  onClick={() =>
                    void params.context?.onRemoveAccount(
                      params.data!.accountId,
                      params.data!.accountName,
                    )
                  }
                  className="rounded-full border border-red-200 px-2 py-1 text-xs font-medium text-red-700 hover:bg-red-50 disabled:cursor-not-allowed disabled:opacity-40"
                  title="Remove Account"
                >
                  Remove
                </button>
              ) : null}
            </div>
          );
        },
      },
    ].map(withHeaderTooltip),
    [
      budgetStartPeriod,
      statusNames,
      statusByName,
      paymentSourceNames,
      paymentSourceByName,
      paymentSourceNameById,
      paymentSourceOptions,
      incomeSourceByName,
      incomeSourceNameById,
      incomeSourceOptions,
      incomeSourceNames,
    ],
  );

  const defaultColDef = useMemo<ColDef>(
    () => ({
      sortable: true,
      filter: true,
      floatingFilter: true,
      resizable: true,
      minWidth: 70,
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
      recalculatePinnedBottomRowData(targetApi, BUDGET_PINNED_TOTALS),
    );
  }, []);

  const getRowClass = useCallback(
    (params: RowClassParams<BudgetGridRow>) => {
      if (isPinnedTotalRow(params.node)) {
        return "account-grid-total-row";
      }
      if (!params.data) return "";

      if (dirtyIds.current.has(params.data.budgetPaymentId)) {
        return "account-row-dirty";
      }

      if (dirtyAccountIds.current.has(params.data.accountId)) {
        return "account-row-dirty";
      }

      return getBudgetPaymentRowClass(params.data);
    },
    [pendingCount, dirtyRevision],
  );

  const applyCellEdit = useCallback(
    (data: BudgetGridRow, colId: string, api: GridApi) => {
      if (colId === "accountPaymentDay") {
        dirtyAccountIds.current.add(data.accountId);
      } else {
        dirtyIds.current.add(data.budgetPaymentId);
      }

      setRowData((prev) =>
        prev.map((row) =>
          row.budgetPaymentId === data.budgetPaymentId ? { ...data } : row,
        ),
      );

      setPendingCount(
        dirtyIds.current.size + dirtyAccountIds.current.size,
      );
      setDirtyRevision((revision) => revision + 1);
      setSummaryTick((tick) => tick + 1);

      const node = api.getRowNode(String(data.budgetPaymentId));
      if (node) {
        if (
          colId === "amount" ||
          colId === "paymentMade" ||
          colId === "paymentSourceId" ||
          colId === "incomeSourceId" ||
          colId === "budgetPaymentStatusName"
        ) {
          api.refreshCells({
            rowNodes: [node],
            columns: ["owed", "accountName"],
          });
        }
        api.redrawRows({ rowNodes: [node] });
      }

      refreshPinnedTotals(api);
    },
    [refreshPinnedTotals],
  );

  const dateEditKey = (budgetPaymentId: number, colId: string) =>
    `${budgetPaymentId}:${colId}`;

  const onCellEditingStarted = useCallback(
    (event: CellEditingStartedEvent<BudgetGridRow>) => {
      if (!event.data) return;

      const colId = event.column.getColId();
      if (colId !== "clearedDate") return;

      dateEditSnapshotRef.current.set(
        dateEditKey(event.data.budgetPaymentId, colId),
        normalizeGridDateIso(event.data.clearedDate),
      );
    },
    [],
  );

  const onCellEditingStopped = useCallback(
    (event: CellEditingStoppedEvent<BudgetGridRow>) => {
      if (!event.data) return;

      const colId = event.column.getColId();
      if (colId !== "clearedDate") return;

      const key = dateEditKey(event.data.budgetPaymentId, colId);
      const before =
        dateEditSnapshotRef.current.get(key) ??
        normalizeGridDateIso(event.oldValue);
      dateEditSnapshotRef.current.delete(key);

      const parsedFromEditor = parseAndNormalizeGridDate(event.value);
      if (parsedFromEditor != null) {
        event.data.clearedDate = parsedFromEditor;
      }

      const after = normalizeGridDateIso(event.data.clearedDate);
      if (before === after) return;

      applyCellEdit(event.data, colId, event.api);
    },
    [applyCellEdit],
  );

  const onCellValueChanged = useCallback(
    (event: CellValueChangedEvent<BudgetGridRow>) => {
      if (!event.data) return;

      const changedColId = event.column.getColId();

      if (changedColId === "paymentDate") {
        const before = parseDayOfMonthInput(event.oldValue);
        const after = getDayOfMonthFromIso(event.data.paymentDate);
        if (before === after) return;
      } else if (changedColId === "clearedDate") {
        if (
          normalizeGridDateIso(event.oldValue) ===
          normalizeGridDateIso(event.newValue)
        ) {
          return;
        }
      } else if (event.oldValue === event.newValue) {
        return;
      }

      applyCellEdit(event.data, changedColId, event.api);
    },
    [applyCellEdit],
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
          paymentsApi.update(
            row.budgetPaymentId,
            toUpdatePaymentRequest(row, budgetStartPeriod),
          ),
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

  useEffect(() => {
    refreshPinnedTotals();
  }, [rowData, summaryTick, refreshPinnedTotals]);

  const onGridReady = (event: GridReadyEvent) => {
    setGridApi(event.api);

    // No mode on load — show the default column set.
    saveBudgetGridMode(null);
    setGridMode(null);
    applyDefaultBudgetColumnVisibility(event.api);
    autoSizeVisibleBudgetColumns(event.api);

    columnStateReadyRef.current = true;
    refreshPinnedTotals(event.api);
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
                {paySchedule ? ` · ${paySchedule.name}` : ""}
              </p>
            ) : null}
          </div>

          {!loading ? (
            <div className="flex flex-wrap items-end gap-2">
              <button
                type="button"
                onClick={() => setAddIncomeOpen(true)}
                disabled={pendingCount > 0}
                className="inline-flex items-center gap-2 rounded-full border border-white/25 bg-white/10 px-3 py-2 text-xs font-medium text-white backdrop-blur-sm disabled:opacity-40"
              >
                <Plus size={14} aria-hidden />
                Add income
              </button>
            </div>
          ) : null}

          {!loading && paySchedules.length > 0 ? (
            <div className="flex flex-wrap items-end gap-2">
              <label className="text-xs text-white/80">
                <span className="mb-1 block font-medium">Pay schedule</span>
                <select
                  value={paySchedule?.payScheduleId ?? ""}
                  onChange={(event) =>
                    void handleBudgetPayScheduleChange(Number(event.target.value))
                  }
                  disabled={scheduleMutating || pendingCount > 0}
                  className="rounded-xl border border-white/20 bg-white/10 px-3 py-2 text-sm text-white backdrop-blur-sm"
                >
                  {paySchedules.map((item) => (
                    <option
                      key={item.payScheduleId}
                      value={item.payScheduleId}
                      className="text-[var(--foreground)]"
                    >
                      {item.name}
                    </option>
                  ))}
                </select>
              </label>
              {paySchedule ? (
                <button
                  type="button"
                  onClick={() => setPayScheduleSettingsOpen(true)}
                  disabled={scheduleMutating || pendingCount > 0}
                  className="inline-flex items-center gap-2 rounded-full border border-white/25 bg-white/10 px-3 py-2 text-xs font-medium text-white backdrop-blur-sm disabled:opacity-40"
                >
                  <Settings2 size={14} aria-hidden />
                  Edit schedule
                </button>
              ) : null}
            </div>
          ) : null}
        </div>

        {!loading ? (
          <div className="mt-4 grid gap-3 sm:grid-cols-3">
            <div className="rounded-xl border border-white/15 bg-white/10 p-3 backdrop-blur-sm">
              <p className="text-xs text-white/70">Income</p>
              <p className="mt-1 text-lg font-semibold tabular-nums text-white">
                {formatCurrencyDetailed(incomeTotal)}
              </p>
            </div>
            <div className="rounded-xl border border-white/15 bg-white/10 p-3 backdrop-blur-sm">
              <p className="text-xs text-white/70">Allocated</p>
              <p className="mt-1 text-lg font-semibold tabular-nums text-white">
                {formatCurrencyDetailed(incomeAllocated)}
              </p>
            </div>
            <div className="rounded-xl border border-emerald-300/30 bg-emerald-500/20 p-3 backdrop-blur-sm">
              <p className="text-xs text-emerald-100/85">Remaining</p>
              <p className="mt-1 text-lg font-semibold tabular-nums text-white">
                {formatCurrencyDetailed(incomeRemaining)}
              </p>
            </div>
          </div>
        ) : null}

        {!loading ? (
          <BudgetNotesSection
            budgetId={budgetId}
            notes={budgetNotes}
            onNotesChange={setBudgetNotes}
            buildUpdateRequest={buildBudgetUpdateRequest}
            disabled={scheduleMutating || pendingCount > 0}
            onStatus={setStatus}
          />
        ) : null}

        {!loading && paymentPeriodSummaries ? (
          <div
            className={`mt-5 grid gap-4 sm:grid-cols-2 ${
              paymentPeriodSummaries.periods.length + 1 >= 4
                ? "xl:grid-cols-4"
                : "lg:grid-cols-3"
            }`}
          >
            {paymentPeriodSummaries.periods.map((period, index) => (
              <PaymentHalfPanel
                key={`${period.periodStart}-${period.periodEnd}`}
                title={period.label}
                summary={period.summary}
                cleared={period.cleared}
                isActive={
                  payPeriodFilter?.type === "period" &&
                  payPeriodFilter.index === index
                }
                onSelect={() =>
                  handlePayPeriodFilterSelect({ type: "period", index })
                }
              />
            ))}
            <PaymentHalfPanel
              title="No Payment"
              summary={paymentPeriodSummaries.noDate}
              cleared={paymentPeriodSummaries.noDateCleared}
              isActive={payPeriodFilter?.type === "no-date"}
              onSelect={() => handlePayPeriodFilterSelect({ type: "no-date" })}
            />
          </div>
        ) : loading ? (
          <div className="mt-5 grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
            <div className="h-36 animate-pulse rounded-xl bg-white/10" />
            <div className="h-36 animate-pulse rounded-xl bg-white/10" />
            <div className="h-36 animate-pulse rounded-xl bg-white/10" />
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

          <div
            className="inline-flex rounded-full border border-[var(--border)] bg-white p-1"
            role="group"
            aria-label="Budget grid mode"
          >
            {(["budget", "payment"] as const).map((mode) => {
              const active = gridMode === mode;
              return (
                <button
                  key={mode}
                  type="button"
                  onClick={() => applyMode(mode)}
                  aria-pressed={active}
                  className={`rounded-full px-3 py-1.5 text-xs font-semibold transition ${
                    active
                      ? "bg-[var(--link)] text-white"
                      : "text-[var(--muted)] hover:text-[var(--foreground)]"
                  }`}
                >
                  {BUDGET_GRID_MODE_LABELS[mode]}
                </button>
              );
            })}
          </div>

          <div className="account-grid-toolbar-actions">
            <button
              type="button"
              onClick={() => {
                if (!ensureAccountMutationAllowed()) return;
                setAddPaymentAccountId(undefined);
                setAddPaymentOpen(true);
              }}
              disabled={
                loading || saving || accountMutating || paymentMutating
              }
              className="inline-flex items-center gap-2 rounded-full border border-[var(--border)] bg-white px-4 py-2 text-sm font-medium disabled:cursor-not-allowed disabled:opacity-40"
            >
              <Plus size={15} aria-hidden />
              Add payment
            </button>
            <button
              type="button"
              onClick={() => {
                if (!ensureAccountMutationAllowed()) return;
                setAddAccountOpen(true);
              }}
              disabled={loading || saving || accountMutating || paymentMutating}
              className="inline-flex items-center gap-2 rounded-full border border-[var(--border)] bg-white px-4 py-2 text-sm font-medium disabled:cursor-not-allowed disabled:opacity-40"
            >
              <Plus size={15} aria-hidden />
              Add account
            </button>
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

        {payPeriodFilterLabel ? (
          <div className="account-grid-active-filters">
            <div className="account-grid-active-filters-list">
              <span className="account-grid-filter-chip">
                <span className="account-grid-filter-chip-label">
                  Pay period: <strong>{payPeriodFilterLabel}</strong>
                </span>
                <button
                  type="button"
                  onClick={() => {
                    setPayPeriodFilter(null);
                    setFilterRevision((revision) => revision + 1);
                  }}
                  className="account-grid-filter-chip-clear"
                  aria-label="Clear pay period filter"
                >
                  <X size={14} aria-hidden />
                </button>
              </span>
            </div>
          </div>
        ) : null}

        <div className="account-grid-viewport">
          <AgGridReact<BudgetGridRow>
            ref={gridRef}
            theme={accountGridTheme}
            rowData={rowData}
            getRowId={(params) => String(params.data.budgetPaymentId)}
            pinnedBottomRowData={pinnedBottomRowData}
            columnDefs={columnDefs}
            defaultColDef={defaultColDef}
            context={gridContext}
            getRowClass={getRowClass}
            isExternalFilterPresent={isExternalFilterPresent}
            doesExternalFilterPass={doesExternalFilterPass}
            onCellEditingStarted={onCellEditingStarted}
            onCellEditingStopped={onCellEditingStopped}
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
            tooltipShowDelay={400}
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
        Click a pay-period summary to filter the grid · Double-click to edit ·
        Column layout is saved in this browser · Save when done
      </p>

      {addPaymentOpen ? (
        <AddBudgetPaymentDialog
          budgetId={budgetId}
          budgetStartPeriod={budgetStartPeriod}
          accounts={budgetAccounts}
          statuses={paymentStatuses}
          initialAccountId={addPaymentAccountId}
          onClose={() => {
            setAddPaymentOpen(false);
            setAddPaymentAccountId(undefined);
          }}
          onAdded={(accountName) => {
            setStatus({
              type: "success",
              message: `Added payment for ${accountName}.`,
            });
            void loadData();
          }}
        />
      ) : null}

      {addIncomeOpen ? (
        <AddIncomeDialog
          budgets={[
            {
              budgetId,
              name: budgetName,
              startPeriod: budgetStartPeriod,
              endPeriod: budgetEndPeriod,
              budgetTemplateId,
              accountIds: budgetAccountIds,
              payScheduleId: paySchedule?.payScheduleId ?? null,
            },
          ]}
          sources={incomeSourceNames.map((name) => ({
            incomeSourceId: incomeSourceByName.get(name)!,
            name,
            isActive: true,
          }))}
          initialBudgetId={budgetId}
          onClose={() => setAddIncomeOpen(false)}
          onAdded={() => {
            setStatus({ type: "success", message: "Income added." });
            void loadData();
          }}
        />
      ) : null}

      {payScheduleSettingsOpen && paySchedule ? (
        <PayScheduleSettingsDialog
          schedule={paySchedule}
          onClose={() => setPayScheduleSettingsOpen(false)}
          onSaved={(updated) => {
            setPaySchedule(updated);
            setPaySchedules((current) =>
              current.map((item) =>
                item.payScheduleId === updated.payScheduleId ? updated : item,
              ),
            );
            setStatus({
              type: "success",
              message: `Updated pay schedule "${updated.name}".`,
            });
          }}
        />
      ) : null}

      {addAccountOpen ? (
        <AddBudgetAccountDialog
          budgetId={budgetId}
          accounts={allAccounts}
          includedAccountIds={includedAccountIds}
          onClose={() => setAddAccountOpen(false)}
          onAdded={(accountName) => {
            setStatus({
              type: "success",
              message: `Added ${accountName} to the budget.`,
            });
            void loadData();
          }}
        />
      ) : null}
    </div>
  );
}
