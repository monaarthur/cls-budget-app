"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import { ChevronDown, ChevronUp, Save } from "lucide-react";
import { AppLink as Link } from "@/components/AppLink";
import { TopBar } from "@/components/layout/TopBar";
import { Card } from "@/components/ui/Card";
import { autoPayoffApi } from "@/features/auto-payoff/api/autoPayoffApi";
import { autoBudgetHref } from "@/features/auto-payoff/autoBudgetPath";
import { AutoBudgetList } from "@/features/auto-payoff/components/AutoBudgetList";
import type {
  AutoPayoffConfig,
  AutoPayoffPreview,
  SaveAutoPayoffConfigRequest,
} from "@/features/auto-payoff/types";
import { useAccountCategories } from "@/features/accounts/hooks/useAccountCategories";
import { useAccounts } from "@/features/accounts/hooks/useAccounts";
import { paymentsApi } from "@/features/payments/api/paymentsApi";
import type { BudgetPaymentStatusResponse } from "@/features/payments/types/payment";
import { ApiError } from "@/lib/api/client";
import {
  formatCurrencyDetailed,
  parseMoneyInputOrZero,
  sanitizeMoneyInput,
} from "@/lib/format";

const EMPTY_FORM: SaveAutoPayoffConfigRequest = {
  autoPayoffConfigId: null,
  name: "",
  excludeStatusIds: [3],
  excludeCategoryIds: [],
  excludeAccountIds: [],
  targetCategoryIds: [],
  targetByPayoffDate: false,
  extraMonthlyAmount: 0,
};

const FALLBACK_STATUSES: BudgetPaymentStatusResponse[] = [
  { budgetPaymentStatusId: 1, name: "Pending", description: "Not yet paid" },
  { budgetPaymentStatusId: 2, name: "Scheduled", description: "Scheduled for payment" },
  { budgetPaymentStatusId: 3, name: "Paid", description: "Payment completed" },
  { budgetPaymentStatusId: 4, name: "Failed", description: "Payment attempt failed" },
  { budgetPaymentStatusId: 5, name: "Overdue", description: "Past due and not paid" },
  { budgetPaymentStatusId: 6, name: "Unassigned", description: "Status not yet chosen" },
  {
    budgetPaymentStatusId: 7,
    name: "Scheduled Online",
    description: "Scheduled for online payment",
  },
];

function toggleId(ids: number[], id: number, on: boolean): number[] {
  if (on) return ids.includes(id) ? ids : [...ids, id];
  return ids.filter((value) => value !== id);
}

function moveId(ids: number[], id: number, direction: -1 | 1): number[] {
  const index = ids.indexOf(id);
  const nextIndex = index + direction;
  if (index < 0 || nextIndex < 0 || nextIndex >= ids.length) return ids;
  const next = [...ids];
  [next[index], next[nextIndex]] = [next[nextIndex], next[index]];
  return next;
}

function formatTargetDate(value: string | null): string {
  if (!value) return "—";
  const day = value.slice(0, 10);
  if (!/^\d{4}-\d{2}-\d{2}$/.test(day)) return "—";
  const [year, month, date] = day.split("-");
  return `${month}/${date}/${year}`;
}

function toForm(config: AutoPayoffConfig): SaveAutoPayoffConfigRequest {
  return {
    autoPayoffConfigId: config.autoPayoffConfigId ?? null,
    name: config.name ?? "",
    excludeStatusIds: config.excludeStatusIds ?? [],
    excludeCategoryIds: config.excludeCategoryIds ?? [],
    excludeAccountIds: config.excludeAccountIds ?? [],
    targetCategoryIds: config.targetCategoryIds ?? [],
    targetByPayoffDate: config.targetByPayoffDate,
    extraMonthlyAmount: config.extraMonthlyAmount,
  };
}

