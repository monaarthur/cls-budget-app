import type { GridApi } from "ag-grid-community";

export type ActiveGridFilter = {
  colId: string;
  label: string;
  description: string;
};

const FILTER_TYPE_LABELS: Record<string, string> = {
  contains: "contains",
  notContains: "does not contain",
  equals: "is",
  notEqual: "is not",
  startsWith: "starts with",
  endsWith: "ends with",
  greaterThan: ">",
  greaterThanOrEqual: "≥",
  lessThan: "<",
  lessThanOrEqual: "≤",
  inRange: "between",
  blank: "is blank",
  notBlank: "is not blank",
};

function formatFilterDate(value: string): string {
  const normalized = value.includes("T") ? value : `${value}T00:00:00.000Z`;
  const date = new Date(normalized);
  if (!Number.isFinite(date.getTime())) return value;
  return date.toLocaleDateString("en-US", {
    month: "numeric",
    day: "numeric",
    year: "numeric",
    timeZone: "UTC",
  });
}

function getColumnLabel(api: GridApi, colId: string): string {
  const column = api.getColumn(colId);
  const headerName = column?.getColDef().headerName;
  if (headerName) return headerName;
  return colId;
}

function formatFilterDescription(filterDef: Record<string, unknown>): string {
  const filterType = filterDef.filterType as string | undefined;

  if (filterType === "set" && Array.isArray(filterDef.values)) {
    return filterDef.values.map(String).join(", ");
  }

  const type = (filterDef.type as string | undefined) ?? "equals";
  const operator = FILTER_TYPE_LABELS[type] ?? type;

  if (type === "blank" || type === "notBlank") {
    return operator;
  }

  if (filterType === "date") {
    const from = filterDef.dateFrom as string | undefined;
    const to = filterDef.dateTo as string | undefined;
    if (type === "inRange" && from && to) {
      return `${operator} ${formatFilterDate(from)} – ${formatFilterDate(to)}`;
    }
    if (from) return `${operator} ${formatFilterDate(from)}`;
  }

  if (filterType === "number") {
    const filter = filterDef.filter;
    const filterTo = filterDef.filterTo;
    if (type === "inRange" && filter != null && filterTo != null) {
      return `${operator} ${filter} – ${filterTo}`;
    }
    if (filter != null) return `${operator} ${filter}`;
  }

  const filter = filterDef.filter;
  if (filter != null && filter !== "") {
    return `${operator} ${String(filter)}`;
  }

  return operator;
}

export function getActiveGridFilters(api: GridApi): ActiveGridFilter[] {
  const model = api.getFilterModel() as Record<
    string,
    Record<string, unknown>
  >;

  return Object.entries(model).map(([colId, filterDef]) => ({
    colId,
    label: getColumnLabel(api, colId),
    description: formatFilterDescription(filterDef ?? {}),
  }));
}

export function clearColumnFilter(api: GridApi, colId: string): void {
  const model = { ...api.getFilterModel() };
  delete model[colId];
  api.setFilterModel(model);
}

export function clearAllColumnFilters(api: GridApi): void {
  api.setFilterModel(null);
}
