"use client";

import Link from "next/link";
import { useCallback, useEffect, useState, type FormEvent } from "react";
import { activePayoffPlanApi } from "@/features/credit-cards/payoff/api/activePayoffPlanApi";
import type {
  ActivePayoffPlan,
  ActivePayoffPlanHistory,
} from "@/features/credit-cards/payoff/types";
import { useCreditCards } from "@/features/accounts/hooks/useCreditCards";
import { ApiError } from "@/lib/api/client";
import {
  formatCurrencyDetailed,
  parseMoneyInput,
  sanitizeMoneyInput,
} from "@/lib/format";

function formatDate(value: string | null | undefined): string {
  if (!value) return "—";
  const d = new Date(value.length <= 10 ? `${value}T00:00:00` : value);
  if (Number.isNaN(d.getTime())) return value;
  return d.toLocaleDateString(undefined, {
    year: "numeric",
    month: "short",
    day: "numeric",
  });
}

function eventLabel(eventType: string): string {
  switch (eventType) {
    case "Started":
      return "Started";
    case "PaymentRecorded":
      return "Payment";
    case "PaymentVoided":
      return "Payment voided";
    case "Revised":
      return "Revised";
    case "Completed":
      return "Completed";
    case "Abandoned":
      return "Abandoned";
    default:
      return eventType;
  }
}

