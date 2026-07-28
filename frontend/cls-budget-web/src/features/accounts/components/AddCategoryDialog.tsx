"use client";

import { useEffect, useMemo, useState } from "react";
import { Plus, X } from "lucide-react";
import { accountCategoriesApi } from "@/features/accounts/api/accountCategoriesApi";
import type { AccountCategoryResponse } from "@/features/accounts/types/accountCategory";
import { Card } from "@/components/ui/Card";
import { ApiError } from "@/lib/api/client";

type Mode = "category" | "subcategory";

export function AddCategoryDialog({
  categories,
  defaultMode = "category",
  defaultCategoryId,
  onClose,
  onSaved,
}: {
  categories: AccountCategoryResponse[];
  defaultMode?: Mode;
  defaultCategoryId?: number;
  onClose: () => void;
  onSaved: (created: AccountCategoryResponse | null) => void;
}) {
  const [mode, setMode] = useState<Mode>(defaultMode);
  const [name, setName] = useState("");
  const [description, setDescription] = useState("");
  const [parentCategoryId, setParentCategoryId] = useState<number>(
    defaultCategoryId ?? categories[0]?.accountCategoryId ?? 0,
  );
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const parentOptions = useMemo(
    () =>
      categories.length > 0
        ? categories
        : [{ accountCategoryId: 0, name: "No categories loaded", isSystem: true, description: null, subCategories: [] }],
    [categories],
  );

  useEffect(() => {
    function onKeyDown(event: KeyboardEvent) {
      if (event.key === "Escape" && !submitting) onClose();
    }
    document.addEventListener("keydown", onKeyDown);
    return () => document.removeEventListener("keydown", onKeyDown);
  }, [onClose, submitting]);

  async function handleSubmit(event: React.FormEvent) {
    event.preventDefault();
    const trimmed = name.trim();
    if (!trimmed) return;
    if (mode === "subcategory" && parentCategoryId <= 0) {
      setError("Select a parent category.");
      return;
    }

    setSubmitting(true);
    setError(null);
    try {
      if (mode === "category") {
        const result = await accountCategoriesApi.create({
          name: trimmed,
          description: description.trim() || null,
        });
        onSaved(result.data ?? null);
      } else {
        await accountCategoriesApi.createSubCategory({
          accountCategoryId: parentCategoryId,
          name: trimmed,
          description: description.trim() || null,
        });
        onSaved(null);
      }
      onClose();
    } catch (err) {
      const message =
        err instanceof ApiError
          ? err.errors.join(", ") || err.message
          : err instanceof Error
            ? err.message
            : "Failed to save";
      setError(message);
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4"
      role="dialog"
      aria-modal="true"
      onClick={submitting ? undefined : onClose}
    >
      <Card className="w-full max-w-md p-5 shadow-xl">
        <div onClick={(event) => event.stopPropagation()}>
          <div className="mb-4 flex items-start justify-between gap-3">
            <div>
              <h2 className="text-lg font-semibold">
                {mode === "category" ? "Add category" : "Add subcategory"}
              </h2>
              <p className="mt-1 text-sm text-[var(--muted)]">
                New values appear in the account grid dropdowns.
              </p>
            </div>
            <button type="button" onClick={onClose} aria-label="Close">
              <X size={18} />
            </button>
          </div>

          <div className="mb-3 flex gap-2">
            <button
              type="button"
              onClick={() => setMode("category")}
              className={`rounded-full px-3 py-1.5 text-sm font-medium ${
                mode === "category"
                  ? "bg-[var(--accent)] text-white"
                  : "border border-[var(--border)] bg-white"
              }`}
            >
              Category
            </button>
            <button
              type="button"
              onClick={() => setMode("subcategory")}
              className={`rounded-full px-3 py-1.5 text-sm font-medium ${
                mode === "subcategory"
                  ? "bg-[var(--accent)] text-white"
                  : "border border-[var(--border)] bg-white"
              }`}
            >
              Subcategory
            </button>
          </div>

          <form className="space-y-3" onSubmit={(event) => void handleSubmit(event)}>
            {mode === "subcategory" ? (
              <label className="block text-sm">
                <span className="mb-1 block font-medium">Parent category</span>
                <select
                  value={parentCategoryId}
                  onChange={(e) => setParentCategoryId(Number(e.target.value))}
                  className="w-full rounded-xl border border-[var(--border)] px-3 py-2"
                >
                  {parentOptions.map((option) => (
                    <option
                      key={option.accountCategoryId}
                      value={option.accountCategoryId}
                    >
                      {option.name}
                    </option>
                  ))}
                </select>
              </label>
            ) : null}
            <label className="block text-sm">
              <span className="mb-1 block font-medium">Name</span>
              <input
                value={name}
                onChange={(e) => setName(e.target.value)}
                className="w-full rounded-xl border border-[var(--border)] px-3 py-2"
                required
                autoFocus
              />
            </label>
            <label className="block text-sm">
              <span className="mb-1 block font-medium">Description (optional)</span>
              <input
                value={description}
                onChange={(e) => setDescription(e.target.value)}
                className="w-full rounded-xl border border-[var(--border)] px-3 py-2"
              />
            </label>
            {error ? <p className="text-sm text-[var(--negative)]">{error}</p> : null}
            <button
              type="submit"
              disabled={submitting}
              className="inline-flex w-full items-center justify-center gap-2 rounded-full bg-[var(--accent)] px-4 py-2.5 text-sm font-semibold text-white disabled:opacity-50"
            >
              <Plus size={16} aria-hidden />
              {submitting ? "Saving…" : "Save"}
            </button>
          </form>
        </div>
      </Card>
    </div>
  );
}
