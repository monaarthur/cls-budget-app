"use client";

import { useState, type FormEvent } from "react";
import Link from "next/link";
import { Card } from "@/components/ui/Card";
import { balanceTransferApi } from "@/features/credit-cards/balance-transfer/api/balanceTransferApi";
import type {
  BalanceTransferAnalysisResult,
  CalculationEnvelope,
} from "@/features/credit-cards/balance-transfer/types";
import { ApiError } from "@/lib/api/client";
import {
  formatCurrencyDetailed,
  parseMoneyInput,
  sanitizeMoneyInput,
} from "@/lib/format";

const DISCLAIMER =
  "The calculations and recommendations provided by this application are estimates for educational and planning purposes only. They are not financial, legal, tax, or credit advice. Actual interest charges, credit-score effects, fees, and payoff dates may differ based on lender rules, transaction timing, and account activity.";

function recommendationTone(recommendation: string): string {
  switch (recommendation) {
    case "Recommended":
      return "text-emerald-800";
    case "PotentiallyBeneficial":
      return "text-amber-800";
    case "NotRecommended":
      return "text-red-800";
    default:
      return "text-[var(--foreground)]";
  }
}

function recommendationLabel(recommendation: string): string {
  switch (recommendation) {
    case "PotentiallyBeneficial":
      return "Potentially beneficial";
    case "NotRecommended":
      return "Not recommended";
    case "InsufficientInformation":
      return "Insufficient information";
    default:
      return recommendation;
  }
}

