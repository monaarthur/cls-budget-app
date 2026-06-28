"use client";

import { useEffect, useState } from "react";
import { Plus } from "lucide-react";
import { useRouter } from "next/navigation";
import { budgetsApi } from "@/features/budgets/api/budgetsApi";
import { budgetTemplatesApi } from "@/features/budgets/api/budgetTemplatesApi";
import {
  dateInputToIso,
  getDefaultBudgetPeriod,
  nameFromStartDate,
  toDateInputValue,
} from "@/features/budgets/utils/budgetFormat";
import type { BudgetTemplateResponse } from "@/features/budgets/types/budgetTemplate";
import { Card } from "@/components/ui/Card";
import { ApiError } from "@/lib/api/client";

export function AddBudgetForm({ onCreated }: { onCreated: (name: string) => void }) {
  const router = useRouter();
  const defaults = getDefaultBudgetPeriod();
  const [templates, setTemplates] = useState<BudgetTemplateResponse[]>([]);
  const [templateId, setTemplateId] = useState<number | "">("");
  const [name, setName] = useState(defaults.name);
  const [startDate, setStartDate] = useState(toDateInputValue(defaults.startPeriod));
  const [endDate, setEndDate] = useState(toDateInputValue(defaults.endPeriod));
  const [loadingTemplates, setLoadingTemplates] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    async function loadTemplates() {
      setLoadingTemplates(true);
      try {
        const result = await budgetTemplatesApi.getAll();
        const items = result.data ?? [];
        setTemplates(items);
        if (items.length > 0) {
          setTemplateId(items[0].budgetTemplateId);
        }
      } catch (err) {
        const message =
          err instanceof ApiError
            ? err.message
            : err instanceof Error
              ? err.message
              : "Failed to load budget templates";
        setError(message);
      } finally {
        setLoadingTemplates(false);
      }
    }

    void loadTemplates();
  }, []);

  function handleStartDateChange(value: string) {
    setStartDate(value);
    setName(nameFromStartDate(value));
  }

  async function handleSubmit(event: React.FormEvent) {
    event.preventDefault();

    if (templateId === "") {
      setError("Select a budget template.");
      return;
    }

    const trimmedName = name.trim();
    if (endDate < startDate) {
      setError("End date must be on or after the start date.");
      return;
    }

    setSubmitting(true);
    setError(null);

    try {
      const result = await budgetsApi.create({
        name: trimmedName,
        startPeriod: dateInputToIso(startDate),
        endPeriod: dateInputToIso(endDate),
        budgetTemplateId: templateId,
      });

      const created = result.data;
      if (!created) {
        throw new Error("Budget was not returned from the API.");
      }

      onCreated(trimmedName);
      router.push(`/budgets/detail?id=${created.budgetId}`);
    } catch (err) {
      const message =
        err instanceof ApiError
          ? err.errors.length > 0
            ? err.errors.join(" ")
            : err.message
          : err instanceof Error
            ? err.message
            : "Failed to create budget";
      setError(message);
    } finally {
      setSubmitting(false);
    }
  }

  const selectedTemplate = templates.find(
    (template) => template.budgetTemplateId === templateId,
  );

  return (
    <Card className="p-5">
      <h2 className="text-base font-semibold">New budget</h2>
      <p className="mt-1 text-sm text-[var(--muted)]">
        Choose a template and set the budget period.
      </p>

      <form onSubmit={(e) => void handleSubmit(e)} className="mt-4 space-y-4">
        <label className="block">
          <span className="text-sm font-medium">Template</span>
          <select
            required
            disabled={loadingTemplates || submitting}
            value={templateId}
            onChange={(e) =>
              setTemplateId(e.target.value ? Number(e.target.value) : "")
            }
            className="mt-1 w-full rounded-xl border border-[var(--border)] bg-white px-3 py-2 text-sm outline-none focus:border-[var(--link)] disabled:opacity-60"
          >
            {loadingTemplates ? (
              <option value="">Loading templates…</option>
            ) : templates.length === 0 ? (
              <option value="">No templates available</option>
            ) : (
              templates.map((template) => (
                <option key={template.budgetTemplateId} value={template.budgetTemplateId}>
                  {template.name}
                </option>
              ))
            )}
          </select>
          {selectedTemplate?.description ? (
            <span className="mt-1 block text-xs text-[var(--muted)]">
              {selectedTemplate.description}
            </span>
          ) : null}
        </label>

        <label className="block">
          <span className="text-sm font-medium">Name</span>
          <input
            type="text"
            required
            value={name}
            onChange={(e) => setName(e.target.value)}
            disabled={submitting}
            className="mt-1 w-full rounded-xl border border-[var(--border)] bg-white px-3 py-2 text-sm outline-none focus:border-[var(--link)]"
          />
        </label>

        <div className="grid grid-cols-2 gap-3">
          <label className="block">
            <span className="text-sm font-medium">Start</span>
            <input
              type="date"
              required
              value={startDate}
              onChange={(e) => handleStartDateChange(e.target.value)}
              disabled={submitting}
              className="mt-1 w-full rounded-xl border border-[var(--border)] bg-white px-3 py-2 text-sm outline-none focus:border-[var(--link)]"
            />
          </label>
          <label className="block">
            <span className="text-sm font-medium">End</span>
            <input
              type="date"
              required
              value={endDate}
              onChange={(e) => setEndDate(e.target.value)}
              disabled={submitting}
              className="mt-1 w-full rounded-xl border border-[var(--border)] bg-white px-3 py-2 text-sm outline-none focus:border-[var(--link)]"
            />
          </label>
        </div>

        {error ? (
          <p className="text-sm text-[var(--negative)]">{error}</p>
        ) : null}

        <button
          type="submit"
          disabled={
            submitting ||
            loadingTemplates ||
            templateId === "" ||
            templates.length === 0 ||
            !name.trim()
          }
          className="inline-flex items-center gap-2 rounded-full bg-[var(--link)] px-4 py-2 text-sm font-semibold text-white disabled:cursor-not-allowed disabled:opacity-50"
        >
          <Plus className="h-4 w-4" />
          {submitting ? "Creating…" : "Create budget"}
        </button>
      </form>
    </Card>
  );
}