export function ActivePayoffPlanPanel() {
  const { accounts, loading: cardsLoading, reload: reloadCards } = useCreditCards();
  const [plan, setPlan] = useState<ActivePayoffPlan | null>(null);
  const [history, setHistory] = useState<ActivePayoffPlanHistory | null>(null);
  const [loading, setLoading] = useState(true);
  const [status, setStatus] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const [paymentAccountId, setPaymentAccountId] = useState("");
  const [paymentAmount, setPaymentAmount] = useState("");
  const [paymentDate, setPaymentDate] = useState(
    () => new Date().toISOString().slice(0, 10),
  );
  const [paymentNotes, setPaymentNotes] = useState("");

  const [reviseOpen, setReviseOpen] = useState(false);
  const [reviseName, setReviseName] = useState("");
  const [reviseStrategy, setReviseStrategy] = useState("Avalanche");
  const [reviseMonthly, setReviseMonthly] = useState("");
  const [reviseExtra, setReviseExtra] = useState("");
  const [reviseReason, setReviseReason] = useState("");

  const includedCards = accounts.filter(
    (a) =>
      !a.isPaidOff &&
      a.balance > 0 &&
      (a.includeInPayoffAnalysis ?? true),
  );

  const load = useCallback(async () => {
    setLoading(true);
    setStatus(null);
    try {
      const active = await activePayoffPlanApi.getActive();
      setPlan(active);
      if (active) {
        const hist = await activePayoffPlanApi.history();
        setHistory(hist);
        setReviseName(active.name);
        setReviseStrategy(active.strategy);
        setReviseMonthly(String(active.totalMonthlyDebtPayment));
        setReviseExtra(String(active.extraMonthlyPayment));
      } else {
        setHistory(null);
      }
    } catch (err) {
      setStatus(
        err instanceof ApiError
          ? err.errors.join(", ") || err.message
          : err instanceof Error
            ? err.message
            : "Failed to load active plan",
      );
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void load();
  }, [load]);

  useEffect(() => {
    if (!paymentAccountId && includedCards[0]) {
      setPaymentAccountId(String(includedCards[0].accountId));
    }
  }, [includedCards, paymentAccountId]);

  async function handleRecordPayment(event: FormEvent) {
    event.preventDefault();
    const accountId = Number(paymentAccountId);
    const amount = parseMoneyInput(paymentAmount);
    if (!Number.isFinite(accountId) || accountId <= 0) {
      setStatus("Select a credit card.");
      return;
    }
    if (amount === null || amount <= 0) {
      setStatus("Enter a payment amount greater than zero.");
      return;
    }

    setBusy(true);
    setStatus(null);
    try {
      await activePayoffPlanApi.recordPayment({
        accountId,
        amount,
        paymentDate: paymentDate || null,
        notes: paymentNotes.trim() || null,
      });
      setPaymentAmount("");
      setPaymentNotes("");
      await reloadCards();
      await load();
      setStatus("Payment recorded and card balance updated.");
    } catch (err) {
      setStatus(
        err instanceof ApiError
          ? err.errors.join(", ") || err.message
          : err instanceof Error
            ? err.message
            : "Failed to record payment",
      );
    } finally {
      setBusy(false);
    }
  }

  async function handleVoidPayment(paymentId: number) {
    if (!window.confirm("Void this payment and restore the card balance?")) {
      return;
    }
    setBusy(true);
    setStatus(null);
    try {
      await activePayoffPlanApi.voidPayment(paymentId);
      await reloadCards();
      await load();
      setStatus("Payment voided.");
    } catch (err) {
      setStatus(
        err instanceof ApiError
          ? err.errors.join(", ") || err.message
          : err instanceof Error
            ? err.message
            : "Failed to void payment",
      );
    } finally {
      setBusy(false);
    }
  }

  async function handleRevise(event: FormEvent) {
    event.preventDefault();
    if (!plan) return;
    const monthly = parseMoneyInput(reviseMonthly);
    const extra = parseMoneyInput(reviseExtra) ?? 0;
    if (!reviseName.trim()) {
      setStatus("Enter a plan name.");
      return;
    }
    if (monthly === null || monthly <= 0) {
      setStatus("Enter a monthly debt payment greater than zero.");
      return;
    }

    setBusy(true);
    setStatus(null);
    try {
      await activePayoffPlanApi.revise({
        name: reviseName.trim(),
        goal: plan.goal,
        strategy: reviseStrategy,
        extraMonthlyPayment: Math.max(0, extra),
        totalMonthlyDebtPayment: monthly,
        targetUtilizationPercent: plan.targetUtilizationPercent,
        payOverLimitFirst: plan.payOverLimitFirst,
        postUtilizationStrategy:
          plan.postUtilizationStrategy === "Avalanche" ||
          plan.postUtilizationStrategy === "Snowball"
            ? plan.postUtilizationStrategy
            : null,
        enableCashAdvanceBalanceMoves: plan.enableCashAdvanceBalanceMoves,
        loanAmount: plan.loanAmount,
        loanAnnualPercentageRate: plan.loanAnnualPercentageRate,
        loanApplyStrategy:
          plan.loanApplyStrategy === "Avalanche" ||
          plan.loanApplyStrategy === "Snowball" ||
          plan.loanApplyStrategy === "SelectedAccounts"
            ? plan.loanApplyStrategy
            : null,
        loanApplyCreditCardIds: plan.loanApplyCreditCardIds ?? [],
        loanType:
          plan.loanType === "Personal" ||
          plan.loanType === "HomeEquity" ||
          plan.loanType === "Heloc" ||
          plan.loanType === "Retirement401k" ||
          plan.loanType === "Family"
            ? plan.loanType
            : null,
        loanTermMonths: plan.loanTermMonths,
        loanInterestOnlyMonths: plan.loanInterestOnlyMonths,
        loanFixedMonthlyPayment: plan.loanFixedMonthlyPayment,
        promotionalTransfers: plan.promotionalTransfers,
        reason: reviseReason.trim() || null,
      });
      setReviseOpen(false);
      setReviseReason("");
      await load();
      setStatus("Plan revised (new version saved).");
    } catch (err) {
      setStatus(
        err instanceof ApiError
          ? err.errors.join(", ") || err.message
          : err instanceof Error
            ? err.message
            : "Failed to revise plan",
      );
    } finally {
      setBusy(false);
    }
  }

  async function handleComplete() {
    if (!window.confirm("Mark this plan as completed?")) return;
    setBusy(true);
    setStatus(null);
    try {
      await activePayoffPlanApi.complete();
      setPlan(null);
      setHistory(null);
      setStatus("Plan completed.");
    } catch (err) {
      setStatus(
        err instanceof ApiError
          ? err.errors.join(", ") || err.message
          : err instanceof Error
            ? err.message
            : "Failed to complete plan",
      );
    } finally {
      setBusy(false);
    }
  }

  async function handleAbandon() {
    if (!window.confirm("Abandon this active plan? Payment history is kept on the archived plan.")) {
      return;
    }
    setBusy(true);
    setStatus(null);
    try {
      await activePayoffPlanApi.abandon();
      setPlan(null);
      setHistory(null);
      setStatus("Plan abandoned.");
    } catch (err) {
      setStatus(
        err instanceof ApiError
          ? err.errors.join(", ") || err.message
          : err instanceof Error
            ? err.message
            : "Failed to abandon plan",
      );
    } finally {
      setBusy(false);
    }
  }

  if (loading || cardsLoading) {
    return (
      <p className="text-sm text-[var(--muted)]">Loading active plan…</p>
    );
  }

  if (!plan) {
    return (
      <div className="space-y-3">
        {status ? (
          <p className="text-sm text-[var(--muted)]">{status}</p>
        ) : null}
        <p className="text-sm text-[var(--muted)]">
          No active payoff plan. Build a plan in the calculator, then choose{" "}
          <span className="font-medium text-[var(--foreground)]">Start this plan</span>.
        </p>
        <Link
          href="/credit-cards/payoff"
          className="inline-flex rounded-full bg-[var(--link)] px-4 py-2 text-sm font-semibold text-white"
        >
          Open Payoff Calculator
        </Link>
      </div>
    );
  }

  const progress = plan.progress;

  return (
    <div className="space-y-8">
      {status ? (
        <p className="text-sm text-[var(--muted)]" role="status">
          {status}
        </p>
      ) : null}

      <section className="space-y-3">
        <div className="flex flex-wrap items-start justify-between gap-3">
          <div>
            <h2 className="text-lg font-semibold">{plan.name}</h2>
            <p className="mt-1 text-sm text-[var(--muted)]">
              {plan.strategy} · ${plan.totalMonthlyDebtPayment.toFixed(2)}/mo ·
              version {plan.currentVersionNumber} · started{" "}
              {formatDate(plan.startedOnUtc)}
            </p>
          </div>
          <div className="flex flex-wrap gap-2">
            <button
              type="button"
              disabled={busy}
              onClick={() => setReviseOpen((v) => !v)}
              className="rounded-full border border-[var(--border)] px-3 py-1.5 text-xs font-semibold text-[var(--link)] hover:bg-[var(--accent-soft)] disabled:opacity-40"
            >
              Revise plan
            </button>
            <button
              type="button"
              disabled={busy}
              onClick={() => void handleComplete()}
              className="rounded-full border border-[var(--border)] px-3 py-1.5 text-xs font-semibold disabled:opacity-40"
            >
              Complete
            </button>
            <button
              type="button"
              disabled={busy}
              onClick={() => void handleAbandon()}
              className="rounded-full border border-[var(--border)] px-3 py-1.5 text-xs font-semibold text-red-700 disabled:opacity-40"
            >
              Abandon
            </button>
          </div>
        </div>

        <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
          <ProgressStat label="Starting debt" value={formatCurrencyDetailed(progress.startingDebt)} />
          <ProgressStat label="Current debt" value={formatCurrencyDetailed(progress.currentDebt)} />
          <ProgressStat label="Paid to date" value={formatCurrencyDetailed(progress.paidToDate)} />
          <ProgressStat label="Debt reduced" value={formatCurrencyDetailed(progress.debtReduced)} />
          <ProgressStat
            label="Months remaining"
            value={
              progress.projectionIsValid
                ? String(progress.projectedMonthsRemaining)
                : "—"
            }
          />
          <ProgressStat
            label="Projected interest left"
            value={
              progress.projectionIsValid
                ? formatCurrencyDetailed(progress.projectedRemainingInterest)
                : "—"
            }
          />
          <ProgressStat
            label="Projected payoff"
            value={formatDate(progress.projectedPayoffDate)}
          />
          <ProgressStat
            label="Avg monthly paid"
            value={formatCurrencyDetailed(progress.averageMonthlyPaid)}
          />
        </div>
        {progress.adherenceNote ? (
          <p className="text-sm text-[var(--muted)]">{progress.adherenceNote}</p>
        ) : null}
      </section>

      {reviseOpen ? (
        <section className="rounded-xl border border-[var(--border)] px-4 py-4">
          <h3 className="text-sm font-semibold">Revise plan settings</h3>
          <p className="mt-1 text-xs text-[var(--muted)]">
            Saves a new version. Payment history is kept.
          </p>
          <form onSubmit={handleRevise} className="mt-3 grid gap-3 sm:grid-cols-2">
            <label className="block sm:col-span-2">
              <span className="mb-1.5 block text-xs font-medium text-[var(--muted)]">
                Name
              </span>
              <input
                value={reviseName}
                onChange={(e) => setReviseName(e.target.value)}
                className="w-full rounded-xl border border-[var(--border)] bg-white px-3 py-2 text-sm"
              />
            </label>
            <label className="block">
              <span className="mb-1.5 block text-xs font-medium text-[var(--muted)]">
                Strategy
              </span>
              <select
                value={reviseStrategy}
                onChange={(e) => setReviseStrategy(e.target.value)}
                className="w-full rounded-xl border border-[var(--border)] bg-white px-3 py-2 text-sm"
              >
                <option value="Avalanche">Avalanche</option>
                <option value="Snowball">Snowball</option>
                <option value="MinimumsOnly">Minimums only</option>
              </select>
            </label>
            <label className="block">
              <span className="mb-1.5 block text-xs font-medium text-[var(--muted)]">
                Monthly debt payment
              </span>
              <input
                type="text"
                inputMode="decimal"
                value={reviseMonthly}
                onChange={(e) => setReviseMonthly(sanitizeMoneyInput(e.target.value))}
                className="w-full rounded-xl border border-[var(--border)] bg-white px-3 py-2 text-sm"
              />
            </label>
            <label className="block">
              <span className="mb-1.5 block text-xs font-medium text-[var(--muted)]">
                Extra monthly payment
              </span>
              <input
                type="text"
                inputMode="decimal"
                value={reviseExtra}
                onChange={(e) => setReviseExtra(sanitizeMoneyInput(e.target.value))}
                className="w-full rounded-xl border border-[var(--border)] bg-white px-3 py-2 text-sm"
              />
            </label>
            <label className="block sm:col-span-2">
              <span className="mb-1.5 block text-xs font-medium text-[var(--muted)]">
                Reason (optional)
              </span>
              <input
                value={reviseReason}
                onChange={(e) => setReviseReason(e.target.value)}
                className="w-full rounded-xl border border-[var(--border)] bg-white px-3 py-2 text-sm"
                placeholder="e.g. Got a raise"
              />
            </label>
            <div className="flex flex-wrap gap-2 sm:col-span-2">
              <button
                type="submit"
                disabled={busy}
                className="rounded-full bg-[var(--link)] px-4 py-2 text-sm font-semibold text-white disabled:opacity-40"
              >
                Save revision
              </button>
              <button
                type="button"
                disabled={busy}
                onClick={() => setReviseOpen(false)}
                className="text-sm font-medium text-[var(--muted)] hover:underline"
              >
                Cancel
              </button>
            </div>
          </form>
        </section>
      ) : null}

      <section className="rounded-xl border border-[var(--border)] px-4 py-4">
        <h3 className="text-sm font-semibold">Record payment</h3>
        <p className="mt-1 text-xs text-[var(--muted)]">
          Updates the selected card balance immediately.
        </p>
        <form onSubmit={handleRecordPayment} className="mt-3 grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
          <label className="block sm:col-span-2">
            <span className="mb-1.5 block text-xs font-medium text-[var(--muted)]">
              Credit card
            </span>
            <select
              value={paymentAccountId}
              onChange={(e) => setPaymentAccountId(e.target.value)}
              className="w-full rounded-xl border border-[var(--border)] bg-white px-3 py-2 text-sm"
            >
              <option value="">Select card…</option>
              {includedCards.map((card) => (
                <option key={card.accountId} value={card.accountId}>
                  {card.name} ({formatCurrencyDetailed(card.balance)})
                </option>
              ))}
            </select>
          </label>
          <label className="block">
            <span className="mb-1.5 block text-xs font-medium text-[var(--muted)]">
              Amount
            </span>
            <input
              type="text"
              inputMode="decimal"
              value={paymentAmount}
              onChange={(e) => setPaymentAmount(sanitizeMoneyInput(e.target.value))}
              className="w-full rounded-xl border border-[var(--border)] bg-white px-3 py-2 text-sm"
            />
          </label>
          <label className="block">
            <span className="mb-1.5 block text-xs font-medium text-[var(--muted)]">
              Date
            </span>
            <input
              type="date"
              value={paymentDate}
              onChange={(e) => setPaymentDate(e.target.value)}
              className="w-full rounded-xl border border-[var(--border)] bg-white px-3 py-2 text-sm"
            />
          </label>
          <label className="block sm:col-span-2 lg:col-span-3">
            <span className="mb-1.5 block text-xs font-medium text-[var(--muted)]">
              Notes
            </span>
            <input
              value={paymentNotes}
              onChange={(e) => setPaymentNotes(e.target.value)}
              className="w-full rounded-xl border border-[var(--border)] bg-white px-3 py-2 text-sm"
            />
          </label>
          <div className="flex items-end">
            <button
              type="submit"
              disabled={busy}
              className="rounded-full bg-[var(--link)] px-4 py-2 text-sm font-semibold text-white disabled:opacity-40"
            >
              Record payment
            </button>
          </div>
        </form>
      </section>

      <section className="space-y-3">
        <h3 className="text-sm font-semibold">History</h3>
        {!history || history.events.length === 0 ? (
          <p className="text-sm text-[var(--muted)]">No history yet.</p>
        ) : (
          <ol className="space-y-2 border-l border-[var(--border)] pl-4">
            {history.events.map((event) => (
              <li key={event.payoffPlanEventId} className="relative">
                <span className="absolute -left-[1.3rem] top-1.5 h-2 w-2 rounded-full bg-[var(--link)]" />
                <p className="text-xs font-semibold uppercase tracking-wide text-[var(--muted)]">
                  {eventLabel(event.eventType)} · {formatDate(event.createdOnUtc)}
                </p>
                <p className="text-sm">{event.summary}</p>
              </li>
            ))}
          </ol>
        )}

        {history && history.payments.some((p) => !p.isVoided) ? (
          <div className="mt-4">
            <h4 className="text-xs font-semibold uppercase tracking-wide text-[var(--muted)]">
              Payments
            </h4>
            <ul className="mt-2 space-y-2">
              {history.payments
                .filter((p) => !p.isVoided)
                .map((payment) => (
                  <li
                    key={payment.payoffPlanPaymentId}
                    className="flex flex-wrap items-center justify-between gap-2 text-sm"
                  >
                    <span>
                      {formatDate(payment.paymentDate)} ·{" "}
                      {payment.accountName ?? `Card #${payment.accountId}`} ·{" "}
                      {formatCurrencyDetailed(payment.amount)}
                    </span>
                    <button
                      type="button"
                      disabled={busy}
                      onClick={() => void handleVoidPayment(payment.payoffPlanPaymentId)}
                      className="text-xs font-medium text-red-700 hover:underline disabled:opacity-40"
                    >
                      Void
                    </button>
                  </li>
                ))}
            </ul>
          </div>
        ) : null}
      </section>

      <p className="text-sm">
        <Link href="/credit-cards/payoff" className="font-medium text-[var(--link)] hover:underline">
          Back to Payoff Calculator
        </Link>
      </p>
    </div>
  );
}

function ProgressStat({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-xl border border-[var(--border)] px-3 py-3">
      <p className="text-xs font-medium text-[var(--muted)]">{label}</p>
      <p className="mt-1 text-sm font-semibold">{value}</p>
    </div>
  );
}