export function AutoPayoffPage() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const selectedParam = Number(searchParams.get("id"));
  const selectedConfigId =
    Number.isFinite(selectedParam) && selectedParam > 0 ? selectedParam : null;

  const { accounts, loading: accountsLoading, error: accountsError } =
    useAccounts();
  const {
    categories,
    loading: categoriesLoading,
    error: categoriesError,
  } = useAccountCategories();

  const [statuses, setStatuses] =
    useState<BudgetPaymentStatusResponse[]>(FALLBACK_STATUSES);
  const [form, setForm] = useState<SaveAutoPayoffConfigRequest>(EMPTY_FORM);
  const [savedConfigs, setSavedConfigs] = useState<AutoPayoffConfig[]>([]);
  const [extraInput, setExtraInput] = useState("0");
  const [ready, setReady] = useState(false);
  const [loading, setLoading] = useState(true);
  const [listLoading, setListLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [previewing, setPreviewing] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [savedMessage, setSavedMessage] = useState<string | null>(null);
  const [preview, setPreview] = useState<AutoPayoffPreview | null>(null);
  const [accountFilter, setAccountFilter] = useState("");

  const requestBody = useMemo<SaveAutoPayoffConfigRequest>(
    () => ({
      ...form,
      extraMonthlyAmount: Math.max(0, parseMoneyInputOrZero(extraInput)),
    }),
    [form, extraInput],
  );

  useEffect(() => {
    let cancelled = false;

    async function loadStatuses() {
      try {
        const statusResult = await paymentsApi.getStatuses();
        if (cancelled) return;
        const loaded = statusResult.data ?? [];
        if (loaded.length > 0) setStatuses(loaded);
      } catch {
        // Keep fallback statuses so the exclude checkboxes still render.
      }
    }

    async function loadSavedList() {
      setListLoading(true);
      try {
        const listResult = await autoPayoffApi.getAll();
        if (cancelled) return;
        setSavedConfigs(listResult.data ?? []);
      } catch (err) {
        if (cancelled) return;
        setError(
          err instanceof ApiError
            ? err.message
            : err instanceof Error
              ? err.message
              : "Failed to load Auto budgets",
        );
      } finally {
        if (!cancelled) setListLoading(false);
      }
    }

    void loadStatuses();
    void loadSavedList();
    return () => {
      cancelled = true;
    };
  }, []);

  useEffect(() => {
    let cancelled = false;

    async function loadSelected() {
      setLoading(true);
      setError(null);
      setSavedMessage(null);
      if (selectedConfigId == null) {
        setForm(EMPTY_FORM);
        setExtraInput("0");
        setReady(true);
        setLoading(false);
        return;
      }

      try {
        const result = await autoPayoffApi.getConfigById(selectedConfigId);
        if (cancelled) return;
        const config = result.data;
        if (config) {
          setForm(toForm(config));
          setExtraInput(String(config.extraMonthlyAmount ?? 0));
        }
        setReady(true);
      } catch (err) {
        if (cancelled) return;
        setError(
          err instanceof ApiError
            ? err.message
            : err instanceof Error
              ? err.message
              : "Failed to load Auto Budget",
        );
        setForm(EMPTY_FORM);
        setExtraInput("0");
        setReady(true);
      } finally {
        if (!cancelled) setLoading(false);
      }
    }

    void loadSelected();
    return () => {
      cancelled = true;
    };
  }, [selectedConfigId]);

  useEffect(() => {
    if (!ready) return;
    let cancelled = false;
    const timer = window.setTimeout(async () => {
      setPreviewing(true);
      try {
        const result = await autoPayoffApi.preview(requestBody);
        if (cancelled) return;
        setPreview(result.data);
      } catch (err) {
        if (cancelled) return;
        setError(
          err instanceof ApiError
            ? err.message
            : err instanceof Error
              ? err.message
              : "Failed to preview Auto Budget queue",
        );
      } finally {
        if (!cancelled) setPreviewing(false);
      }
    }, 300);

    return () => {
      cancelled = true;
      window.clearTimeout(timer);
    };
  }, [ready, requestBody]);

  const save = useCallback(async () => {
    const name = form.name.trim();
    if (!name) {
      setError("Auto Budget name is required.");
      return;
    }

    const duplicate = savedConfigs.some(
      (config) =>
        config.autoPayoffConfigId !== form.autoPayoffConfigId &&
        config.name.trim().toLowerCase() === name.toLowerCase(),
    );
    if (duplicate) {
      setError(`An Auto Budget named "${name}" already exists.`);
      return;
    }

    setSaving(true);
    setError(null);
    setSavedMessage(null);
    try {
      const result = await autoPayoffApi.saveConfig({
        ...requestBody,
        name,
      });
      const saved = result.data;
      if (saved) {
        setForm(toForm(saved));
        setExtraInput(String(saved.extraMonthlyAmount ?? 0));
      }
      const list = await autoPayoffApi.getAll();
      setSavedConfigs(list.data ?? []);
      setSavedMessage("Auto Budget saved.");
      const savedId = saved?.autoPayoffConfigId;
      if (savedId && savedId !== selectedConfigId) {
        router.replace(autoBudgetHref(savedId));
      }
    } catch (err) {
      setError(
        err instanceof ApiError
          ? err.message
          : err instanceof Error
            ? err.message
            : "Failed to save Auto Budget",
      );
    } finally {
      setSaving(false);
    }
  }, [form.autoPayoffConfigId, form.name, requestBody, router, savedConfigs, selectedConfigId]);

  const startNew = useCallback(() => {
    setForm(EMPTY_FORM);
    setExtraInput("0");
    setSavedMessage(null);
    setError(null);
    if (selectedConfigId != null) {
      router.push(autoBudgetHref());
    }
  }, [router, selectedConfigId]);

  const excludedCategorySet = useMemo(
    () => new Set(form.excludeCategoryIds),
    [form.excludeCategoryIds],
  );

  const visibleAccounts = useMemo(() => {
    const query = accountFilter.trim().toLowerCase();
    return accounts
      .filter(
        (account) =>
          account.balance > 0 ||
          form.excludeAccountIds.includes(account.accountId),
      )
      .filter((account) =>
        query
          ? `${account.name} ${account.accountCategoryName ?? ""}`
              .toLowerCase()
              .includes(query)
          : true,
      )
      .sort((a, b) => a.name.localeCompare(b.name));
  }, [accounts, accountFilter, form.excludeAccountIds]);

  const pageError =
    error ?? accountsError ?? categoriesError ?? null;

  return (
    <>
      <TopBar
        title={form.name.trim() ? `Auto Budget · ${form.name.trim()}` : "Auto Budget"}
        actions={
          <Link
            href="/config"
            className="text-sm font-medium text-[var(--link)] hover:underline"
          >
            Config
          </Link>
        }
      />

      <p className="mb-4 max-w-3xl text-sm text-[var(--muted)]">
        Extra monthly money is applied down this queue: skip excluded statuses,
        categories, and accounts; hit target categories first; then optionally
        order the rest by payoff or due date.
      </p>

      <Card className="mb-4 p-5">
        <h2 className="text-base font-semibold">Auto budgets</h2>
        <p className="mt-1 text-sm text-[var(--muted)]">
          Select a saved Auto Budget to view and edit it, or start a new one.
        </p>
        <AutoBudgetList
          configs={savedConfigs}
          selectedId={selectedConfigId}
          loading={listLoading}
        />
      </Card>

      {pageError ? (
        <p className="mb-4 rounded-xl border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-900">
          {pageError}
        </p>
      ) : null}
      {savedMessage ? (
        <p className="mb-4 rounded-xl border border-emerald-200 bg-emerald-50 px-3 py-2 text-sm text-emerald-900">
          {savedMessage}
        </p>
      ) : null}

      <Card className="mb-4 p-5">
        <h2 className="text-base font-semibold">Auto Budget name</h2>
        <p className="mt-1 text-sm text-[var(--muted)]">
          Each saved Auto Budget must have a unique name.
        </p>
        <div className="mt-3">
          <label className="block text-sm">
            <span className="mb-1.5 block font-medium">Name</span>
            <input
              type="text"
              value={form.name}
              maxLength={200}
              onChange={(event) =>
                setForm((current) => ({
                  ...current,
                  name: event.target.value,
                }))
              }
              placeholder="e.g. Credit cards first"
              className="w-full rounded-xl border border-[var(--border)] bg-white px-3 py-2.5 text-sm sm:max-w-md"
            />
          </label>
        </div>
      </Card>

      <div className="mb-4 flex flex-wrap items-center gap-3">
        <button
          type="button"
          onClick={() => void save()}
          disabled={saving || loading || !form.name.trim()}
          className="inline-flex items-center gap-2 rounded-full bg-[var(--accent)] px-5 py-2.5 text-sm font-semibold text-white disabled:opacity-50"
        >
          <Save size={15} aria-hidden />
          {saving ? "Saving…" : "Save Auto Budget"}
        </button>
        <button
          type="button"
          onClick={startNew}
          disabled={saving || loading}
          className="rounded-full border border-[var(--border)] px-4 py-2.5 text-sm font-medium disabled:opacity-50"
        >
          New Auto Budget
        </button>
        {previewing ? (
          <span className="text-sm text-[var(--muted)]">Updating queue…</span>
        ) : null}
      </div>

      <div className="grid gap-4 xl:grid-cols-2">
        <Card className="p-5">
          <h2 className="text-base font-semibold">Statuses to exclude</h2>
          <p className="mt-1 text-sm text-[var(--muted)]">
            Accounts whose latest payment is in a selected status are skipped.
            Paid is excluded by default.
          </p>
          <ul className="mt-3 space-y-1">
            {statuses.map((status) => (
              <li key={status.budgetPaymentStatusId}>
                <label className="flex cursor-pointer items-start gap-3 rounded-xl px-2 py-2 text-sm hover:bg-black/[0.03]">
                  <input
                    type="checkbox"
                    className="mt-0.5 h-4 w-4 shrink-0 accent-[var(--accent)]"
                    checked={form.excludeStatusIds.includes(
                      status.budgetPaymentStatusId,
                    )}
                    onChange={(event) =>
                      setForm((current) => ({
                        ...current,
                        excludeStatusIds: toggleId(
                          current.excludeStatusIds,
                          status.budgetPaymentStatusId,
                          event.target.checked,
                        ),
                      }))
                    }
                  />
                  <span>
                    <span className="block font-medium">{status.name}</span>
                    {status.description ? (
                      <span className="mt-0.5 block text-xs text-[var(--muted)]">
                        {status.description}
                      </span>
                    ) : null}
                  </span>
                </label>
              </li>
            ))}
          </ul>
        </Card>

        <Card className="p-5">
          <h2 className="text-base font-semibold">Categories to exclude</h2>
          <p className="mt-1 text-sm text-[var(--muted)]">
            Entire categories stay out of the Auto Budget queue.
          </p>
          <div className="mt-3 grid gap-2 sm:grid-cols-2">
            {categories.map((category) => (
              <label
                key={category.accountCategoryId}
                className="flex items-center gap-2 text-sm"
              >
                <input
                  type="checkbox"
                  checked={form.excludeCategoryIds.includes(
                    category.accountCategoryId,
                  )}
                  onChange={(event) => {
                    const on = event.target.checked;
                    setForm((current) => ({
                      ...current,
                      excludeCategoryIds: toggleId(
                        current.excludeCategoryIds,
                        category.accountCategoryId,
                        on,
                      ),
                      targetCategoryIds: on
                        ? current.targetCategoryIds.filter(
                            (id) => id !== category.accountCategoryId,
                          )
                        : current.targetCategoryIds,
                    }));
                  }}
                />
                {category.name}
              </label>
            ))}
          </div>
        </Card>

        <Card className="p-5">
          <h2 className="text-base font-semibold">Accounts to exclude</h2>
          <p className="mt-1 text-sm text-[var(--muted)]">
            Skip specific bills even if their category is eligible.
          </p>
          <input
            type="search"
            value={accountFilter}
            onChange={(event) => setAccountFilter(event.target.value)}
            placeholder="Filter accounts"
            className="mt-3 w-full rounded-xl border border-[var(--border)] bg-white px-3 py-2.5 text-sm"
          />
          <div className="mt-3 max-h-64 space-y-2 overflow-y-auto">
            {accountsLoading ? (
              <p className="text-sm text-[var(--muted)]">Loading accounts…</p>
            ) : visibleAccounts.length === 0 ? (
              <p className="text-sm text-[var(--muted)]">No matching accounts.</p>
            ) : (
              visibleAccounts.map((account) => (
                <label
                  key={account.accountId}
                  className="flex items-start gap-2 text-sm"
                >
                  <input
                    type="checkbox"
                    className="mt-1"
                    checked={form.excludeAccountIds.includes(account.accountId)}
                    onChange={(event) =>
                      setForm((current) => ({
                        ...current,
                        excludeAccountIds: toggleId(
                          current.excludeAccountIds,
                          account.accountId,
                          event.target.checked,
                        ),
                      }))
                    }
                  />
                  <span>
                    <span className="font-medium">{account.name}</span>
                    <span className="mt-0.5 block text-xs text-[var(--muted)]">
                      {account.accountCategoryName ?? "Uncategorized"} ·{" "}
                      {formatCurrencyDetailed(account.balance)}
                    </span>
                  </span>
                </label>
              ))
            )}
          </div>
        </Card>

        <Card className="p-5">
          <h2 className="text-base font-semibold">Categories to target first</h2>
          <p className="mt-1 text-sm text-[var(--muted)]">
            Checked categories are paid in this order before remaining eligible
            debts.
          </p>
          <div className="mt-3 space-y-2">
            {categoriesLoading ? (
              <p className="text-sm text-[var(--muted)]">Loading categories…</p>
            ) : (
              categories.map((category) => {
                const excluded = excludedCategorySet.has(
                  category.accountCategoryId,
                );
                const rank = form.targetCategoryIds.indexOf(
                  category.accountCategoryId,
                );
                const targeted = rank >= 0;
                return (
                  <div
                    key={category.accountCategoryId}
                    className="flex items-center gap-2 text-sm"
                  >
                    <label className="flex min-w-0 flex-1 items-center gap-2">
                      <input
                        type="checkbox"
                        checked={targeted}
                        disabled={excluded}
                        onChange={(event) =>
                          setForm((current) => ({
                            ...current,
                            targetCategoryIds: toggleId(
                              current.targetCategoryIds,
                              category.accountCategoryId,
                              event.target.checked,
                            ),
                          }))
                        }
                      />
                      <span className={excluded ? "text-[var(--muted)]" : ""}>
                        {targeted ? (
                          <span className="mr-2 inline-flex h-5 min-w-5 items-center justify-center rounded-full bg-[var(--accent-soft)] px-1.5 text-xs font-semibold text-[var(--link)]">
                            {rank + 1}
                          </span>
                        ) : null}
                        {category.name}
                        {excluded ? " (excluded)" : ""}
                      </span>
                    </label>
                    {targeted ? (
                      <span className="flex shrink-0 gap-1">
                        <button
                          type="button"
                          aria-label={`Move ${category.name} up`}
                          disabled={rank === 0}
                          onClick={() =>
                            setForm((current) => ({
                              ...current,
                              targetCategoryIds: moveId(
                                current.targetCategoryIds,
                                category.accountCategoryId,
                                -1,
                              ),
                            }))
                          }
                          className="rounded-lg p-1 text-[var(--muted)] hover:bg-black/[0.04] disabled:opacity-30"
                        >
                          <ChevronUp size={16} />
                        </button>
                        <button
                          type="button"
                          aria-label={`Move ${category.name} down`}
                          disabled={rank === form.targetCategoryIds.length - 1}
                          onClick={() =>
                            setForm((current) => ({
                              ...current,
                              targetCategoryIds: moveId(
                                current.targetCategoryIds,
                                category.accountCategoryId,
                                1,
                              ),
                            }))
                          }
                          className="rounded-lg p-1 text-[var(--muted)] hover:bg-black/[0.04] disabled:opacity-30"
                        >
                          <ChevronDown size={16} />
                        </button>
                      </span>
                    ) : null}
                  </div>
                );
              })
            )}
          </div>
        </Card>
      </div>

      <Card className="mt-4 p-5">
        <h2 className="text-base font-semibold">Extra payment targeting</h2>
        <div className="mt-3 grid gap-4 sm:grid-cols-2">
          <label className="flex items-start gap-2 text-sm">
            <input
              type="checkbox"
              className="mt-1"
              checked={form.targetByPayoffDate}
              onChange={(event) =>
                setForm((current) => ({
                  ...current,
                  targetByPayoffDate: event.target.checked,
                }))
              }
            />
            <span>
              <span className="font-medium">Target items by payoff date</span>
              <span className="mt-0.5 block text-xs text-[var(--muted)]">
                After target categories, remaining debts are ordered by the next
                due or estimated payoff date.
              </span>
            </span>
          </label>
          <label className="block text-sm">
            <span className="mb-1.5 block font-medium">
              Extra monthly amount
            </span>
            <input
              type="text"
              inputMode="decimal"
              value={extraInput}
              onChange={(event) =>
                setExtraInput(sanitizeMoneyInput(event.target.value))
              }
              className="w-full rounded-xl border border-[var(--border)] bg-white px-3 py-2.5 text-sm"
            />
          </label>
        </div>
      </Card>

      <Card className="mt-4 overflow-hidden">
        <div className="flex flex-wrap items-baseline justify-between gap-2 px-5 pt-5">
          <h2 className="text-base font-semibold">Auto Budget queue</h2>
          {preview ? (
            <p className="text-sm text-[var(--muted)]">
              Extra {formatCurrencyDetailed(preview.extraPayment)} · allocated{" "}
              {formatCurrencyDetailed(preview.extraAllocated)}
              {preview.extraRemaining > 0
                ? ` · leftover ${formatCurrencyDetailed(preview.extraRemaining)}`
                : ""}
            </p>
          ) : null}
        </div>
        <div className="mt-3 overflow-x-auto px-3 pb-5">
          <table className="min-w-full text-left text-sm">
            <thead className="text-xs text-[var(--muted)]">
              <tr>
                <th className="px-2 py-1.5 font-medium">Rank</th>
                <th className="px-2 py-1.5 font-medium">Account</th>
                <th className="px-2 py-1.5 font-medium">Category</th>
                <th className="px-2 py-1.5 font-medium">Balance</th>
                <th className="px-2 py-1.5 font-medium">Monthly</th>
                <th className="px-2 py-1.5 font-medium">Target date</th>
                <th className="px-2 py-1.5 font-medium">Extra</th>
                <th className="px-2 py-1.5 font-medium">Reason</th>
              </tr>
            </thead>
            <tbody>
              {(preview?.queue ?? []).map((item) => (
                <tr
                  key={item.accountId}
                  className="border-t border-[var(--border)]"
                >
                  <td className="px-2 py-1.5">{item.rank}</td>
                  <td className="px-2 py-1.5 font-medium">{item.name}</td>
                  <td className="px-2 py-1.5">{item.categoryName}</td>
                  <td className="px-2 py-1.5">
                    {formatCurrencyDetailed(item.balance)}
                  </td>
                  <td className="px-2 py-1.5">
                    {formatCurrencyDetailed(item.monthlyPayment)}
                  </td>
                  <td className="px-2 py-1.5">
                    {formatTargetDate(item.targetDate)}
                  </td>
                  <td className="px-2 py-1.5">
                    {item.extraAllocated > 0
                      ? formatCurrencyDetailed(item.extraAllocated)
                      : "—"}
                  </td>
                  <td className="px-2 py-1.5 text-[var(--muted)]">
                    {item.reason}
                  </td>
                </tr>
              ))}
              {!preview?.queue.length ? (
                <tr className="border-t border-[var(--border)]">
                  <td
                    colSpan={8}
                    className="px-2 py-6 text-center text-sm text-[var(--muted)]"
                  >
                    {loading
                      ? "Loading Auto Budget queue…"
                      : "No eligible accounts in the queue."}
                  </td>
                </tr>
              ) : null}
            </tbody>
          </table>
        </div>
      </Card>

      {preview?.excluded.length ? (
        <Card className="mt-4 p-5">
          <h2 className="text-base font-semibold">Excluded</h2>
          <ul className="mt-3 space-y-1.5 text-sm">
            {preview.excluded.map((item) => (
              <li key={item.accountId}>
                <span className="font-medium">{item.name}</span>
                <span className="text-[var(--muted)]"> — {item.reason}</span>
              </li>
            ))}
          </ul>
        </Card>
      ) : null}
    </>
  );
}
