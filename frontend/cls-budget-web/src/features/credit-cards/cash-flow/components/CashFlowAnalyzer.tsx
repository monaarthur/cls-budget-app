"use client";

import { useState, type FormEvent } from "react";
import Link from "next/link";
import { Card } from "@/components/ui/Card";
import { cashFlowApi } from "@/features/credit-cards/cash-flow/api/cashFlowApi";
import type {
  CalculationEnvelope,
  CashFlowAnalysisResult,
} from "@/features/credit-cards/cash-flow/types";
import { ApiError } from "@/lib/api/client";
import { formatCurrencyDetailed } from "@/lib/format";

const DISCLAIMER =
  "The calculations and recommendations provided by this application are estimates for educational and planning purposes only. They are not financial, legal, tax, or credit advice. Actual interest charges, credit-score effects, fees, and payoff dates may differ based on lender rules, transaction timing, and account activity.";

export function CashFlowAnalyzer() {
  const [monthlyNetIncome, setMonthlyNetIncome] = useState("5000");
  const [requiredExpenses, setRequiredExpenses] = useState("2000");
  const [variableExpenses, setVariableExpenses] = useState("500");
  const [debtMinimums, setDebtMinimums] = useState("");
  const [savings, setSavings] = useState("200");
  const [safetyBuffer, setSafetyBuffer] = useState("300");
  const [additionalFunds, setAdditionalFunds] = useState("0");
  const [overrideExtra, setOverrideExtra] = useState("");
  const [analyzing, setAnalyzing] = useState(false);
  const [status, setStatus] = useState<string | null>(null);
  const [envelope, setEnvelope] = useState<CalculationEnvelope<
    CashFlowAnalysisResult
  > | null>(null);

  async function handleAnalyze(event: FormEvent) {
    event.preventDefault();

    const required = {
      monthlyNetIncome: Number(monthlyNetIncome),
      requiredExpenses: Number(requiredExpenses),
      variableExpenses: Number(variableExpenses),
      emergencySavingsContribution: Number(savings),
      safetyBuffer: Number(safetyBuffer),
      additionalAvailableFunds: Number(additionalFunds || "0"),
    };

    if (Object.values(required).some((value) => !Number.isFinite(value) || value < 0)) {
      setStatus("Enter valid non-negative numbers for income, expenses, savings, and buffer.");
      return;
    }

    let existingDebtMinimums: number | null = null;
    if (debtMinimums.trim() !== "") {
      const mins = Number(debtMinimums);
      if (!Number.isFinite(mins) || mins < 0) {
        setStatus("Debt minimums must be a non-negative number, or blank to use card minimums.");
        return;
      }
      existingDebtMinimums = mins;
    }

    let userOverrideExtraPayment: number | null = null;
    if (overrideExtra.trim() !== "") {
      const override = Number(overrideExtra);
      if (!Number.isFinite(override) || override < 0) {
        setStatus("Override extra payment must be a non-negative number, or blank.");
        return;
      }
      userOverrideExtraPayment = override;
    }

    setAnalyzing(true);
    setStatus(null);
    try {
      const result = await cashFlowApi.analyze({
        ...required,
        existingDebtMinimums,
        userOverrideExtraPayment,
      });
      setEnvelope(result);
    } catch (err) {
      const message =
        err instanceof ApiError
          ? err.errors.join(", ") || err.message
          : err instanceof Error
            ? err.message
            : "Failed to analyze cash flow";
      setStatus(message);
      setEnvelope(null);
    } finally {
      setAnalyzing(false);
    }
  }

  const result = envelope?.result;

  return (
    <div className="w-full space-y-4 pb-10 pt-2">
      <p className="text-sm text-[var(--muted)]">
        Estimate how much you can safely put toward debt after bills and a cash
        buffer. Use the suggested total on the{" "}
        <Link
          href="/credit-cards/payoff"
          className="font-medium text-[var(--link)] hover:underline"
        >
          payoff calculator
        </Link>
        .
      </p>

      <Card className="p-5">
        <form className="space-y-4" onSubmit={(event) => void handleAnalyze(event)}>
          <div className="grid gap-4 sm:grid-cols-2">
            <Field
              label="Monthly net income"
              value={monthlyNetIncome}
              onChange={setMonthlyNetIncome}
              disabled={analyzing}
            />
            <Field
              label="Required expenses"
              value={requiredExpenses}
              onChange={setRequiredExpenses}
              disabled={analyzing}
            />
            <Field
              label="Variable expenses"
              value={variableExpenses}
              onChange={setVariableExpenses}
              disabled={analyzing}
            />
            <Field
              label="Debt minimums (blank = from cards)"
              value={debtMinimums}
              onChange={setDebtMinimums}
              disabled={analyzing}
              placeholder="Auto from credit cards"
            />
            <Field
              label="Emergency savings contribution"
              value={savings}
              onChange={setSavings}
              disabled={analyzing}
            />
            <Field
              label="Safety buffer"
              value={safetyBuffer}
              onChange={setSafetyBuffer}
              disabled={analyzing}
            />
            <Field
              label="Additional available funds"
              value={additionalFunds}
              onChange={setAdditionalFunds}
              disabled={analyzing}
            />
            <Field
              label="Override extra payment (optional)"
              value={overrideExtra}
              onChange={setOverrideExtra}
              disabled={analyzing}
              placeholder="Leave blank to use safe amount"
            />
          </div>

          <button
            type="submit"
            disabled={analyzing}
            className="rounded-full bg-[var(--link)] px-4 py-2 text-sm font-semibold text-white disabled:opacity-40"
          >
            {analyzing ? "Analyzing…" : "Analyze cash flow"}
          </button>
        </form>

        {status ? (
          <p className="mt-4 rounded-xl border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-900">
            {status}
          </p>
        ) : null}
      </Card>

      {result ? (
        <Card className="space-y-3 p-5">
          <p className="text-lg font-semibold text-[var(--foreground)]">
            Recommended extra:{" "}
            {formatCurrencyDetailed(result.recommendedExtraDebtPayment)}
            {result.usedUserOverride ? " (override)" : " (safe)"}
          </p>
          <p className="text-sm text-[var(--muted)]">
            Suggested total monthly debt payment{" "}
            <span className="font-medium text-[var(--foreground)]">
              {formatCurrencyDetailed(result.suggestedTotalMonthlyDebtPayment)}
            </span>{" "}
            (minimums + recommended extra)
          </p>

          <dl className="grid gap-3 sm:grid-cols-2">
            <Metric
              label="Disposable income"
              value={formatCurrencyDetailed(result.monthlyDisposableIncome)}
            />
            <Metric
              label="Required debt minimums"
              value={formatCurrencyDetailed(result.requiredDebtMinimums)}
            />
            <Metric
              label="Safe extra payment"
              value={formatCurrencyDetailed(result.safeExtraDebtPayment)}
            />
            <Metric
              label="Aggressive extra payment"
              value={formatCurrencyDetailed(result.aggressiveExtraDebtPayment)}
            />
            <Metric
              label="Remaining cash buffer"
              value={formatCurrencyDetailed(result.remainingCashBuffer)}
            />
          </dl>

          {envelope?.warnings?.length ? (
            <ul className="list-disc space-y-1 pl-5 text-sm text-amber-800">
              {envelope.warnings.map((warning) => (
                <li key={warning}>{warning}</li>
              ))}
            </ul>
          ) : null}
        </Card>
      ) : null}

      <p className="text-xs leading-relaxed text-[var(--muted)]">{DISCLAIMER}</p>
    </div>
  );
}

function Field({
  label,
  value,
  onChange,
  disabled,
  placeholder,
}: {
  label: string;
  value: string;
  onChange: (value: string) => void;
  disabled: boolean;
  placeholder?: string;
}) {
  return (
    <label className="block text-sm">
      <span className="mb-1.5 block font-medium">{label}</span>
      <input
        type="number"
        step="any"
        value={value}
        onChange={(event) => onChange(event.target.value)}
        disabled={disabled}
        placeholder={placeholder}
        className="w-full rounded-xl border border-[var(--border)] bg-white px-3 py-2.5 text-sm"
      />
    </label>
  );
}

function Metric({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <dt className="text-xs text-[var(--muted)]">{label}</dt>
      <dd className="text-sm font-medium text-[var(--foreground)]">{value}</dd>
    </div>
  );
}
