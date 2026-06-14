"use client";

import { useEffect, useMemo, useState } from "react";
import { Plus, X } from "lucide-react";
import { accountsApi } from "@/features/accounts/api/accountsApi";
import {
  ACCOUNT_CATEGORY_NAMES,
  getAccountCategoryId,
} from "@/features/accounts/data/accountCategories";
import type { CreateAccountRequest } from "@/features/accounts/types/account";
import { dateInputToIso, toDateInputValue } from "@/features/budgets/utils/budgetFormat";
import { Card } from "@/components/ui/Card";
import { ApiError } from "@/lib/api/client";

export function AddAccountDialog({
  creditCardOnly = false,
  onClose,
  onAdded,
}: {
  creditCardOnly?: boolean;
  onClose: () => void;
  onAdded: () => void;
}) {
  const defaultCategory = creditCardOnly ? "Credit Card" : "Checking";
  const [name, setName] = useState("");
  const [number, setNumber] = useState("");
  const [categoryName, setCategoryName] = useState(defaultCategory);
  const [balance, setBalance] = useState("0");
  const [limit, setLimit] = useState("0");
  const [monthlyPayment, setMonthlyPayment] = useState("");
  const [paymentDay, setPaymentDay] = useState("");
  const [openDate, setOpenDate] = useState(toDateInputValue(new Date().toISOString()));
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const categoryOptions = useMemo(
    () =>
      creditCardOnly
        ? ["Credit Card"]
        : ACCOUNT_CATEGORY_NAMES.filter((item) => item !== "Credit Card"),
    [creditCardOnly],
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
    const categoryId = getAccountCategoryId(categoryName);
    if (!categoryId || !name.trim() || !number.trim() || !openDate) return;

    const parsedBalance = Number(balance);
    const parsedLimit = Number(limit);
    if (!Number.isFinite(parsedBalance) || !Number.isFinite(parsedLimit)) {
      setError("Balance and limit must be valid numbers.");
      return;
    }

    setSubmitting(true);
    setError(null);

    const body: CreateAccountRequest = {
      name: name.trim(),
      number: number.trim(),
      balance: parsedBalance,
      limit: parsedLimit,
      accountOpenDate: dateInputToIso(openDate),
      phone: "000-000-0000",
      email: "import@cls-budget.local",
      url: "https://import.cls-budget.local",
      isPaidOff: false,
      isCreditCard: creditCardOnly ? true : categoryName === "Credit Card",
      accountCategoryId: categoryId,
      monthlyPayment: monthlyPayment.trim()
        ? Number(monthlyPayment)
        : null,
      paymentDay: paymentDay.trim() ? Number.parseInt(paymentDay, 10) : null,
    };

    try {
      await accountsApi.create(body);
      onAdded();
      onClose();
    } catch (err) {
      const message =
        err instanceof ApiError
          ? err.errors.join(", ") || err.message
          : err instanceof Error
            ? err.message
            : "Failed to create account";
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
                Add {creditCardOnly ? "credit card" : "account"}
              </h2>
            </div>
            <button type="button" onClick={onClose} aria-label="Close">
              <X size={18} />
            </button>
          </div>

          <form className="space-y-3" onSubmit={(event) => void handleSubmit(event)}>
            <label className="block text-sm">
              <span className="mb-1 block font-medium">Name</span>
              <input
                value={name}
                onChange={(e) => setName(e.target.value)}
                className="w-full rounded-xl border border-[var(--border)] px-3 py-2"
                required
              />
            </label>
            <label className="block text-sm">
              <span className="mb-1 block font-medium">Number</span>
              <input
                value={number}
                onChange={(e) => setNumber(e.target.value)}
                className="w-full rounded-xl border border-[var(--border)] px-3 py-2"
                required
              />
            </label>
            {!creditCardOnly ? (
              <label className="block text-sm">
                <span className="mb-1 block font-medium">Category</span>
                <select
                  value={categoryName}
                  onChange={(e) => setCategoryName(e.target.value)}
                  className="w-full rounded-xl border border-[var(--border)] px-3 py-2"
                >
                  {categoryOptions.map((option) => (
                    <option key={option} value={option}>
                      {option}
                    </option>
                  ))}
                </select>
              </label>
            ) : null}
            <div className="grid grid-cols-2 gap-3">
              <label className="block text-sm">
                <span className="mb-1 block font-medium">Balance</span>
                <input
                  type="number"
                  step="0.01"
                  value={balance}
                  onChange={(e) => setBalance(e.target.value)}
                  className="w-full rounded-xl border border-[var(--border)] px-3 py-2"
                />
              </label>
              <label className="block text-sm">
                <span className="mb-1 block font-medium">Limit</span>
                <input
                  type="number"
                  step="0.01"
                  value={limit}
                  onChange={(e) => setLimit(e.target.value)}
                  className="w-full rounded-xl border border-[var(--border)] px-3 py-2"
                />
              </label>
            </div>
            <div className="grid grid-cols-2 gap-3">
              <label className="block text-sm">
                <span className="mb-1 block font-medium">Monthly payment</span>
                <input
                  type="number"
                  step="0.01"
                  value={monthlyPayment}
                  onChange={(e) => setMonthlyPayment(e.target.value)}
                  className="w-full rounded-xl border border-[var(--border)] px-3 py-2"
                />
              </label>
              <label className="block text-sm">
                <span className="mb-1 block font-medium">Payment day</span>
                <input
                  type="number"
                  min="1"
                  max="31"
                  value={paymentDay}
                  onChange={(e) => setPaymentDay(e.target.value)}
                  className="w-full rounded-xl border border-[var(--border)] px-3 py-2"
                />
              </label>
            </div>
            <label className="block text-sm">
              <span className="mb-1 block font-medium">Opened</span>
              <input
                type="date"
                value={openDate}
                onChange={(e) => setOpenDate(e.target.value)}
                className="w-full rounded-xl border border-[var(--border)] px-3 py-2"
                required
              />
            </label>
            {error ? <p className="text-sm text-[var(--negative)]">{error}</p> : null}
            <button
              type="submit"
              disabled={submitting}
              className="inline-flex w-full items-center justify-center gap-2 rounded-full bg-[var(--accent)] px-4 py-2.5 text-sm font-semibold text-white disabled:opacity-50"
            >
              <Plus size={16} aria-hidden />
              {submitting ? "Creating…" : "Create"}
            </button>
          </form>
        </div>
      </Card>
    </div>
  );
}
