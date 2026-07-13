import type { ColumnState, GridApi } from "ag-grid-community";
import {
  BUDGET_DEFAULT_HIDDEN_COLUMNS,
  BUDGET_GRID_COLUMNS,
  BUDGET_GRID_MODE_COLUMNS,
  type BudgetGridMode,
} from "@/features/budgets/components/budgetGridColumns";

const STORAGE_KEY = "cls-budget.budget-grid.column-state";
const STORAGE_VERSION_KEY = "cls-budget.budget-grid.column-state-version";
const MODE_STORAGE_KEY = "cls-budget.budget-grid.view-mode";
const STORAGE_VERSION = 4;

const validColIds = new Set<string>([
  ...BUDGET_GRID_COLUMNS.map((column) => column.colId),
  "actions",
]);

function isValidState(state: ColumnState[]): boolean {
  if (state.length === 0) return false;
  return state.every((column) => validColIds.has(column.colId));
}

export function loadBudgetGridMode(): BudgetGridMode | null {
  if (typeof window === "undefined") return null;
  const value = localStorage.getItem(MODE_STORAGE_KEY);
  if (value === "payment" || value === "budget") return value;
  return null;
}

export function saveBudgetGridMode(mode: BudgetGridMode | null): void {
  if (typeof window === "undefined") return;
  if (mode == null) {
    localStorage.removeItem(MODE_STORAGE_KEY);
    return;
  }
  localStorage.setItem(MODE_STORAGE_KEY, mode);
}

export function applyBudgetGridMode(api: GridApi, mode: BudgetGridMode): void {
  const visibleIds = BUDGET_GRID_MODE_COLUMNS[mode];
  const visibleSet = new Set(visibleIds);
  const allColIds =
    api.getColumns()?.map((column) => column.getColId()) ??
    [...validColIds];

  api.setColumnsVisible(
    allColIds.filter((colId) => !visibleSet.has(colId)),
    false,
  );
  api.setColumnsVisible([...visibleSet], true);
  api.applyColumnState({
    state: visibleIds.map((colId) => ({ colId, hide: false })),
    applyOrder: true,
  });
}

export function loadBudgetColumnState(): ColumnState[] | null {
  if (typeof window === "undefined") return null;

  try {
    const version = Number(localStorage.getItem(STORAGE_VERSION_KEY));
    if (version !== STORAGE_VERSION) return null;

    const raw = localStorage.getItem(STORAGE_KEY);
    if (!raw) return null;

    const state = JSON.parse(raw) as ColumnState[];
    return isValidState(state) ? state : null;
  } catch {
    return null;
  }
}

export function saveBudgetColumnState(api: GridApi): void {
  if (typeof window === "undefined") return;

  const state = api.getColumnState();
  localStorage.setItem(STORAGE_KEY, JSON.stringify(state));
  localStorage.setItem(STORAGE_VERSION_KEY, String(STORAGE_VERSION));
}

export function clearBudgetColumnState(): void {
  if (typeof window === "undefined") return;

  localStorage.removeItem(STORAGE_KEY);
  localStorage.removeItem(STORAGE_VERSION_KEY);
}

export function applyDefaultBudgetColumnVisibility(api: GridApi): void {
  api.setColumnsVisible(
    BUDGET_GRID_COLUMNS.map((column) => column.colId),
    true,
  );
  api.setColumnsVisible([...BUDGET_DEFAULT_HIDDEN_COLUMNS], false);
}

export function restoreBudgetColumnState(api: GridApi): boolean {
  const saved = loadBudgetColumnState();
  if (!saved) return false;

  api.applyColumnState({ state: saved, applyOrder: true });
  return true;
}

export function resetBudgetColumnState(api: GridApi): void {
  clearBudgetColumnState();
  api.resetColumnState();
  applyDefaultBudgetColumnVisibility(api);
  const ids =
    api
      .getColumns()
      ?.filter((column) => column.isVisible() && column.getColId() !== "actions")
      .map((column) => column.getColId()) ?? [];
  if (ids.length > 0) {
    api.autoSizeColumns(ids, false);
  }
}
