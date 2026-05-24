import type { ColumnState, GridApi } from "ag-grid-community";
import {
  DEFAULT_HIDDEN_COLUMNS,
  GRID_COLUMNS,
} from "@/features/accounts/components/gridColumns";

const STORAGE_KEY = "cls-budget.accounts-grid.column-state";
const STORAGE_VERSION_KEY = "cls-budget.accounts-grid.column-state-version";
const STORAGE_VERSION = 2;

const validColIds = new Set<string>(GRID_COLUMNS.map((column) => column.colId));

function isValidState(state: ColumnState[]): boolean {
  if (state.length === 0) return false;
  return state.every((column) => validColIds.has(column.colId));
}

export function loadColumnState(): ColumnState[] | null {
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

export function saveColumnState(api: GridApi): void {
  if (typeof window === "undefined") return;

  const state = api.getColumnState();
  localStorage.setItem(STORAGE_KEY, JSON.stringify(state));
  localStorage.setItem(STORAGE_VERSION_KEY, String(STORAGE_VERSION));
}

export function clearColumnState(): void {
  if (typeof window === "undefined") return;

  localStorage.removeItem(STORAGE_KEY);
  localStorage.removeItem(STORAGE_VERSION_KEY);
}

export function applyDefaultColumnVisibility(api: GridApi): void {
  api.setColumnsVisible(
    GRID_COLUMNS.map((column) => column.colId),
    true,
  );
  api.setColumnsVisible([...DEFAULT_HIDDEN_COLUMNS], false);
}

export function restoreColumnState(api: GridApi): boolean {
  const saved = loadColumnState();
  if (!saved) return false;

  api.applyColumnState({ state: saved, applyOrder: true });
  return true;
}

export function resetColumnState(api: GridApi): void {
  clearColumnState();
  api.resetColumnState();
  applyDefaultColumnVisibility(api);
  api.autoSizeColumns(["name", "number", "accountCategoryName"], false);
}
