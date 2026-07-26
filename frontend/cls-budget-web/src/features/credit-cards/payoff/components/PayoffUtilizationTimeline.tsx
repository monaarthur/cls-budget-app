"use client";

import { useMemo } from "react";
import type { CardPayoffOrder } from "@/features/credit-cards/payoff/types";
import { formatCurrencyDetailed } from "@/lib/format";

const UTILIZATION_MARKER_PERCENT = 30;

type UtilizationHit = {
  creditCardId: number;
  name: string;
  month: string;
  monthIndex: number;
  endingBalance: number;
  creditLimit: number;
  alreadyAtOrBelow: boolean;
  /** Portfolio utilization (all cards) at this milestone month. */
  overallUtilizationPercent: number;
};

function formatMonthLabel(iso: string): string {
  const date = new Date(`${iso}T00:00:00.000Z`);
  if (!Number.isFinite(date.getTime())) return iso;
  return date.toLocaleDateString("en-US", {
    month: "short",
    year: "2-digit",
    timeZone: "UTC",
  });
}

function buildOverallUtilizationByMonth(
  cardOrder: CardPayoffOrder[],
  portfolio: {
    /** Same total credit limits as the Utilization summary card. */
    totalCreditLimits: number;
    /**
     * Balances for cards not in this payoff plan (excluded / $0-balance cards).
     * Held constant while plan cards pay down.
     */
    outsidePlanBalance: number;
  },
): { byMonthIndex: Map<number, number>; initialPercent: number } {
  const totalLimit = portfolio.totalCreditLimits;
  const planInitialBalance = cardOrder.reduce((sum, card) => {
    const first = card.monthlyBalances?.[0];
    return sum + (first?.startingBalance ?? 0);
  }, 0);
  const initialBalance = planInitialBalance + portfolio.outsidePlanBalance;
  const initialPercent =
    totalLimit > 0 ? (initialBalance / totalLimit) * 100 : 0;

  const maxMonthIndex = cardOrder.reduce((max, card) => {
    const len = card.monthlyBalances?.length ?? 0;
    return Math.max(max, len - 1);
  }, -1);

  const byMonthIndex = new Map<number, number>();
  if (totalLimit <= 0 || maxMonthIndex < 0) {
    return { byMonthIndex, initialPercent };
  }

  for (let monthIndex = 0; monthIndex <= maxMonthIndex; monthIndex++) {
    let planBalance = 0;
    for (const card of cardOrder) {
      const rows = card.monthlyBalances ?? [];
      if (monthIndex < rows.length) {
        planBalance += rows[monthIndex].endingBalance;
      }
      // Paid-off cards with no further rows contribute $0.
    }
    const totalBalance = planBalance + portfolio.outsidePlanBalance;
    byMonthIndex.set(monthIndex, (totalBalance / totalLimit) * 100);
  }

  return { byMonthIndex, initialPercent };
}

function findUtilizationHits(
  cardOrder: CardPayoffOrder[],
  limitsByCardId: Map<number, number>,
  targetPercent: number,
  portfolio: {
    totalCreditLimits: number;
    outsidePlanBalance: number;
  },
): UtilizationHit[] {
  const { byMonthIndex: overallByMonth, initialPercent } =
    buildOverallUtilizationByMonth(cardOrder, portfolio);
  const hits: UtilizationHit[] = [];

  for (const card of cardOrder) {
    const limit = limitsByCardId.get(card.creditCardId) ?? 0;
    if (limit <= 0) continue;

    const targetBalance = (limit * targetPercent) / 100;
    const rows = card.monthlyBalances ?? [];
    if (rows.length === 0) continue;

    const first = rows[0];
    if (first.startingBalance <= targetBalance + 0.005) {
      hits.push({
        creditCardId: card.creditCardId,
        name: card.name,
        month: first.month,
        monthIndex: 0,
        endingBalance: first.startingBalance,
        creditLimit: limit,
        alreadyAtOrBelow: true,
        overallUtilizationPercent: initialPercent,
      });
      continue;
    }

    for (let i = 0; i < rows.length; i++) {
      const row = rows[i];
      if (row.endingBalance <= targetBalance + 0.005) {
        hits.push({
          creditCardId: card.creditCardId,
          name: card.name,
          month: row.month,
          monthIndex: i,
          endingBalance: row.endingBalance,
          creditLimit: limit,
          alreadyAtOrBelow: false,
          overallUtilizationPercent: overallByMonth.get(i) ?? 0,
        });
        break;
      }
    }
  }

  return hits.sort(
    (a, b) => a.monthIndex - b.monthIndex || a.name.localeCompare(b.name),
  );
}

type PayoffUtilizationTimelineProps = {
  cardOrder: CardPayoffOrder[];
  limitsByCardId: Map<number, number>;
  /** Same total limits as the Utilization summary (all credit cards). */
  totalCreditLimits: number;
  /** Balances for cards not included in this payoff plan (held constant). */
  outsidePlanBalance: number;
  /** Defaults to 30% when not provided. */
  targetPercent?: number;
};

