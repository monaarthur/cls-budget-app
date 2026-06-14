"use client";

import { useMemo } from "react";
import { X } from "lucide-react";
import type { GridApi } from "ag-grid-community";
import {
  clearAllColumnFilters,
  clearColumnFilter,
  getActiveGridFilters,
} from "@/features/accounts/components/gridFilterUtils";

export function GridActiveFilters({
  gridApi,
  quickFilter,
  onQuickFilterChange,
  filterRevision,
  onFiltersCleared,
}: {
  gridApi: GridApi | null;
  quickFilter: string;
  onQuickFilterChange: (value: string) => void;
  filterRevision: number;
  onFiltersCleared?: () => void;
}) {
  const columnFilters = useMemo(() => {
    if (!gridApi) return [];
    void filterRevision;
    return getActiveGridFilters(gridApi);
  }, [gridApi, filterRevision]);

  const hasQuickFilter = quickFilter.trim().length > 0;
  const hasColumnFilters = columnFilters.length > 0;
  const hasActiveFilters = hasQuickFilter || hasColumnFilters;

  if (!hasActiveFilters) return null;

  function notifyCleared() {
    onFiltersCleared?.();
  }

  function handleClearColumn(colId: string) {
    if (!gridApi) return;
    clearColumnFilter(gridApi, colId);
    notifyCleared();
  }

  function handleClearQuickFilter() {
    onQuickFilterChange("");
    notifyCleared();
  }

  function handleClearAll() {
    if (gridApi) {
      clearAllColumnFilters(gridApi);
    }
    onQuickFilterChange("");
    notifyCleared();
  }

  return (
    <div className="account-grid-active-filters">
      <div className="account-grid-active-filters-list">
        {hasQuickFilter ? (
          <span className="account-grid-filter-chip">
            <span className="account-grid-filter-chip-label">
              Search: <strong>{quickFilter}</strong>
            </span>
            <button
              type="button"
              onClick={handleClearQuickFilter}
              className="account-grid-filter-chip-clear"
              aria-label="Clear search filter"
            >
              <X size={14} aria-hidden />
            </button>
          </span>
        ) : null}

        {columnFilters.map((filter) => (
          <span key={filter.colId} className="account-grid-filter-chip">
            <span className="account-grid-filter-chip-label">
              {filter.label}: <strong>{filter.description}</strong>
            </span>
            <button
              type="button"
              onClick={() => handleClearColumn(filter.colId)}
              className="account-grid-filter-chip-clear"
              aria-label={`Clear ${filter.label} filter`}
            >
              <X size={14} aria-hidden />
            </button>
          </span>
        ))}
      </div>

      {(hasQuickFilter && hasColumnFilters) || columnFilters.length > 1 ? (
        <button
          type="button"
          onClick={handleClearAll}
          className="account-grid-filter-clear-all"
        >
          Clear all
        </button>
      ) : null}
    </div>
  );
}
