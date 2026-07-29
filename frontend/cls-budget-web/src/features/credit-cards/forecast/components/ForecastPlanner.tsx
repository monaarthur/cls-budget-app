"use client";

import { useMemo, useState, type FormEvent } from "react";
import { AppLink as Link } from "@/components/AppLink";
import { Card } from "@/components/ui/Card";
import { forecastApi } from "@/features/credit-cards/forecast/api/forecastApi";
import type {
  CalculationEnvelope,
  ForecastResult,
} from "@/features/credit-cards/forecast/types";
import { ApiError } from "@/lib/api/client";
import { formatCurrencyDetailed } from "@/lib/format";

const DISCLAIMER =
  "The calculations and recommendations provided by this application are estimates for educational and planning purposes only. They are not financial, legal, tax, or credit advice. Actual interest charges, credit-score effects, fees, and payoff dates may differ based on lender rules, transaction timing, and account activity.";

function formatMonth(iso: string): string {
  const date = new Date(`${iso}T00:00:00.000Z`);
  if (!Number.isFinite(date.getTime())) return iso;
  return date.toLocaleDateString("en-US", {
    month: "short",
    year: "numeric",
    timeZone: "UTC",
  });
}

export function ForecastPlanner() {
  const [strategy, setStrategy] = useState("Avalanche");
  const [monthlyPayment, setMonthlyPayment] = useState("400");
  const [forecastMonths, setForecastMonths] = useState("36");
  const [monthlyIncome, setMonthlyIncome] = useState("5000");
  const [monthlyExpenses, setMonthlyExpenses] = useState("3000");
  const [saveName, setSaveName] = useState("");
  const [saveScenario, setSaveScenario] = useState(false);
  const [running, setRunning] = useState(false);
  const [status, setStatus] = useState<string | null>(null);
  const [envelope, setEnvelope] = useState<CalculationEnvelope<ForecastResult> | null>(
    null,
  );

  const previewMonths = useMemo(() => {
    const months = envelope?.result.months ?? [];
    if (months.length <= 12) return months;
    return months.filter((_, index) => index % Math.ceil(months.length / 12) === 0);
  }, [envelope]);

  async function handleGenerate(event: FormEvent) {
    event.preventDefault();
    const payment = Number(monthlyPayment);
    const months = Number(forecastMonths);
    const income = Number(monthlyIncome);
    const expenses = Number(monthlyExpenses);

    if (![payment, months, income, expenses].every((n) => Number.isFinite(n))) {
      setStatus("Enter valid numbers for payment, months, income, and expenses.");
      return;
    }
    if (payment <= 0 || months < 1 || months > 1200) {
      setStatus("Payment must be > 0 and forecast months between 1 and 1200.");
      return;
    }
    if (saveScenario && saveName.trim() === "") {
      setStatus("Enter a name to save this forecast scenario.");
      return;
    }

    setRunning(true);
    setStatus(null);
    try {
      const result = await forecastApi.create({
        strategy,
        totalMonthlyDebtPayment: payment,
        forecastMonths: months,
        monthlyNetIncome: income,
        monthlyExpenses: expenses,
        save: saveScenario,
        name: saveScenario ? saveName.trim() : null,
      });
      setEnvelope(result);
    } catch (err) {
      const message =
        err instanceof ApiError
          ? err.errors.join(", ") || err.message
          : err instanceof Error
            ? err.message
            : "Failed to generate forecast";
      setStatus(message);
      setEnvelope(null);
    } finally {
      setRunning(false);
    }
  }

  const result = envelope?.result;

  return (
    <div className="w-full space-y-4 pb-10 pt-2">
      <p className="text-sm text-[var(--muted)]">
        Project debt, interest, utilization, and available cash over time using
        your{" "}
        <Link
          href="/credit-cards/payoff"
          className="font-medium text-[var(--link)] hover:underline"
        >
          payoff strategy
        </Link>
        .
      </p>

      <Card className="p-5">
        <form className="space-y-4" onSubmit={(event) => void handleGenerate(event)}>
          <div className="grid gap-4 sm:grid-cols-2">
            <label className="block text-sm">
              <span className="mb-1.5 block font-medium">Strategy</span>
              <select
                value={strategy}
                onChange={(event) => setStrategy(event.target.value)}
                disabled={running}
                className="w-full rounded-xl border border-[var(--border)] bg-white px-3 py-2.5 text-sm"
              >
                <option value="Avalanche">Avalanche</option>
                <option value="Snowball">Snowball</option>
                <option value="MinimumsOnly">Minimums only</option>
              </select>
            </label>
            <label className="block text-sm">
              <span className="mb-1.5 block font-medium">Monthly debt payment</span>
              <input
                type="number"
                step="any"
                value={monthlyPayment}
                onChange={(event) => setMonthlyPayment(event.target.value)}
                disabled={running}
                className="w-full rounded-xl border border-[var(--border)] bg-white px-3 py-2.5 text-sm"
              />
            </label>
            <label className="block text-sm">
              <span className="mb-1.5 block font-medium">Forecast months</span>
              <input
                type="number"
                min={1}
                max={1200}
                value={forecastMonths}
                onChange={(event) => setForecastMonths(event.target.value)}
                disabled={running}
                className="w-full rounded-xl border border-[var(--border)] bg-white px-3 py-2.5 text-sm"
              />
            </label>
            <label className="block text-sm">
              <span className="mb-1.5 block font-medium">Monthly net income</span>
              <input
                type="number"
                step="any"
                value={monthlyIncome}
                onChange={(event) => setMonthlyIncome(event.target.value)}
                disabled={running}
                className="w-full rounded-xl border border-[var(--border)] bg-white px-3 py-2.5 text-sm"
              />
            </label>
            <label className="block text-sm">
              <span className="mb-1.5 block font-medium">Monthly expenses</span>
              <input
                type="number"
                step="any"
                value={monthlyExpenses}
                onChange={(event) => setMonthlyExpenses(event.target.value)}
                disabled={running}
                className="w-full rounded-xl border border-[var(--border)] bg-white px-3 py-2.5 text-sm"
              />
            </label>
          </div>

          <label className="flex items-start gap-2 text-sm">
            <input
              type="checkbox"
              checked={saveScenario}
              onChange={(event) => setSaveScenario(event.target.checked)}
              disabled={running}
              className="mt-1"
            />
            <span>
              <span className="font-medium">Save this scenario</span>
              <span className="mt-0.5 block text-xs text-[var(--muted)]">
                Persists monthly snapshots so you can reload them later.
              </span>
            </span>
          </label>

          {saveScenario ? (
            <label className="block text-sm">
              <span className="mb-1.5 block font-medium">Scenario name</span>
              <input
                type="text"
                value={saveName}
                onChange={(event) => setSaveName(event.target.value)}
                disabled={running}
                className="w-full max-w-md rounded-xl border border-[var(--border)] bg-white px-3 py-2.5 text-sm"
              />
            </label>
          ) : null}

          <button
            type="submit"
            disabled={running}
            className="rounded-full bg-[var(--link)] px-4 py-2 text-sm font-semibold text-white disabled:opacity-40"
          >
            {running ? "Generating…" : "Generate forecast"}
          </button>
        </form>

        {status ? (
          <p className="mt-4 rounded-xl border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-900">
            {status}
          </p>
        ) : null}
      </Card>

      {result ? (
        <Card className="space-y-4 p-5">
          <div>
            <p className="text-lg font-semibold">
              {result.strategy} · {formatCurrencyDetailed(result.startingDebt)} starting debt
            </p>
            <p className="mt-1 text-sm text-[var(--muted)]">
              Total interest {formatCurrencyDetailed(result.totalInterestPaid)}
              {result.estimatedDebtFreeDate
                ? ` · Debt-free ${formatMonth(result.estimatedDebtFreeDate)}`
                : ""}
              {result.forecastId != null
                ? ` · Saved as #${result.forecastId}${result.name ? ` (${result.name})` : ""}`
                : ""}
            </p>
          </div>

          {envelope?.warnings?.length ? (
            <ul className="list-disc space-y-1 pl-5 text-sm text-amber-800">
              {envelope.warnings.map((warning) => (
                <li key={warning}>{warning}</li>
              ))}
            </ul>
          ) : null}

          <div className="overflow-x-auto">
            <table className="min-w-full text-left text-sm">
              <thead className="text-xs text-[var(--muted)]">
                <tr>
                  <th className="px-2 py-1.5 font-medium">Month</th>
                  <th className="px-2 py-1.5 font-medium">Start</th>
                  <th className="px-2 py-1.5 font-medium">Interest</th>
                  <th className="px-2 py-1.5 font-medium">Payments</th>
                  <th className="px-2 py-1.5 font-medium">End</th>
                  <th className="px-2 py-1.5 font-medium">Util %</th>
                  <th className="px-2 py-1.5 font-medium">Cash</th>
                </tr>
              </thead>
              <tbody>
                {previewMonths.map((month) => (
                  <tr key={`${month.monthIndex}-${month.month}`} className="border-t border-[var(--border)]">
                    <td className="px-2 py-1.5">{formatMonth(month.month)}</td>
                    <td className="px-2 py-1.5">{formatCurrencyDetailed(month.startingDebt)}</td>
                    <td className="px-2 py-1.5">{formatCurrencyDetailed(month.interest)}</td>
                    <td className="px-2 py-1.5">{formatCurrencyDetailed(month.payments)}</td>
                    <td className="px-2 py-1.5">{formatCurrencyDetailed(month.endingDebt)}</td>
                    <td className="px-2 py-1.5">
                      {month.overallUtilizationPercentage.toFixed(1)}%
                    </td>
                    <td className="px-2 py-1.5">{formatCurrencyDetailed(month.availableCash)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
            {(envelope?.result.months.length ?? 0) > previewMonths.length ? (
              <p className="mt-2 text-xs text-[var(--muted)]">
                Showing a sample of {previewMonths.length} of{" "}
                {envelope?.result.months.length} months.
              </p>
            ) : null}
          </div>
        </Card>
      ) : null}

      <p className="text-xs leading-relaxed text-[var(--muted)]">{DISCLAIMER}</p>
    </div>
  );
}
