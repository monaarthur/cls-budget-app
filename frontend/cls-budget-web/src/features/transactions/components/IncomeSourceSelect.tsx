"use client";

import { useState } from "react";
import type { IncomeSourceResponse } from "@/features/incomes/types/income";

const NONE_VALUE = "";
const ADD_NEW_VALUE = "__add_new__";

export function IncomeSourceSelect({
  sources,
  value,
  disabled = false,
  onChange,
  onCreateSource,
}: {
  sources: IncomeSourceResponse[];
  value: number | null;
  disabled?: boolean;
  onChange: (incomeSourceId: number | null) => void;
  onCreateSource: (name: string) => Promise<IncomeSourceResponse | null>;
}) {
  const [adding, setAdding] = useState(false);
  const [newName, setNewName] = useState("");
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleSelectChange = async (next: string) => {
    if (next === ADD_NEW_VALUE) {
      setAdding(true);
      setNewName("");
      setError(null);
      return;
    }

    setAdding(false);
    onChange(next === NONE_VALUE ? null : Number(next));
  };

  const handleCreate = async () => {
    const trimmed = newName.trim();
    if (!trimmed) {
      setError("Enter a name for the income source.");
      return;
    }

    setSaving(true);
    setError(null);
    try {
      const created = await onCreateSource(trimmed);
      if (created) {
        onChange(created.incomeSourceId);
        setAdding(false);
        setNewName("");
      }
    } catch {
      setError("Failed to create income source.");
    } finally {
      setSaving(false);
    }
  };

  if (adding) {
    return (
      <div className="flex min-w-[180px] flex-col gap-1">
        <div className="flex items-center gap-1">
          <input
            type="text"
            value={newName}
            onChange={(event) => setNewName(event.target.value)}
            placeholder="New income source"
            disabled={saving}
            className="w-full rounded-lg border border-[var(--border)] px-2 py-1.5 text-sm"
            onKeyDown={(event) => {
              if (event.key === "Enter") {
                event.preventDefault();
                void handleCreate();
              }
            }}
          />
          <button
            type="button"
            onClick={() => void handleCreate()}
            disabled={saving}
            className="shrink-0 rounded-lg bg-[var(--accent)] px-2 py-1.5 text-xs font-semibold text-white disabled:opacity-50"
          >
            {saving ? "…" : "Add"}
          </button>
          <button
            type="button"
            onClick={() => {
              setAdding(false);
              setError(null);
            }}
            disabled={saving}
            className="shrink-0 rounded-lg border border-[var(--border)] px-2 py-1.5 text-xs"
          >
            Cancel
          </button>
        </div>
        {error ? <span className="text-xs text-[var(--negative)]">{error}</span> : null}
      </div>
    );
  }

  return (
    <select
      value={value ?? NONE_VALUE}
      disabled={disabled}
      onChange={(event) => void handleSelectChange(event.target.value)}
      className="w-full min-w-[160px] rounded-lg border border-[var(--border)] bg-white px-2 py-1.5 text-sm"
    >
      <option value={NONE_VALUE}>(None)</option>
      {sources.map((source) => (
        <option key={source.incomeSourceId} value={source.incomeSourceId}>
          {source.name}
        </option>
      ))}
      <option value={ADD_NEW_VALUE}>+ Add income source…</option>
    </select>
  );
}