export function PayoffUtilizationTimeline({
  cardOrder,
  limitsByCardId,
  totalCreditLimits,
  outsidePlanBalance,
  targetPercent = UTILIZATION_MARKER_PERCENT,
}: PayoffUtilizationTimelineProps) {
  const hits = useMemo(
    () =>
      findUtilizationHits(cardOrder, limitsByCardId, targetPercent, {
        totalCreditLimits,
        outsidePlanBalance,
      }),
    [
      cardOrder,
      limitsByCardId,
      targetPercent,
      totalCreditLimits,
      outsidePlanBalance,
    ],
  );

  const maxMonthIndex = useMemo(() => {
    const fromHits = hits.reduce((max, hit) => Math.max(max, hit.monthIndex), 0);
    const fromSchedules = cardOrder.reduce((max, card) => {
      const len = card.monthlyBalances?.length ?? 0;
      return Math.max(max, Math.max(0, len - 1));
    }, 0);
    return Math.max(fromHits, fromSchedules, 1);
  }, [hits, cardOrder]);

  if (cardOrder.length === 0) return null;

  const width = 720;
  const rowHeight = 28;
  const paddingLeft = 120;
  const paddingRight = 88;
  const paddingTop = 28;
  const paddingBottom = 36;
  const plotWidth = width - paddingLeft - paddingRight;
  const height = paddingTop + paddingBottom + Math.max(hits.length, 1) * rowHeight;

  const xForMonth = (monthIndex: number) =>
    paddingLeft + (monthIndex / maxMonthIndex) * plotWidth;

  const tickCount = Math.min(6, maxMonthIndex);
  const ticks = Array.from({ length: tickCount + 1 }, (_, i) =>
    Math.round((i / tickCount) * maxMonthIndex),
  );

  return (
    <div className="mt-4">
      <h4 className="text-sm font-semibold text-[var(--foreground)]">
        {targetPercent}% utilization milestones
      </h4>
      <p className="mt-1 text-xs text-[var(--muted)]">
        Points mark when each card first reaches {targetPercent}% utilization.
        Overall % matches the Utilization summary (all card balances ÷ all
        credit limits). Cards outside this plan keep their current balances.
      </p>

      {hits.length === 0 ? (
        <p className="mt-3 text-sm text-[var(--muted)]">
          No cards reach {targetPercent}% utilization in this schedule (missing
          credit limits, or balances stay above the target).
        </p>
      ) : (
        <div className="mt-3 overflow-x-auto rounded-xl border border-[var(--border)] bg-black/[0.02] p-3">
          <svg
            viewBox={`0 0 ${width} ${height}`}
            className="h-auto w-full min-w-[32rem]"
            role="img"
            aria-label={`Timeline of cards reaching ${targetPercent}% utilization with overall utilization`}
          >
            <line
              x1={paddingLeft}
              y1={paddingTop - 8}
              x2={paddingLeft + plotWidth}
              y2={paddingTop - 8}
              stroke="var(--border)"
              strokeWidth={1}
            />
            {ticks.map((monthIndex) => {
              const x = xForMonth(monthIndex);
              return (
                <g key={`tick-${monthIndex}`}>
                  <line
                    x1={x}
                    y1={paddingTop - 12}
                    x2={x}
                    y2={height - paddingBottom + 4}
                    stroke="var(--border)"
                    strokeWidth={1}
                    strokeDasharray={monthIndex === 0 ? undefined : "3 4"}
                    opacity={0.7}
                  />
                  <text
                    x={x}
                    y={height - 12}
                    textAnchor="middle"
                    className="fill-[var(--muted)]"
                    fontSize={10}
                  >
                    Mo {monthIndex + 1}
                  </text>
                </g>
              );
            })}

            {hits.map((hit, index) => {
              const y = paddingTop + index * rowHeight + rowHeight / 2;
              const x = xForMonth(hit.monthIndex);
              const overallLabel = `${hit.overallUtilizationPercent.toFixed(1)}%`;
              return (
                <g key={hit.creditCardId}>
                  <text
                    x={paddingLeft - 10}
                    y={y + 3}
                    textAnchor="end"
                    className="fill-[var(--foreground)]"
                    fontSize={11}
                  >
                    {hit.name.length > 14
                      ? `${hit.name.slice(0, 13)}…`
                      : hit.name}
                  </text>
                  <line
                    x1={paddingLeft}
                    y1={y}
                    x2={x}
                    y2={y}
                    stroke="var(--link)"
                    strokeWidth={1.5}
                    opacity={0.35}
                  />
                  <circle
                    cx={x}
                    cy={y}
                    r={6}
                    fill="var(--link)"
                    stroke="white"
                    strokeWidth={2}
                  />
                  <text
                    x={x + 10}
                    y={y + 3}
                    className="fill-[var(--foreground)]"
                    fontSize={11}
                    fontWeight={600}
                  >
                    {overallLabel}
                  </text>
                  <title>
                    {hit.name}: {hit.alreadyAtOrBelow
                      ? `already at or below ${targetPercent}%`
                      : `reaches ${targetPercent}%`}{" "}
                    in {formatMonthLabel(hit.month)} · card balance{" "}
                    {formatCurrencyDetailed(hit.endingBalance)} · overall
                    utilization {overallLabel}
                  </title>
                </g>
              );
            })}
          </svg>

          <ul className="mt-3 grid gap-1.5 text-xs text-[var(--muted)] sm:grid-cols-2">
            {hits.map((hit) => (
              <li key={`legend-${hit.creditCardId}`}>
                <span className="font-medium text-[var(--foreground)]">
                  {hit.name}
                </span>
                {hit.alreadyAtOrBelow
                  ? ` · already ≤${targetPercent}%`
                  : ` · ${formatMonthLabel(hit.month)} (month ${hit.monthIndex + 1})`}
                {" · "}
                <span className="font-medium text-[var(--foreground)]">
                  overall {hit.overallUtilizationPercent.toFixed(1)}%
                </span>
              </li>
            ))}
          </ul>
        </div>
      )}
    </div>
  );
}
