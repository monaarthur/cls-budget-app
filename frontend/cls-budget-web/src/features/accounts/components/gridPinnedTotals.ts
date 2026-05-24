import type { EditableCallback, GridApi, IRowNode } from "ag-grid-community";

export const PINNED_TOTAL_ROW_FLAG = "__pinnedTotalRow";

export interface PinnedTotalsConfig {
  labelField: string;
  sumFields: readonly string[];
}

export function isPinnedTotalRow(node: IRowNode | null | undefined): boolean {
  return node?.rowPinned === "bottom";
}

export function editableUnlessPinned(
  editable: boolean | EditableCallback = true,
): EditableCallback {
  return (params) => {
    if (isPinnedTotalRow(params.node)) return false;
    return typeof editable === "function" ? editable(params) : editable;
  };
}

export function recalculatePinnedBottomRowData(
  api: GridApi,
  config: PinnedTotalsConfig,
): Record<string, unknown>[] {
  const totals = Object.fromEntries(
    config.sumFields.map((field) => [field, 0]),
  ) as Record<string, number>;

  let visibleCount = 0;

  api.forEachNodeAfterFilter((node) => {
    if (!node.data || isPinnedTotalRow(node)) return;
    visibleCount += 1;

    for (const field of config.sumFields) {
      const value = node.data[field];
      totals[field] +=
        typeof value === "number" && Number.isFinite(value) ? value : 0;
    }
  });

  const row: Record<string, unknown> = {
    [PINNED_TOTAL_ROW_FLAG]: true,
    [config.labelField]:
      visibleCount > 0 ? `Total (${visibleCount})` : "Total",
    ...totals,
  };

  return [row];
}
