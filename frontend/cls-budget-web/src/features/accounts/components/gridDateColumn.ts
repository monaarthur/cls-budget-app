import type {
  ColDef,
  ColDefField,
  ValueGetterParams,
  ValueParserParams,
  ValueSetterParams,
} from "ag-grid-community";
import { GridDateCellEditor } from "@/features/accounts/components/GridDateCellEditor";
import {
  formatDateForGrid,
  normalizeGridDateIso,
  parseAndNormalizeGridDate,
} from "@/features/accounts/utils/accountMapper";

function toFilterDate(iso: string | null | undefined): Date | null {
  const normalized = normalizeGridDateIso(iso);
  if (!normalized) return null;
  const date = new Date(normalized);
  return Number.isFinite(date.getTime()) ? date : null;
}

function commitParsedDate(
  rawValue: unknown,
  fallback: string | null | undefined,
): string | null {
  return parseAndNormalizeGridDate(rawValue, fallback ?? null);
}

function createEditableDateColDefBase<T extends object>(
  field: keyof T & string,
  headerName: string,
): ColDef<T> {
  return {
    colId: field,
    field: field as unknown as ColDefField<T>,
    headerName,
    filter: "agDateColumnFilter",
    filterValueGetter: (params: ValueGetterParams<T>) =>
      toFilterDate(params.data?.[field as keyof T] as string | null | undefined),
    cellClass: "ag-cell-date",
    tooltipValueGetter: () => "Enter MM/DD/YYYY",
    valueFormatter: (params) =>
      formatDateForGrid((params.data?.[field as keyof T] as string | null) ?? null),
    valueParser: (params: ValueParserParams<T, unknown, string | null>) =>
      commitParsedDate(params.newValue, params.oldValue as string | null),
    valueSetter: (params: ValueSetterParams<T>) => {
      if (!params.data) return false;

      const next = commitParsedDate(
        params.newValue,
        params.oldValue as string | null,
      );
      const current = normalizeGridDateIso(
        params.data[field as keyof T] as string | null | undefined,
      );

      if (current === next) return false;

      (params.data as Record<string, unknown>)[field] = next;
      return true;
    },
  };
}

/** Text-only date editing (MM/DD/YYYY). */
export function createEditableDateTextColDef<T extends object>(
  field: keyof T & string,
  headerName: string,
  overrides?: ColDef<T>,
): ColDef<T> {
  return {
    ...createEditableDateColDefBase(field, headerName),
    cellEditor: GridDateCellEditor,
    ...overrides,
  };
}

/** Date editing with calendar picker. */
export function createEditableDateColDef<T extends object>(
  field: keyof T & string,
  headerName: string,
  overrides?: ColDef<T>,
): ColDef<T> {
  return {
    ...createEditableDateColDefBase(field, headerName),
    cellEditor: GridDateCellEditor,
    tooltipValueGetter: () => "Enter MM/DD/YYYY or use the calendar",
    ...overrides,
  };
}