export function BalanceTransferAnalyzer() {
  const [transferAmount, setTransferAmount] = useState("5000");
  const [currentApr, setCurrentApr] = useState("24");
  const [promoApr, setPromoApr] = useState("0");
  const [promoMonths, setPromoMonths] = useState("12");
  const [feePercent, setFeePercent] = useState("3");
  const [feeFlat, setFeeFlat] = useState("0");
  const [newRegularApr, setNewRegularApr] = useState("22");
  const [monthlyPayment, setMonthlyPayment] = useState("450");
  const [transferLimit, setTransferLimit] = useState("10000");
  const [analyzing, setAnalyzing] = useState(false);
  const [status, setStatus] = useState<string | null>(null);
  const [envelope, setEnvelope] = useState<CalculationEnvelope<
    BalanceTransferAnalysisResult
  > | null>(null);

  async function handleAnalyze(event: FormEvent) {
    event.preventDefault();

    const body = {
      transferAmount: parseMoneyInput(transferAmount),
      currentAnnualPercentageRate: parseMoneyInput(currentApr),
      promotionalAnnualPercentageRate: parseMoneyInput(promoApr),
      promotionalPeriodMonths: parseMoneyInput(promoMonths),
      transferFeePercentage: parseMoneyInput(feePercent),
      transferFeeFlatAmount: parseMoneyInput(feeFlat) ?? 0,
      newRegularAnnualPercentageRate: parseMoneyInput(newRegularApr),
      plannedMonthlyPayment: parseMoneyInput(monthlyPayment),
      availableTransferLimit: parseMoneyInput(transferLimit),
    };

    if (
      body.transferAmount === null ||
      body.currentAnnualPercentageRate === null ||
      body.promotionalAnnualPercentageRate === null ||
      body.promotionalPeriodMonths === null ||
      body.transferFeePercentage === null ||
      body.newRegularAnnualPercentageRate === null ||
      body.plannedMonthlyPayment === null ||
      body.availableTransferLimit === null
    ) {
      setStatus("Enter valid numbers for every field.");
      return;
    }

    setAnalyzing(true);
    setStatus(null);
    try {
      const result = await balanceTransferApi.analyze({
        transferAmount: body.transferAmount,
        currentAnnualPercentageRate: body.currentAnnualPercentageRate,
        promotionalAnnualPercentageRate: body.promotionalAnnualPercentageRate,
        promotionalPeriodMonths: body.promotionalPeriodMonths,
        transferFeePercentage: body.transferFeePercentage,
        transferFeeFlatAmount: body.transferFeeFlatAmount,
        newRegularAnnualPercentageRate: body.newRegularAnnualPercentageRate,
        plannedMonthlyPayment: body.plannedMonthlyPayment,
        availableTransferLimit: body.availableTransferLimit,
      });
      setEnvelope(result);
    } catch (err) {
      const message =
        err instanceof ApiError
          ? err.errors.join(", ") || err.message
          : err instanceof Error
            ? err.message
            : "Failed to analyze balance transfer";
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
        Compare keeping a balance at your current APR versus moving it to a
        promotional balance-transfer offer.{" "}
        <Link
          href="/credit-cards/payoff"
          className="font-medium text-[var(--link)] hover:underline"
        >
          Payoff calculator
        </Link>
      </p>

      <Card className="p-5">
        <form className="space-y-4" onSubmit={(event) => void handleAnalyze(event)}>
          <div className="grid gap-4 sm:grid-cols-2">
            <Field
              label="Transfer amount"
              value={transferAmount}
              onChange={setTransferAmount}
              disabled={analyzing}
            />
            <Field
              label="Available transfer limit"
              value={transferLimit}
              onChange={setTransferLimit}
              disabled={analyzing}
            />
            <Field
              label="Current APR %"
              value={currentApr}
              onChange={setCurrentApr}
              disabled={analyzing}
            />
            <Field
              label="Promotional APR %"
              value={promoApr}
              onChange={setPromoApr}
              disabled={analyzing}
            />
            <Field
              label="Promo period (months)"
              value={promoMonths}
              onChange={setPromoMonths}
              disabled={analyzing}
            />
            <Field
              label="New regular APR % (after promo)"
              value={newRegularApr}
              onChange={setNewRegularApr}
              disabled={analyzing}
            />
            <Field
              label="Transfer fee %"
              value={feePercent}
              onChange={setFeePercent}
              disabled={analyzing}
            />
            <Field
              label="Transfer fee flat $"
              value={feeFlat}
              onChange={setFeeFlat}
              disabled={analyzing}
            />
            <Field
              label="Planned monthly payment"
              value={monthlyPayment}
              onChange={setMonthlyPayment}
              disabled={analyzing}
            />
          </div>

          <button
            type="submit"
            disabled={analyzing}
            className="rounded-full bg-[var(--link)] px-4 py-2 text-sm font-semibold text-white disabled:opacity-40"
          >
            {analyzing ? "Analyzing…" : "Analyze transfer"}
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
          <p className={`text-lg font-semibold ${recommendationTone(result.recommendation)}`}>
            {recommendationLabel(result.recommendation)}
          </p>
          <p className="text-sm text-[var(--muted)]">{result.explanation}</p>

          <dl className="grid gap-3 sm:grid-cols-2">
            <Metric label="Transfer fee" value={formatCurrencyDetailed(result.totalTransferFee)} />
            <Metric
              label="Starting balance with transfer"
              value={formatCurrencyDetailed(result.startingBalanceWithTransfer)}
            />
            <Metric
              label="Interest without transfer"
              value={formatCurrencyDetailed(result.interestWithoutTransfer)}
            />
            <Metric
              label="Interest with transfer"
              value={formatCurrencyDetailed(result.interestWithTransfer)}
            />
            <Metric label="Net savings" value={formatCurrencyDetailed(result.netSavings)} />
            <Metric
              label="Break-even month"
              value={result.breakEvenMonth != null ? String(result.breakEvenMonth) : "—"}
            />
            <Metric
              label="Balance when promo ends"
              value={formatCurrencyDetailed(result.balanceRemainingWhenPromotionEnds)}
            />
            <Metric
              label="Payment to clear in promo"
              value={formatCurrencyDetailed(result.paymentNeededToClearBeforePromotionEnds)}
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
}: {
  label: string;
  value: string;
  onChange: (value: string) => void;
  disabled: boolean;
}) {
  return (
    <label className="block text-sm">
      <span className="mb-1.5 block font-medium">{label}</span>
      <input
        type="text"
        inputMode="decimal"
        value={value}
        onChange={(event) => onChange(sanitizeMoneyInput(event.target.value))}
        disabled={disabled}
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
