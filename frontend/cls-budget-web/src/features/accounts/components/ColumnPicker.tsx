"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import type { GridApi } from "ag-grid-community";
import { Columns3 } from "lucide-react";
import { GRID_COLUMNS } from "@/features/accounts/components/gridColumns";
import { resetColumnState } from "@/features/accounts/components/gridColumnState";

export function ColumnPicker({ gridApi }: { gridApi: GridApi | null }) {
  const [open, setOpen] = useState(false);
  const [visible, setVisible] = useState<Record<string, boolean>>({});
  const panelRef = useRef<HTMLDivElement>(null);

  const syncVisibility = useCallback(() => {
    if (!gridApi) return;
    const next: Record<string, boolean> = {};
    for (const { colId } of GRID_COLUMNS) {
      const column = gridApi.getColumn(colId);
      next[colId] = column?.isVisible() ?? true;
    }
    setVisible(next);
  }, [gridApi]);

  useEffect(() => {
    if (!gridApi) return;
    syncVisibility();
    gridApi.addEventListener("columnVisible", syncVisibility);
    gridApi.addEventListener("columnMoved", syncVisibility);
    return () => {
      gridApi.removeEventListener("columnVisible", syncVisibility);
      gridApi.removeEventListener("columnMoved", syncVisibility);
    };
  }, [gridApi, syncVisibility]);

  useEffect(() => {
    if (!open) return;
    const handleClickOutside = (event: MouseEvent) => {
      if (
        panelRef.current &&
        !panelRef.current.contains(event.target as Node)
      ) {
        setOpen(false);
      }
    };
    document.addEventListener("mousedown", handleClickOutside);
    return () => document.removeEventListener("mousedown", handleClickOutside);
  }, [open]);

  const toggleColumn = (colId: string) => {
    if (!gridApi) return;
    const isVisible = visible[colId] ?? true;
    gridApi.setColumnsVisible([colId], !isVisible);
  };

  const showAll = () => {
    gridApi?.setColumnsVisible(
      GRID_COLUMNS.map((c) => c.colId),
      true,
    );
  };

  const hideOptional = () => {
    if (!gridApi) return;
    const optional = GRID_COLUMNS.filter(
      (c) => c.colId !== "name" && c.colId !== "number",
    );
    gridApi.setColumnsVisible(
      optional.map((c) => c.colId),
      false,
    );
    gridApi.setColumnsVisible(["name", "number"], true);
  };

  const handleResetLayout = () => {
    if (!gridApi) return;
    resetColumnState(gridApi);
    syncVisibility();
  };

  const visibleCount = Object.values(visible).filter(Boolean).length;

  return (
    <div className="account-grid-column-picker" ref={panelRef}>
      <button
        type="button"
        onClick={() => setOpen((value) => !value)}
        className="inline-flex items-center gap-2 rounded-full border border-[var(--border)] bg-white px-4 py-2 text-sm font-medium"
        aria-expanded={open}
        aria-haspopup="true"
      >
        <Columns3 size={15} aria-hidden />
        Columns
        <span className="account-grid-column-badge">{visibleCount}</span>
      </button>

      {open ? (
        <div className="account-grid-column-panel" role="menu">
          <div className="account-grid-column-panel-header">
            <p className="font-semibold text-[#0f2744]">Show columns</p>
            <div className="flex flex-wrap gap-2">
              <button type="button" onClick={showAll} className="text-xs text-[var(--link)]">
                Show all
              </button>
              <button
                type="button"
                onClick={hideOptional}
                className="text-xs text-[var(--link)]"
              >
                Compact
              </button>
              <button
                type="button"
                onClick={handleResetLayout}
                className="text-xs text-[var(--link)]"
              >
                Reset layout
              </button>
            </div>
          </div>
          <ul className="account-grid-column-list">
            {GRID_COLUMNS.map(({ colId, label }) => (
              <li key={colId}>
                <label className="account-grid-column-option">
                  <input
                    type="checkbox"
                    checked={visible[colId] ?? true}
                    onChange={() => toggleColumn(colId)}
                  />
                  <span>{label}</span>
                </label>
              </li>
            ))}
          </ul>
        </div>
      ) : null}
    </div>
  );
}
