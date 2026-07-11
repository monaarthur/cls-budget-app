import type { ColumnState, GridApi } from "ag-grid-community";
import {
  ACCOUNT_EXCLUDED_COLUMNS,
  CREDIT_CARD_EXCLUDED_COLUMNS,
  DEFAULT_HIDDEN_COLUMNS,
  GRID_COLUMNS,
} from "@/features/accounts/components/gridColumns";

const ACCOUNTS_GRID_NAMESPACE = "accounts-grid";
const CREDIT_CARD_GRID_NAMESPACE = "credit-cards-grid";
const ACCOUNTS_STORAGE_VERSION = 2;
const CREDIT_CARD_STORAGE_VERSION = 5;

function storageKeys(namespace: string) {
  return {
    state: `cls-budget.${namespace}.column-state`,
    version: `cls-budget.${namespace}.column-state-version`,
  };
}

function storageVersion(namespace: string): number {
  return namespace === CREDIT_CARD_GRID_NAMESPACE
    ? CREDIT_CARD_STORAGE_VERSION
    : ACCOUNTS_STORAGE_VERSION;
}

function isCreditCardGrid(namespace: string): boolean {
  return namespace === CREDIT_CARD_GRID_NAMESPACE;
}

function validColIds(namespace: string): Set<string> {
  const ids = GRID_COLUMNS.map((column) => column.colId);
  if (isCreditCardGrid(namespace)) {
    return new Set(
      ids.filter((colId) => !CREDIT_CARD_EXCLUDED_COLUMNS.has(colId)),
    );
  }
  return new Set(ids.filter((colId) => !ACCOUNT_EXCLUDED_COLUMNS.has(colId)));
}

export function defaultHiddenColumns(namespace: string): Set<string> {
  return new Set(DEFAULT_HIDDEN_COLUMNS);
}

function existingColIds(api: GridApi): Set<string> {
  return new Set(api.getColumns()?.map((column) => column.getColId()) ?? []);
}

export function filterExistingColIds(
  api: GridApi,
  colIds: Iterable<string>,
): string[] {
  const existing = existingColIds(api);
  return [...colIds].filter((colId) => existing.has(colId));
}

function sanitizeColumnState(
  state: ColumnState[],
  namespace: string,
): ColumnState[] {
  const allowed = validColIds(namespace);
  return state.filter((column) => allowed.has(column.colId));
}

function isValidState(state: ColumnState[], namespace: string): boolean {
  if (state.length === 0) return false;
  const allowed = validColIds(namespace);
  return state.every((column) => allowed.has(column.colId));
}

export function loadColumnState(
  namespace = ACCOUNTS_GRID_NAMESPACE,
): ColumnState[] | null {
  if (typeof window === "undefined") return null;

  const { state: storageKey, version: versionKey } = storageKeys(namespace);

  try {
    const version = Number(localStorage.getItem(versionKey));
    if (version !== storageVersion(namespace)) return null;

    const raw = localStorage.getItem(storageKey);
    if (!raw) return null;

    const state = sanitizeColumnState(
      JSON.parse(raw) as ColumnState[],
      namespace,
    );
    return isValidState(state, namespace) ? state : null;
  } catch {
    return null;
  }
}

export function saveColumnState(
  api: GridApi,
  namespace = ACCOUNTS_GRID_NAMESPACE,
): void {
  if (typeof window === "undefined") return;

  const { state: storageKey, version: versionKey } = storageKeys(namespace);
  const state = sanitizeColumnState(api.getColumnState(), namespace);
  localStorage.setItem(storageKey, JSON.stringify(state));
  localStorage.setItem(versionKey, String(storageVersion(namespace)));
}

export function clearColumnState(namespace = ACCOUNTS_GRID_NAMESPACE): void {
  if (typeof window === "undefined") return;

  const { state: storageKey, version: versionKey } = storageKeys(namespace);
  localStorage.removeItem(storageKey);
  localStorage.removeItem(versionKey);
}

export function applyDefaultColumnVisibility(
  api: GridApi,
  namespace = ACCOUNTS_GRID_NAMESPACE,
): void {
  const visible = filterExistingColIds(api, validColIds(namespace));
  const hidden = filterExistingColIds(api, defaultHiddenColumns(namespace));

  if (visible.length > 0) {
    api.setColumnsVisible(visible, true);
  }
  if (hidden.length > 0) {
    api.setColumnsVisible(hidden, false);
  }
}

export function restoreColumnState(
  api: GridApi,
  namespace = ACCOUNTS_GRID_NAMESPACE,
): boolean {
  const saved = loadColumnState(namespace);
  if (!saved) return false;

  const existing = existingColIds(api);
  const state = saved.filter((column) => existing.has(column.colId));
  if (state.length === 0) return false;

  api.applyColumnState({ state, applyOrder: true });
  return true;
}

export function resetColumnState(
  api: GridApi,
  namespace = ACCOUNTS_GRID_NAMESPACE,
): void {
  clearColumnState(namespace);
  api.resetColumnState();
  applyDefaultColumnVisibility(api, namespace);
  api.autoSizeColumns(
    isCreditCardGrid(namespace)
      ? ["name", "number"]
      : ["name", "number", "accountCategoryName"],
    false,
  );
}
