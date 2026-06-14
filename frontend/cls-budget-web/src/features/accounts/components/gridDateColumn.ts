import type {
  ColDef,
  ColDefField,
  ValueGetterParams,
  ValueSetterParams,
} from "ag-grid-community";
import { GridDateCellEditor } from "@/features/accounts/components/GridDateCellEditor";
import {
  formatDateForGrid,
  parseGridDateValue,
} from "@/features/accounts/utils/accountMapper";

function toFilterDate(iso: string | null | undefined): Date | null {
  if (!iso) return null;
  const normalized = iso.includes("T") ? iso : `${iso}T00:00:00.000Z`;
  const date = new Date(normalized);
  return Number.isFinite(date.getTime()) ? date : null;
}

export function createEditableDateColDef<T extends object>(
  field: keyof T & string,
  headerName: string,
  overrides?: ColDef<T>,
): ColDef<T> {
  return {
    colId: field,
    field: field as unknown as ColDefField<T>,
    headerName,
    filter: "agDateColumnFilter",
    cellEditor: GridDateCellEditor,
    cellClass: "ag-cell-date",
    tooltipValueGetter: () => "Enter MM/DD/YYYY or use the calendar",
    valueFormatter: (params) =>
      formatDateForGrid((params.data?.[field as keyof T] as string | null) ?? null),
    valueGetter: (params: ValueGetterParams<T>) =>
      toFilterDate(params.data?.[field as keyof T] as string | null | undefined),
    valueSetter: (params: ValueSetterParams<T>) => {
      if (!params.data) return false;
      const parsed = parseGridDateValue(params.newValue);
      const current = params.data[field as keyof T] as string | null | undefined;
      if (parsed === current) return false;
      (params.data as Record<string, unknown>)[field] = parsed;
      return true;
    },
    valueParser: (params) => parseGridDateValue(params.newValue),
    ...overrides,
  };
}
