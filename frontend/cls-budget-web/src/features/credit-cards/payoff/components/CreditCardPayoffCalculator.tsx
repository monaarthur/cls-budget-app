"use client";

import { useEffect, useMemo, useState, Fragment, type FormEvent } from "react";
import Link from "next/link";
import { ChevronDown, CheckCircle2, CreditCard, Gauge, Wallet } from "lucide-react";
import { Card } from "@/components/ui/Card";
import { CompanyLogo } from "@/components/ui/CompanyLogo";
import { accountsApi } from "@/features/accounts/api/accountsApi";
import { useCreditCards } from "@/features/accounts/hooks/useCreditCards";
import type { AccountResponse } from "@/features/accounts/types/account";
import { toUpdateAccountRequest } from "@/features/accounts/utils/accountMapper";
import { activePayoffPlanApi } from "@/features/credit-cards/payoff/api/activePayoffPlanApi";
import { payoffApi } from "@/features/credit-cards/payoff/api/payoffApi";
import { payoffPlansApi } from "@/features/credit-cards/payoff/api/payoffPlansApi";
import { PayoffUtilizationTimeline } from "@/features/credit-cards/payoff/components/PayoffUtilizationTimeline";
import { buildDefaultPlanName } from "@/features/credit-cards/payoff/utils/buildDefaultPlanName";
import type { PayoffGoalId } from "@/features/credit-cards/payoff/utils/buildDefaultPlanName";
import type {
  CalculationEnvelope,
  CardPayoffOrder,
  CompareLoanSavingsResult,
  LoanScheduleResult,
  LoanTypeId,
  PayoffStrategySummary,
  PromotionalBalanceTransfer,
  SavedPayoffPlan,
  UtilizationSummaryResult,
} from "@/features/credit-cards/payoff/types";
import { ApiError } from "@/lib/api/client";
import {
  formatCurrencyDetailed,
  parseMoneyInput,
  sanitizeMoneyInput,
} from "@/lib/format";

const DISCLAIMER =
  "The calculations and recommendations provided by this application are estimates for educational and planning purposes only. They are not financial, legal, tax, or credit advice. Actual interest charges, credit-score effects, fees, and payoff dates may differ based on lender rules, transaction timing, and account activity.";

/** Max saved plans in one comparison tray. */
const MAX_COMPARE_PLANS = 3;

const LOAN_TYPE_OPTIONS: {
  value: LoanTypeId;
  label: string;
  description: string;
}[] = [
  {
    value: "Personal",
    label: "Personal loan",
    description: "Fixed term with a fixed monthly payment.",
  },
  {
    value: "HomeEquity",
    label: "Home equity / second mortgage",
    description: "Fixed term, typically longer than a personal loan.",
  },
  {
    value: "Heloc",
    label: "HELOC",
    description: "Interest-only draw period, then amortizing payments.",
  },
  {
    value: "Retirement401k",
    label: "401(k) loan",
    description: "Fixed term repaid through payroll-style payments.",
  },
  {
    value: "Family",
    label: "Family / private loan",
    description: "You set the monthly payment; APR is often zero.",
  },
];

type RepaymentType = "Avalanche" | "Snowball" | "Minimums";

type DisplayPlanResult = {
  key: string;
  name: string;
  summary: PayoffStrategySummary;
};

type PromoTransferDraft = {
  id: string;
  fromCreditCardId: string;
  toCreditCardId: string;
  amount: string;
  promotionalApr: string;
  promotionalMonths: string;
  applyAtMonth: string;
};

function formatUtilizationPercent(
  balance: number,
  creditLimit: number | undefined,
): string {
  if (creditLimit == null || creditLimit <= 0) {
    return "—";
  }
  return `${((balance / creditLimit) * 100).toFixed(1)}%`;
}

function newPromoTransferDraft(): PromoTransferDraft {
  return {
    id: `promo-${Date.now()}-${Math.random().toString(36).slice(2, 7)}`,
    fromCreditCardId: "",
    toCreditCardId: "",
    amount: "",
    promotionalApr: "0",
    promotionalMonths: "12",
    applyAtMonth: "0",
  };
}

const GOAL_OPTIONS = [
  {
    value: "improveCredit" as const,
    label: "Improve credit faster",
    description: "Pay off smaller balances first for quicker wins",
  },
  {
    value: "lowerUtilization" as const,
    label: "Lower utilization",
    description: "Bring card usage down before finishing to $0",
  },
  {
    value: "minimizeInterest" as const,
    label: "Minimize interest",
    description: "Attack highest APR first",
  },
] as const;

const REPAYMENT_OPTIONS = [
  {
    value: "Avalanche" as const,
    description: "Highest APR first",
    disabled: false,
  },
  {
    value: "Snowball" as const,
    description: "Lowest balance first",
    disabled: false,
  },
  {
    value: "Minimums" as const,
    description: "Minimums only (baseline)",
    disabled: false,
  },
] as const;

function repaymentTypeToApiStrategy(type: RepaymentType): string {
  return type === "Minimums" ? "MinimumsOnly" : type;
}

function strategyMatchesType(
  strategyName: string,
  type: RepaymentType,
): boolean {
  const s = strategyName.toLowerCase();
  if (type === "Minimums") {
    return s === "minimumsonly" || s === "minimums";
  }
  return s === type.toLowerCase();
}

function displayStrategyName(strategyName: string): string {
  return strategyName.toLowerCase() === "minimumsonly"
    ? "Minimums"
    : strategyName;
}

function hasInterestRate(card: Pick<AccountResponse, "interestRate">): boolean {
  return card.interestRate != null && Number.isFinite(card.interestRate);
}

function isIncludedInPayoff(
  card: Pick<AccountResponse, "includeInPayoffAnalysis">,
): boolean {
  return card.includeInPayoffAnalysis !== false;
}

function formatPayoffDate(iso: string | null | undefined): string {
  if (!iso) return "—";
  const date = new Date(`${iso}T00:00:00.000Z`);
  if (!Number.isFinite(date.getTime())) return iso;
  return date.toLocaleDateString("en-US", {
    month: "short",
    year: "numeric",
    timeZone: "UTC",
  });
}

function cardExtraTotal(card: {
  monthlyBalances?: { extraPaymentApplied?: number }[];
}): number {
  return (card.monthlyBalances ?? []).reduce(
    (sum, row) => sum + (row.extraPaymentApplied ?? 0),
    0,
  );
}

/** First month the card reaches (or is already at) the utilization target. */
function findUtilizationMetMonth(
  card: CardPayoffOrder,
  creditLimit: number | undefined,
  targetPercent: number,
): string | null {
  if (creditLimit == null || creditLimit <= 0 || targetPercent <= 0) {
    return null;
  }
  const targetBalance = (creditLimit * targetPercent) / 100;
  const rows = card.monthlyBalances ?? [];
  if (rows.length === 0) {
    return null;
  }
  if (rows[0].startingBalance <= targetBalance + 0.005) {
    return rows[0].month;
  }
  for (const row of rows) {
    if (row.endingBalance <= targetBalance + 0.005) {
      return row.month;
    }
  }
  return null;
}

/** First month the card balance reaches $0 (100% paid off). */
function findPaidOffMonth(card: CardPayoffOrder): string | null {
  for (const row of card.monthlyBalances ?? []) {
    if (row.endingBalance <= 0.005) {
      return row.month;
    }
  }
  return card.estimatedPayoffDate;
}

function firstMonthExtraFocus(cardOrder: CardPayoffOrder[]): {
  card: CardPayoffOrder;
  extra: number;
} | null {
  let best: { card: CardPayoffOrder; extra: number } | null = null;
  for (const card of cardOrder) {
    const extra = card.monthlyBalances?.[0]?.extraPaymentApplied ?? 0;
    if (extra <= 0) continue;
    if (!best || extra > best.extra) {
      best = { card, extra };
    }
  }
  return best;
}

export function CreditCardPayoffCalculator() {
  const { accounts, loading, error: loadError, reload } = useCreditCards();
  const activeCards = useMemo(
    () => accounts.filter((card) => !card.isPaidOff && card.balance > 0),
    [accounts],
  );

  const includedCards = useMemo(
    () => activeCards.filter(isIncludedInPayoff),
    [activeCards],
  );

  const excludedCards = useMemo(
    () => activeCards.filter((card) => !isIncludedInPayoff(card)),
    [activeCards],
  );

  const cardsMissingRate = useMemo(
    () => includedCards.filter((card) => !hasInterestRate(card)),
    [includedCards],
  );

  const ratesReady =
    includedCards.length > 0 && cardsMissingRate.length === 0;

  const defaultMonthly = useMemo(
    () =>
      includedCards.reduce((sum, card) => sum + (card.monthlyPayment ?? 0), 0),
    [includedCards],
  );

  const [aprDrafts, setAprDrafts] = useState<Record<number, string>>({});
  const [extraMonthlyPayment, setExtraMonthlyPayment] = useState("");
  const [savingRates, setSavingRates] = useState(false);
  const [rateStatus, setRateStatus] = useState<string | null>(null);
  const [editingRates, setEditingRates] = useState(false);

  const [selectedToExclude, setSelectedToExclude] = useState("");
  const [savingExclude, setSavingExclude] = useState(false);
  const [excludeStatus, setExcludeStatus] = useState<string | null>(null);
  const [excludeSectionOpen, setExcludeSectionOpen] = useState(false);

  const [payOverLimitFirst, setPayOverLimitFirst] = useState(false);
  const [enableCashAdvanceBalanceMoves, setEnableCashAdvanceBalanceMoves] =
    useState(false);
  const [targetUtilization, setTargetUtilization] = useState("");
  const [postUtilizationStrategy, setPostUtilizationStrategy] = useState<
    "Avalanche" | "Snowball" | null
  >(null);
  const [promoTransfers, setPromoTransfers] = useState<PromoTransferDraft[]>(
    [],
  );
  const [selectedGoal, setSelectedGoal] = useState<PayoffGoalId | null>(null);
  const [loanAmount, setLoanAmount] = useState("");
  const [loanApr, setLoanApr] = useState("");
  const [loanType, setLoanType] = useState<LoanTypeId>("Personal");
  const [loanTermMonths, setLoanTermMonths] = useState("36");
  const [loanInterestOnlyMonths, setLoanInterestOnlyMonths] = useState("0");
  const [loanFixedMonthlyPayment, setLoanFixedMonthlyPayment] = useState("");
  const [loanSchedule, setLoanSchedule] = useState<LoanScheduleResult | null>(
    null,
  );
  const [loanScheduleError, setLoanScheduleError] = useState<string | null>(
    null,
  );
  const [loanScheduleLoading, setLoanScheduleLoading] = useState(false);
  const [loanApplyStrategy, setLoanApplyStrategy] = useState<
    "Avalanche" | "Snowball" | "SelectedAccounts"
  >("Avalanche");
  const [loanApplyCreditCardIds, setLoanApplyCreditCardIds] = useState<
    number[]
  >([]);
  const [loanSavingsLoading, setLoanSavingsLoading] = useState(false);
  const [loanSavingsError, setLoanSavingsError] = useState<string | null>(null);
  const [loanSavings, setLoanSavings] =
    useState<CompareLoanSavingsResult | null>(null);
  const [selectedRepaymentType, setSelectedRepaymentType] =
    useState<RepaymentType | null>(null);
  const [comparing, setComparing] = useState(false);
  const [status, setStatus] = useState<string | null>(null);
  const [compareWarnings, setCompareWarnings] = useState<string[]>([]);
  const [compareAssumptions, setCompareAssumptions] = useState<string[]>([]);
  const [formulaVersion, setFormulaVersion] = useState<string | null>(null);
  const [displayPlans, setDisplayPlans] = useState<DisplayPlanResult[]>([]);
  const [selectedPlanKey, setSelectedPlanKey] = useState<string | null>(null);
  const [selectedCardId, setSelectedCardId] = useState<number | null>(null);
  const [compareTray, setCompareTray] = useState<SavedPayoffPlan[]>([]);
  const [namingOpen, setNamingOpen] = useState(false);
  const [planNameDraft, setPlanNameDraft] = useState("");
  const [savingPlan, setSavingPlan] = useState(false);
  const [startingPlan, setStartingPlan] = useState(false);
  const [utilization, setUtilization] = useState<CalculationEnvelope<
    UtilizationSummaryResult
  > | null>(null);

  const showRateStep =
    includedCards.length > 0 && (!ratesReady || editingRates);

  const totalExtraPayment = useMemo(() => {
    const value = parseMoneyInput(extraMonthlyPayment);
    return value !== null && value > 0 ? value : 0;
  }, [extraMonthlyPayment]);

  const planUtilizationTargetPercent = useMemo(() => {
    const value = Number(targetUtilization.trim());
    if (!Number.isFinite(value) || value <= 0 || value > 99) {
      return 30;
    }
    return value;
  }, [targetUtilization]);

  const totalMonthlyPayment = useMemo(
    () => defaultMonthly + totalExtraPayment,
    [defaultMonthly, totalExtraPayment],
  );

  useEffect(() => {
    setAprDrafts((prev) => {
      const next: Record<number, string> = {};
      for (const card of includedCards) {
        if (prev[card.accountId] !== undefined) {
          next[card.accountId] = prev[card.accountId];
        } else if (hasInterestRate(card)) {
          next[card.accountId] = String(card.interestRate);
        } else {
          next[card.accountId] = "";
        }
      }
      return next;
    });
  }, [includedCards]);

  useEffect(() => {
    let cancelled = false;
    void (async () => {
      try {
        const summary = await payoffApi.utilizationSummary();
        if (!cancelled) setUtilization(summary);
      } catch {
        // Utilization is secondary; compare still works.
      }
    })();
    return () => {
      cancelled = true;
    };
  }, []);

  async function setIncludeInPayoff(card: AccountResponse, include: boolean) {
    setSavingExclude(true);
    setExcludeStatus(null);
    try {
      await accountsApi.update(card.accountId, {
        ...toUpdateAccountRequest(card),
        includeInPayoffAnalysis: include,
      });
      await reload();
      setSelectedToExclude("");
      clearResults();
      setSelectedCardId(null);
    } catch (err) {
      const message =
        err instanceof ApiError
          ? err.errors.join(", ") || err.message
          : err instanceof Error
            ? err.message
            : "Failed to update payoff exclusion";
      setExcludeStatus(message);
    } finally {
      setSavingExclude(false);
    }
  }

  async function handleExcludeSelected(event: FormEvent) {
    event.preventDefault();
    const accountId = Number(selectedToExclude);
    const card = includedCards.find((c) => c.accountId === accountId);
    if (!card) {
      setExcludeStatus("Select a credit card to exclude.");
      return;
    }
    await setIncludeInPayoff(card, false);
  }

  async function handleSaveRates(event: FormEvent) {
    event.preventDefault();
    setRateStatus(null);

    const cardsToSave = editingRates ? includedCards : cardsMissingRate;
    const parsed: { card: AccountResponse; rate: number }[] = [];

    for (const card of cardsToSave) {
      const raw = (aprDrafts[card.accountId] ?? "").trim();
      const rate = Number(raw);
      if (raw === "" || !Number.isFinite(rate) || rate < 0) {
        setRateStatus(
          `Enter a valid purchase APR (0 or greater) for ${card.name}.`,
        );
        return;
      }
      parsed.push({ card, rate });
    }

    setSavingRates(true);
    try {
      await Promise.all(
        parsed.map(({ card, rate }) =>
          accountsApi.update(card.accountId, {
            ...toUpdateAccountRequest(card),
            interestRate: rate,
          }),
        ),
      );
      await reload();
      setEditingRates(false);
      clearResults();
      setRateStatus(null);
    } catch (err) {
      const message =
        err instanceof ApiError
          ? err.errors.join(", ") || err.message
          : err instanceof Error
            ? err.message
            : "Failed to save interest rates";
      setRateStatus(message);
    } finally {
      setSavingRates(false);
    }
  }

  useEffect(() => {
    let cancelled = false;
    void (async () => {
      try {
        const plans = await payoffPlansApi.list();
        if (!cancelled) {
          setCompareTray(plans.slice(0, MAX_COMPARE_PLANS));
        }
      } catch {
        // Tray can stay empty until the user saves a plan.
      }
    })();
    return () => {
      cancelled = true;
    };
  }, []);

  useEffect(() => {
    const amount = parseMoneyInput(loanAmount);
    if (amount === null || amount <= 0) {
      setLoanSchedule(null);
      setLoanScheduleError(null);
      setLoanScheduleLoading(false);
      return;
    }

    const apr =
      loanApr.trim() === "" ? 0 : (parseMoneyInput(loanApr) ?? Number.NaN);
    if (!Number.isFinite(apr) || apr < 0) {
      setLoanSchedule(null);
      setLoanScheduleError("Loan interest rate must be zero or greater.");
      return;
    }

    let termMonths: number | null = null;
    let interestOnlyMonths: number | null = null;
    let fixedMonthlyPayment: number | null = null;

    if (loanType === "Family") {
      const payment = parseMoneyInput(loanFixedMonthlyPayment);
      if (payment === null || payment <= 0) {
        setLoanSchedule(null);
        setLoanScheduleError(null);
        return;
      }
      fixedMonthlyPayment = payment;
    } else {
      const term = Number.parseInt(loanTermMonths.trim(), 10);
      if (!Number.isFinite(term) || term < 1) {
        setLoanSchedule(null);
        setLoanScheduleError(null);
        return;
      }
      termMonths = term;
      if (loanType === "Heloc") {
        const io = Number.parseInt(loanInterestOnlyMonths.trim() || "0", 10);
        if (!Number.isFinite(io) || io < 0 || io >= term) {
          setLoanSchedule(null);
          setLoanScheduleError(
            io >= term
              ? "Interest-only months must be less than the total term."
              : null,
          );
          return;
        }
        interestOnlyMonths = io;
      }
    }

    let cancelled = false;
    const timer = window.setTimeout(async () => {
      setLoanScheduleLoading(true);
      setLoanScheduleError(null);
      try {
        const envelope = await payoffApi.loanSchedule({
          loanType,
          amount,
          annualPercentageRate: apr,
          termMonths,
          interestOnlyMonths,
          fixedMonthlyPayment,
        });
        if (!cancelled) {
          setLoanSchedule(envelope.result);
        }
      } catch (err) {
        if (!cancelled) {
          setLoanSchedule(null);
          setLoanScheduleError(
            err instanceof ApiError
              ? err.errors.join(", ") || err.message
              : err instanceof Error
                ? err.message
                : "Could not calculate loan schedule",
          );
        }
      } finally {
        if (!cancelled) {
          setLoanScheduleLoading(false);
        }
      }
    }, 300);

    return () => {
      cancelled = true;
      window.clearTimeout(timer);
    };
  }, [
    loanAmount,
    loanApr,
    loanType,
    loanTermMonths,
    loanInterestOnlyMonths,
    loanFixedMonthlyPayment,
  ]);

  function clearResults() {
    setDisplayPlans([]);
    setSelectedPlanKey(null);
    setSelectedCardId(null);
    setCompareWarnings([]);
    setCompareAssumptions([]);
    setFormulaVersion(null);
    setLoanSavings(null);
    setLoanSavingsError(null);
  }

  async function handleShowLoanSavings() {
    const parsed = parsePlanOptions();
    if ("error" in parsed) {
      setLoanSavingsError(parsed.error);
      setLoanSavings(null);
      return;
    }
    if (parsed.loanAmount == null || parsed.loanType == null) {
      setLoanSavingsError(
        "Enter a loan amount and complete the loan details before comparing savings.",
      );
      setLoanSavings(null);
      return;
    }
    if (!Number.isFinite(totalMonthlyPayment) || totalMonthlyPayment <= 0) {
      setLoanSavingsError(
        "Enter card minimum payments and/or an extra monthly payment greater than zero.",
      );
      setLoanSavings(null);
      return;
    }

    setLoanSavingsLoading(true);
    setLoanSavingsError(null);
    try {
      const envelope = await payoffApi.loanSavings({
        totalMonthlyDebtPayment: totalMonthlyPayment,
        strategy: repaymentTypeToApiStrategy(
          selectedRepaymentType ?? "Avalanche",
        ),
        targetUtilizationPercent: parsed.targetUtilizationPercent,
        payOverLimitFirst,
        enableCashAdvanceBalanceMoves,
        promotionalTransfers: parsed.promotionalTransfers,
        postUtilizationStrategy,
        loanAmount: parsed.loanAmount,
        loanAnnualPercentageRate: parsed.loanAnnualPercentageRate ?? 0,
        loanApplyStrategy: parsed.loanApplyStrategy,
        loanApplyCreditCardIds: parsed.loanApplyCreditCardIds,
        loanType: parsed.loanType,
        loanTermMonths: parsed.loanTermMonths,
        loanInterestOnlyMonths: parsed.loanInterestOnlyMonths,
        loanFixedMonthlyPayment: parsed.loanFixedMonthlyPayment,
      });
      setLoanSavings(envelope.result);
    } catch (err) {
      setLoanSavings(null);
      setLoanSavingsError(
        err instanceof ApiError
          ? err.errors.join(", ") || err.message
          : err instanceof Error
            ? err.message
            : "Could not compare loan savings",
      );
    } finally {
      setLoanSavingsLoading(false);
    }
  }

  function parsePlanOptions():
    | {
        targetUtilizationPercent: number | null;
        promotionalTransfers: PromotionalBalanceTransfer[];
        loanAmount: number | null;
        loanAnnualPercentageRate: number | null;
        loanApplyStrategy: "Avalanche" | "Snowball" | "SelectedAccounts" | null;
        loanApplyCreditCardIds: number[] | null;
        loanType: LoanTypeId | null;
        loanTermMonths: number | null;
        loanInterestOnlyMonths: number | null;
        loanFixedMonthlyPayment: number | null;
      }
    | { error: string } {
    const loanRaw = loanAmount.trim();
    let parsedLoanAmount: number | null = null;
    let parsedLoanApr: number | null = null;
    let parsedLoanApplyStrategy:
      | "Avalanche"
      | "Snowball"
      | "SelectedAccounts"
      | null = null;
    let parsedLoanApplyCreditCardIds: number[] | null = null;
    let parsedLoanType: LoanTypeId | null = null;
    let parsedLoanTermMonths: number | null = null;
    let parsedLoanInterestOnlyMonths: number | null = null;
    let parsedLoanFixedMonthlyPayment: number | null = null;
    if (loanRaw !== "") {
      const amount = parseMoneyInput(loanRaw);
      if (amount === null || amount <= 0) {
        return { error: "Loan amount must be greater than zero, or blank to skip." };
      }
      parsedLoanAmount = amount;
      const aprRaw = loanApr.trim();
      const apr = aprRaw === "" ? 0 : parseMoneyInput(aprRaw);
      if (apr === null || apr < 0) {
        return { error: "Loan interest rate must be zero or greater." };
      }
      parsedLoanApr = apr;
      parsedLoanApplyStrategy = loanApplyStrategy;
      parsedLoanType = loanType;
      if (loanApplyStrategy === "SelectedAccounts") {
        if (loanApplyCreditCardIds.length === 0) {
          return {
            error:
              "Select at least one credit card account to apply loan proceeds to.",
          };
        }
        parsedLoanApplyCreditCardIds = loanApplyCreditCardIds;
      }

      if (loanType === "Family") {
        const payment = parseMoneyInput(loanFixedMonthlyPayment);
        if (payment === null || payment <= 0) {
          return {
            error: "Enter a fixed monthly payment for a family / private loan.",
          };
        }
        parsedLoanFixedMonthlyPayment = payment;
      } else {
        const term = Number.parseInt(loanTermMonths.trim(), 10);
        if (!Number.isFinite(term) || term < 1) {
          return { error: "Enter a loan term of at least 1 month." };
        }
        parsedLoanTermMonths = term;
        if (loanType === "Heloc") {
          const io = Number.parseInt(loanInterestOnlyMonths.trim() || "0", 10);
          if (!Number.isFinite(io) || io < 0) {
            return { error: "Interest-only months must be zero or greater." };
          }
          if (io >= term) {
            return {
              error: "Interest-only months must be less than the total term.",
            };
          }
          parsedLoanInterestOnlyMonths = io;
        }
      }
    }

    const utilRaw = targetUtilization.trim();
    let targetUtilizationPercent: number | null = null;
    if (utilRaw !== "") {
      const util = Number(utilRaw);
      if (!Number.isFinite(util) || util < 1 || util > 99) {
        return {
          error:
            "Target utilization must be between 1 and 99, or blank for none.",
        };
      }
      targetUtilizationPercent = util;
    }

    if (postUtilizationStrategy != null && targetUtilizationPercent == null) {
      return {
        error:
          "Enter a target utilization % before choosing Avalanche or Snowball after utilization is met.",
      };
    }

    const promotionalTransfers: PromotionalBalanceTransfer[] = [];
    for (const row of promoTransfers) {
      const fromId = Number(row.fromCreditCardId);
      const toId = Number(row.toCreditCardId);
      if (!Number.isFinite(fromId) || !Number.isFinite(toId) || fromId === toId) {
        return {
          error:
            "Each promotional transfer needs different From and To credit cards.",
        };
      }
      const promoApr = Number(row.promotionalApr);
      const promoMonths = Number.parseInt(row.promotionalMonths, 10);
      const applyAt = Number.parseInt(row.applyAtMonth || "0", 10);
      if (!Number.isFinite(promoApr) || promoApr < 0) {
        return { error: "Promotional APR must be zero or greater." };
      }
      if (!Number.isFinite(promoMonths) || promoMonths < 1) {
        return { error: "Promotional period must be at least 1 month." };
      }
      if (!Number.isFinite(applyAt) || applyAt < 0) {
        return { error: "Transfer month offset must be 0 or greater." };
      }
      const amountRaw = row.amount.trim();
      let transferAmount: number | null = null;
      if (amountRaw !== "") {
        transferAmount = parseMoneyInput(amountRaw);
        if (transferAmount === null || transferAmount <= 0) {
          return {
            error: "Transfer amount must be blank (max) or greater than zero.",
          };
        }
      }
      promotionalTransfers.push({
        fromCreditCardId: fromId,
        toCreditCardId: toId,
        amount: transferAmount,
        promotionalAnnualPercentageRate: promoApr,
        promotionalPeriodMonths: promoMonths,
        applyAtMonthOffset: applyAt,
      });
    }

    return {
      targetUtilizationPercent,
      promotionalTransfers,
      loanAmount: parsedLoanAmount,
      loanAnnualPercentageRate: parsedLoanApr,
      loanApplyStrategy: parsedLoanApplyStrategy,
      loanApplyCreditCardIds: parsedLoanApplyCreditCardIds,
      loanType: parsedLoanType,
      loanTermMonths: parsedLoanTermMonths,
      loanInterestOnlyMonths: parsedLoanInterestOnlyMonths,
      loanFixedMonthlyPayment: parsedLoanFixedMonthlyPayment,
    };
  }

  function applyEnvelopeMeta(envelope: {
    warnings: string[];
    assumptions: string[];
    formulaVersion: string;
  }) {
    setCompareWarnings(envelope.warnings ?? []);
    setCompareAssumptions(envelope.assumptions ?? []);
    setFormulaVersion(envelope.formulaVersion ?? null);
  }

  function selectPlan(plan: DisplayPlanResult) {
    setSelectedPlanKey(plan.key);
    const focus = firstMonthExtraFocus(plan.summary.cardOrder);
    setSelectedCardId(focus?.card.creditCardId ?? null);
  }

  function applyGoalPresets(goal: PayoffGoalId) {
    setSelectedGoal(goal);
    if (goal === "improveCredit") {
      setSelectedRepaymentType("Snowball");
      setTargetUtilization("");
      setPostUtilizationStrategy(null);
    } else if (goal === "lowerUtilization") {
      setSelectedRepaymentType((current) => current ?? "Avalanche");
      setTargetUtilization((current) =>
        current.trim() === "" ? "30" : current,
      );
    } else {
      setSelectedRepaymentType("Avalanche");
      setTargetUtilization("");
      setPostUtilizationStrategy(null);
    }
    clearResults();
  }

  function openAddPlanNaming() {
    if (!selectedRepaymentType) {
      setStatus("Select a payment order in step 3 before adding a plan.");
      return;
    }
    if (compareTray.length >= MAX_COMPARE_PLANS) {
      setStatus(
        `You can compare at most ${MAX_COMPARE_PLANS} plans. Remove one from the tray first.`,
      );
      return;
    }
    const parsed = parsePlanOptions();
    if ("error" in parsed) {
      setStatus(parsed.error);
      return;
    }
    const amount = totalMonthlyPayment;
    if (!Number.isFinite(amount) || amount <= 0) {
      setStatus(
        "Enter card minimum payments and/or an extra monthly payment greater than zero.",
      );
      return;
    }
    setStatus(null);
    setPlanNameDraft(
      buildDefaultPlanName({
        goal: selectedGoal,
        repaymentType: selectedRepaymentType,
        extraMonthlyPayment: totalExtraPayment,
        targetUtilizationPercent: parsed.targetUtilizationPercent,
        postUtilizationStrategy,
        enableCashAdvanceBalanceMoves,
        promotionalTransferCount: parsed.promotionalTransfers.length,
        loanAmount: parsed.loanAmount,
        loanAnnualPercentageRate: parsed.loanAnnualPercentageRate,
        loanApplyStrategy: parsed.loanApplyStrategy,
        loanApplyCreditCardIds: parsed.loanApplyCreditCardIds,
        loanType: parsed.loanType,
        loanTermMonths: parsed.loanTermMonths,
      }),
    );
    setNamingOpen(true);
  }

  async function handleSavePlanToCompare() {
    if (!selectedRepaymentType) {
      setStatus("Select a payment order in step 3 before adding a plan.");
      return;
    }
    if (compareTray.length >= MAX_COMPARE_PLANS) {
      setStatus(
        `You can compare at most ${MAX_COMPARE_PLANS} plans. Remove one from the tray first.`,
      );
      return;
    }
    const name = planNameDraft.trim();
    if (name.length === 0) {
      setStatus("Enter a name for the plan.");
      return;
    }
    const parsed = parsePlanOptions();
    if ("error" in parsed) {
      setStatus(parsed.error);
      return;
    }
    const amount = totalMonthlyPayment;
    if (!Number.isFinite(amount) || amount <= 0) {
      setStatus(
        "Enter card minimum payments and/or an extra monthly payment greater than zero.",
      );
      return;
    }

    setSavingPlan(true);
    setStatus(null);
    try {
      const saved = await payoffPlansApi.create({
        name,
        goal: selectedGoal,
        strategy: repaymentTypeToApiStrategy(selectedRepaymentType),
        extraMonthlyPayment: totalExtraPayment,
        totalMonthlyDebtPayment: amount,
        targetUtilizationPercent: parsed.targetUtilizationPercent,
        payOverLimitFirst,
        postUtilizationStrategy,
        enableCashAdvanceBalanceMoves,
        loanAmount: parsed.loanAmount,
        loanAnnualPercentageRate: parsed.loanAnnualPercentageRate,
        loanApplyStrategy: parsed.loanApplyStrategy,
        loanApplyCreditCardIds: parsed.loanApplyCreditCardIds,
        loanType: parsed.loanType,
        loanTermMonths: parsed.loanTermMonths,
        loanInterestOnlyMonths: parsed.loanInterestOnlyMonths,
        loanFixedMonthlyPayment: parsed.loanFixedMonthlyPayment,
        promotionalTransfers: parsed.promotionalTransfers,
      });
      setCompareTray((current) => {
        if (current.some((p) => p.savedPayoffPlanId === saved.savedPayoffPlanId)) {
          return current;
        }
        if (current.length >= MAX_COMPARE_PLANS) {
          return current;
        }
        return [...current, saved];
      });
      setNamingOpen(false);
      setPlanNameDraft("");
    } catch (err) {
      const message =
        err instanceof ApiError
          ? err.errors.join(", ") || err.message
          : err instanceof Error
            ? err.message
            : "Failed to save plan";
      setStatus(message);
    } finally {
      setSavingPlan(false);
    }
  }

  async function handleStartThisPlan() {
    if (!selectedRepaymentType) {
      setStatus("Select a payment order in step 3 before starting a plan.");
      return;
    }
    const parsed = parsePlanOptions();
    if ("error" in parsed) {
      setStatus(parsed.error);
      return;
    }
    const amount = totalMonthlyPayment;
    if (!Number.isFinite(amount) || amount <= 0) {
      setStatus(
        "Enter card minimum payments and/or an extra monthly payment greater than zero.",
      );
      return;
    }

    const existing = await activePayoffPlanApi.getActive().catch(() => null);
    const confirmed = window.confirm(
      existing
        ? `Start this plan? Your current active plan ("${existing.name}") will be completed/archived.`
        : "Start this plan? You can record payments against it and revise it later.",
    );
    if (!confirmed) return;

    setStartingPlan(true);
    setStatus(null);
    try {
      const name = buildDefaultPlanName({
        goal: selectedGoal,
        repaymentType: selectedRepaymentType,
        extraMonthlyPayment: totalExtraPayment,
        targetUtilizationPercent: parsed.targetUtilizationPercent,
        postUtilizationStrategy,
        enableCashAdvanceBalanceMoves,
        promotionalTransferCount: parsed.promotionalTransfers.length,
        loanAmount: parsed.loanAmount,
        loanAnnualPercentageRate: parsed.loanAnnualPercentageRate,
        loanApplyStrategy: parsed.loanApplyStrategy,
        loanApplyCreditCardIds: parsed.loanApplyCreditCardIds,
        loanType: parsed.loanType,
        loanTermMonths: parsed.loanTermMonths,
      });
      await activePayoffPlanApi.activate({
        name,
        goal: selectedGoal,
        strategy: repaymentTypeToApiStrategy(selectedRepaymentType),
        extraMonthlyPayment: totalExtraPayment,
        totalMonthlyDebtPayment: amount,
        targetUtilizationPercent: parsed.targetUtilizationPercent,
        payOverLimitFirst,
        postUtilizationStrategy,
        enableCashAdvanceBalanceMoves,
        loanAmount: parsed.loanAmount,
        loanAnnualPercentageRate: parsed.loanAnnualPercentageRate,
        loanApplyStrategy: parsed.loanApplyStrategy,
        loanApplyCreditCardIds: parsed.loanApplyCreditCardIds,
        loanType: parsed.loanType,
        loanTermMonths: parsed.loanTermMonths,
        loanInterestOnlyMonths: parsed.loanInterestOnlyMonths,
        loanFixedMonthlyPayment: parsed.loanFixedMonthlyPayment,
        promotionalTransfers: parsed.promotionalTransfers,
        reason: "Started from payoff calculator",
      });
      setStatus("Plan started. Open Active Plan to record payments and track progress.");
      window.location.href = "/credit-cards/payoff/active";
    } catch (err) {
      const message =
        err instanceof ApiError
          ? err.errors.join(", ") || err.message
          : err instanceof Error
            ? err.message
            : "Failed to start plan";
      setStatus(message);
    } finally {
      setStartingPlan(false);
    }
  }

  async function handleStartSavedPlan(saved: SavedPayoffPlan) {
    const existing = await activePayoffPlanApi.getActive().catch(() => null);
    const confirmed = window.confirm(
      existing
        ? `Start "${saved.name}"? Your current active plan ("${existing.name}") will be completed/archived.`
        : `Start "${saved.name}" as your active payoff plan?`,
    );
    if (!confirmed) return;

    setStartingPlan(true);
    setStatus(null);
    try {
      await activePayoffPlanApi.activate({
        savedPayoffPlanId: saved.savedPayoffPlanId,
        reason: "Started from saved compare plan",
      });
      window.location.href = "/credit-cards/payoff/active";
    } catch (err) {
      const message =
        err instanceof ApiError
          ? err.errors.join(", ") || err.message
          : err instanceof Error
            ? err.message
            : "Failed to start plan";
      setStatus(message);
    } finally {
      setStartingPlan(false);
    }
  }

  async function handleRemoveFromTray(planId: number, deleteFromDb: boolean) {
    setStatus(null);
    try {
      if (deleteFromDb) {
        await payoffPlansApi.remove(planId);
      }
      setCompareTray((current) =>
        current.filter((p) => p.savedPayoffPlanId !== planId),
      );
      clearResults();
    } catch (err) {
      const message =
        err instanceof ApiError
          ? err.errors.join(", ") || err.message
          : err instanceof Error
            ? err.message
            : "Failed to remove plan";
      setStatus(message);
    }
  }

  async function handleShowCurrentPlan(event: FormEvent) {
    event.preventDefault();
    if (!selectedRepaymentType) {
      setStatus("Select a payment order (Avalanche, Snowball, or Minimums).");
      return;
    }

    const parsed = parsePlanOptions();
    if ("error" in parsed) {
      setStatus(parsed.error);
      return;
    }

    const amount = totalMonthlyPayment;
    if (!Number.isFinite(amount) || amount <= 0) {
      setStatus(
        "Enter card minimum payments and/or an extra monthly payment greater than zero.",
      );
      return;
    }

    setComparing(true);
    setStatus(null);
    try {
      const envelope = await payoffApi.compare({
        totalMonthlyDebtPayment: amount,
        targetUtilizationPercent: parsed.targetUtilizationPercent,
        payOverLimitFirst,
        enableCashAdvanceBalanceMoves,
        promotionalTransfers: parsed.promotionalTransfers,
        postUtilizationStrategy,
        loanAmount: parsed.loanAmount,
        loanAnnualPercentageRate: parsed.loanAnnualPercentageRate,
        loanApplyStrategy: parsed.loanApplyStrategy,
        loanApplyCreditCardIds: parsed.loanApplyCreditCardIds,
        loanType: parsed.loanType,
        loanTermMonths: parsed.loanTermMonths,
        loanInterestOnlyMonths: parsed.loanInterestOnlyMonths,
        loanFixedMonthlyPayment: parsed.loanFixedMonthlyPayment,
      });
      applyEnvelopeMeta(envelope);
      const strategy = envelope.result.strategies.find((s) =>
        strategyMatchesType(s.strategy, selectedRepaymentType),
      );
      if (!strategy) {
        setStatus("No strategy result was returned for the selected order.");
        clearResults();
        return;
      }
      const plan: DisplayPlanResult = {
        key: `current-${strategy.strategy}`,
        name: `${displayStrategyName(strategy.strategy)} (current setup)`,
        summary: strategy,
      };
      setDisplayPlans([plan]);
      selectPlan(plan);
    } catch (err) {
      const message =
        err instanceof ApiError
          ? err.errors.join(", ") || err.message
          : err instanceof Error
            ? err.message
            : "Failed to show payoff plan";
      setStatus(message);
      clearResults();
    } finally {
      setComparing(false);
    }
  }

  async function handleCompareTrayPlans() {
    if (compareTray.length === 0) {
      setStatus("Add at least one plan to the compare tray first.");
      return;
    }

    setComparing(true);
    setStatus(null);
    try {
      const envelope = await payoffPlansApi.compareSaved({
        planIds: compareTray.map((p) => p.savedPayoffPlanId),
      });
      applyEnvelopeMeta(envelope);
      const plans: DisplayPlanResult[] = envelope.result.plans.map((item) => ({
        key: `saved-${item.savedPayoffPlanId}`,
        name: item.name,
        summary: item.strategySummary,
      }));
      setDisplayPlans(plans);
      const preferred =
        plans.find((p) => p.summary.isValid) ?? plans[0] ?? null;
      if (preferred) {
        selectPlan(preferred);
      } else {
        setSelectedPlanKey(null);
        setSelectedCardId(null);
      }
    } catch (err) {
      const message =
        err instanceof ApiError
          ? err.errors.join(", ") || err.message
          : err instanceof Error
            ? err.message
            : "Failed to compare saved plans";
      setStatus(message);
      clearResults();
    } finally {
      setComparing(false);
    }
  }

  const isCompareMode = displayPlans.length > 1;
  const selectedStrategy =
    displayPlans.find((plan) => plan.key === selectedPlanKey)?.summary ?? null;
  const cardsByPayoffDate = useMemo(() => {
    if (!selectedStrategy) return [];
    return [...selectedStrategy.cardOrder].sort((a, b) => {
      if (!a.estimatedPayoffDate && !b.estimatedPayoffDate) {
        return a.priorityOrder - b.priorityOrder;
      }
      if (!a.estimatedPayoffDate) return 1;
      if (!b.estimatedPayoffDate) return -1;
      return (
        a.estimatedPayoffDate.localeCompare(b.estimatedPayoffDate) ||
        a.priorityOrder - b.priorityOrder
      );
    });
  }, [selectedStrategy]);
  const firstPaidOffCard = cardsByPayoffDate.find(
    (card) => card.estimatedPayoffDate,
  );
  const extraFocus = selectedStrategy
    ? firstMonthExtraFocus(selectedStrategy.cardOrder)
    : null;
  const creditLimitsByCardId = useMemo(() => {
    const map = new Map<number, number>();
    for (const card of includedCards) {
      if (card.limit > 0) {
        map.set(card.accountId, card.limit);
      }
    }
    for (const card of utilization?.result.cards ?? []) {
      if (card.creditLimit > 0) {
        map.set(card.creditCardId, card.creditLimit);
      }
    }
    return map;
  }, [includedCards, utilization]);
  const portfolioUtilizationBasis = useMemo(() => {
    const planIds = new Set(
      (selectedStrategy?.cardOrder ?? []).map((card) => card.creditCardId),
    );
    const utilCards = utilization?.result.cards ?? [];
    const outsidePlanBalance = utilCards
      .filter((card) => !planIds.has(card.creditCardId))
      .reduce((sum, card) => sum + card.currentBalance, 0);
    const totalCreditLimits =
      utilization?.result.totalCreditLimits ??
      utilCards.reduce((sum, card) => sum + card.creditLimit, 0);
    return { outsidePlanBalance, totalCreditLimits };
  }, [selectedStrategy, utilization]);
  const paymentAtOrBelowMinimums = totalExtraPayment <= 0.0001;
  const strategiesHaveIdenticalTotals =
    displayPlans.length >= 2 &&
    displayPlans.every(
      (plan) =>
        plan.summary.totalInterest === displayPlans[0].summary.totalInterest &&
        plan.summary.monthsToPayoff === displayPlans[0].summary.monthsToPayoff,
    );
  const rateFormCards = editingRates ? includedCards : cardsMissingRate;

  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-xl font-semibold text-[var(--foreground)]">
          Payoff calculator
        </h2>
          <p className="mt-1 text-sm text-[var(--muted)]">
            Start getting out of your debts below. Follow the plan…
          </p>
      </div>

      {loading ? (
        <p className="text-sm text-[var(--muted)]">Loading credit cards…</p>
      ) : null}
      {loadError ? (
        <p className="rounded-xl border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-900">
          {loadError}
        </p>
      ) : null}

      {utilization ? (
        <Card className="p-5">
          <div className="grid grid-cols-1 gap-3 sm:grid-cols-3">
            <div className="flex items-start gap-3 rounded-xl border border-[var(--border)] bg-black/[0.02] px-3 py-3">
              <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-lg bg-[var(--accent-soft)] text-[var(--link)]">
                <Gauge className="h-4 w-4" strokeWidth={2} />
              </span>
              <div className="min-w-0">
                <p className="text-xs font-medium uppercase tracking-wide text-[var(--muted)]">
                  Utilization
                </p>
                <p className="mt-0.5 text-lg font-semibold tabular-nums">
                  {utilization.result.overallUtilizationPercentage.toFixed(1)}%
                </p>
              </div>
            </div>
            <div className="flex items-start gap-3 rounded-xl border border-[var(--border)] bg-black/[0.02] px-3 py-3">
              <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-lg bg-[var(--accent-soft)] text-[var(--link)]">
                <Wallet className="h-4 w-4" strokeWidth={2} />
              </span>
              <div className="min-w-0">
                <p className="text-xs font-medium uppercase tracking-wide text-[var(--muted)]">
                  Balances
                </p>
                <p className="mt-0.5 text-lg font-semibold tabular-nums">
                  {formatCurrencyDetailed(utilization.result.totalBalances)}
                </p>
              </div>
            </div>
            <div className="flex items-start gap-3 rounded-xl border border-[var(--border)] bg-black/[0.02] px-3 py-3">
              <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-lg bg-[var(--accent-soft)] text-[var(--link)]">
                <CreditCard className="h-4 w-4" strokeWidth={2} />
              </span>
              <div className="min-w-0">
                <p className="text-xs font-medium uppercase tracking-wide text-[var(--muted)]">
                  Credit limits
                </p>
                <p className="mt-0.5 text-lg font-semibold tabular-nums">
                  {formatCurrencyDetailed(utilization.result.totalCreditLimits)}
                </p>
              </div>
            </div>
          </div>
          {(() => {
            const overLimit = utilization.result.cards.filter(
              (card) =>
                card.creditLimit > 0 &&
                card.currentBalance > card.creditLimit + 0.005,
            );
            if (overLimit.length === 0) return null;
            return (
              <p className="mt-3 rounded-xl border border-amber-200 bg-amber-50 px-3 py-2 text-sm text-amber-950">
                Overall utilization is above 100% because{" "}
                {overLimit
                  .map(
                    (card) =>
                      `${card.name} (${formatCurrencyDetailed(card.currentBalance)} balance vs ${formatCurrencyDetailed(card.creditLimit)} limit)`,
                  )
                  .join("; ")}
                . Update the limit on{" "}
                <Link
                  href="/credit-cards/grid"
                  className="font-medium text-[var(--link)] hover:underline"
                >
                  Credit Card Details
                </Link>{" "}
                if that looks wrong.
              </p>
            );
          })()}
        </Card>
      ) : null}

      <Card className="p-5">
        <div className="grid grid-cols-[minmax(0,1.6fr)_minmax(5.5rem,1fr)_minmax(3.5rem,0.7fr)_minmax(6.5rem,1fr)] items-center gap-x-3 gap-y-1">
          <div className="flex min-w-0 items-center justify-between gap-3">
            <h3 className="text-sm font-semibold text-[var(--foreground)]">
              Cards Analyzed{" "}
              <Link
                href="/credit-cards/grid"
                className="font-medium text-[var(--link)] hover:underline"
              >
                (Edit)
              </Link>
            </h3>
          </div>
          <p className="justify-self-start text-left text-xs font-medium uppercase tracking-wide text-[var(--muted)]">
            Balance
          </p>
          <p className="justify-self-start text-left text-xs font-medium uppercase tracking-wide text-[var(--muted)]">
            APR
          </p>
          <p className="justify-self-start text-left text-xs font-medium uppercase tracking-wide text-[var(--muted)]">
            Minimum Payment
          </p>

          {includedCards.length === 0 ? (
            <p className="col-span-4 mt-2 text-sm text-[var(--muted)]">
              {activeCards.length === 0
                ? "No active credit card balances found."
                : "All cards with a balance are excluded from payoff analysis."}
            </p>
          ) : (
            includedCards.map((card) => (
              <div
                key={card.accountId}
                className="col-span-4 grid grid-cols-subgrid items-center border-t border-[var(--border)] py-2 text-sm"
              >
                <span className="flex min-w-0 items-center gap-2.5 font-medium">
                  <CompanyLogo
                    name={card.name}
                    accountId={card.accountId}
                    size={28}
                  />
                  <span className="truncate">{card.name}</span>
                </span>
                <span className="justify-self-start text-left tabular-nums text-[var(--muted)]">
                  {formatCurrencyDetailed(card.balance)}
                </span>
                <span className="justify-self-start text-left tabular-nums text-[var(--muted)]">
                  {hasInterestRate(card) ? `${card.interestRate}%` : "—"}
                </span>
                <span className="justify-self-start text-left tabular-nums text-[var(--muted)]">
                  {card.monthlyPayment != null
                    ? formatCurrencyDetailed(card.monthlyPayment)
                    : "—"}
                </span>
              </div>
            ))
          )}
        </div>
      </Card>

      {activeCards.length > 0 ? (
        <Card className="p-5">
          <button
            type="button"
            onClick={() => setExcludeSectionOpen((open) => !open)}
            aria-expanded={excludeSectionOpen}
            className="flex w-full items-center justify-between gap-3 text-left"
          >
            <span>
              <span className="block text-sm font-semibold text-[var(--foreground)]">
                Exclude from payoff
              </span>
              {!excludeSectionOpen ? (
                <span className="mt-0.5 block text-xs text-[var(--muted)]">
                  {excludedCards.length === 0
                    ? "No cards excluded"
                    : `${excludedCards.length} card${excludedCards.length === 1 ? "" : "s"} excluded`}
                </span>
              ) : null}
            </span>
            <ChevronDown
              className={`h-4 w-4 shrink-0 text-[var(--muted)] transition-transform ${
                excludeSectionOpen ? "rotate-180" : ""
              }`}
              aria-hidden
            />
          </button>

          {excludeSectionOpen ? (
            <div className="mt-3">
              <p className="text-sm text-[var(--muted)]">
                Use this for cards already on a creditor payment plan. Excluded
                cards stay on your account list and in utilization, but are left
                out of avalanche/snowball.
              </p>

              <form
                className="mt-4 flex flex-wrap items-end gap-3"
                onSubmit={(event) => void handleExcludeSelected(event)}
              >
                <label className="block min-w-[220px] flex-1 text-sm">
                  <span className="mb-1.5 block font-medium">Credit card</span>
                  <select
                    value={selectedToExclude}
                    onChange={(event) => setSelectedToExclude(event.target.value)}
                    disabled={savingExclude || includedCards.length === 0}
                    className="w-full rounded-xl border border-[var(--border)] bg-white px-3 py-2.5 text-sm"
                  >
                    <option value="">Select a card to exclude…</option>
                    {includedCards.map((card) => (
                      <option key={card.accountId} value={card.accountId}>
                        {card.name} · {formatCurrencyDetailed(card.balance)}
                      </option>
                    ))}
                  </select>
                </label>
                <button
                  type="submit"
                  disabled={
                    savingExclude ||
                    !selectedToExclude ||
                    includedCards.length === 0
                  }
                  className="rounded-full bg-[var(--link)] px-4 py-2 text-sm font-semibold text-white disabled:opacity-40"
                >
                  {savingExclude ? "Saving…" : "Exclude"}
                </button>
              </form>

              {excludedCards.length > 0 ? (
                <ul className="mt-4 space-y-2">
                  {excludedCards.map((card) => (
                    <li
                      key={card.accountId}
                      className="flex flex-wrap items-center justify-between gap-2 rounded-xl border border-[var(--border)] bg-black/[0.02] px-3 py-2 text-sm"
                    >
                      <span className="flex min-w-0 items-center gap-2.5">
                        <CompanyLogo
                          name={card.name}
                          accountId={card.accountId}
                          size={28}
                        />
                        <span className="min-w-0">
                          <span className="font-medium">{card.name}</span>
                          <span className="text-[var(--muted)]">
                            {" "}
                            · {formatCurrencyDetailed(card.balance)} · excluded
                          </span>
                        </span>
                      </span>
                      <button
                        type="button"
                        disabled={savingExclude}
                        onClick={() => void setIncludeInPayoff(card, true)}
                        className="text-sm font-medium text-[var(--link)] hover:underline disabled:opacity-40"
                      >
                        Include again
                      </button>
                    </li>
                  ))}
                </ul>
              ) : (
                <p className="mt-3 text-sm text-[var(--muted)]">
                  No cards are currently excluded.
                </p>
              )}

              {excludeStatus ? (
                <p className="mt-3 rounded-xl border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-900">
                  {excludeStatus}
                </p>
              ) : null}
            </div>
          ) : null}
        </Card>
      ) : null}

      {showRateStep ? (
        <Card className="p-5">
          <h3 className="text-sm font-semibold text-[var(--foreground)]">
            {editingRates
              ? "Confirm purchase APR for each included card"
              : "Enter purchase APR for each included card"}
          </h3>
          <p className="mt-1 text-sm text-[var(--muted)]">
            Avalanche vs snowball needs an interest rate on every card included
            in the analysis. Rates are saved to the card.
          </p>
          <form
            className="mt-4 space-y-4"
            onSubmit={(event) => void handleSaveRates(event)}
          >
            <ul className="space-y-3">
              {rateFormCards.map((card) => (
                <li
                  key={card.accountId}
                  className="flex flex-wrap items-end justify-between gap-3"
                >
                  <div className="flex min-w-0 flex-1 items-center gap-2.5">
                    <CompanyLogo
                      name={card.name}
                      accountId={card.accountId}
                      size={32}
                    />
                    <div className="min-w-0">
                      <p className="text-sm font-medium">{card.name}</p>
                      <p className="text-xs text-[var(--muted)]">
                        Balance {formatCurrencyDetailed(card.balance)}
                      </p>
                    </div>
                  </div>
                  <label className="block text-sm">
                    <span className="mb-1.5 block font-medium">APR %</span>
                    <input
                      type="number"
                      min="0"
                      step="0.01"
                      inputMode="decimal"
                      placeholder="e.g. 22.99"
                      required
                      value={aprDrafts[card.accountId] ?? ""}
                      onChange={(event) =>
                        setAprDrafts((prev) => ({
                          ...prev,
                          [card.accountId]: event.target.value,
                        }))
                      }
                      disabled={savingRates}
                      className="w-32 rounded-xl border border-[var(--border)] bg-white px-3 py-2.5 text-sm"
                    />
                  </label>
                </li>
              ))}
            </ul>

            <div className="flex flex-wrap items-center gap-3">
              <button
                type="submit"
                disabled={savingRates}
                className="rounded-full bg-[var(--link)] px-4 py-2 text-sm font-semibold text-white disabled:opacity-40"
              >
                {savingRates ? "Saving…" : "Save APRs & continue"}
              </button>
              {editingRates && ratesReady ? (
                <button
                  type="button"
                  disabled={savingRates}
                  onClick={() => setEditingRates(false)}
                  className="text-sm font-medium text-[var(--muted)] hover:underline"
                >
                  Cancel
                </button>
              ) : null}
            </div>

            {rateStatus ? (
              <p className="rounded-xl border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-900">
                {rateStatus}
              </p>
            ) : null}
          </form>
        </Card>
      ) : null}

      {ratesReady && !editingRates ? (
        <>
          <Card className="p-5">
            <form
              className="space-y-4"
              onSubmit={(event) => void handleShowCurrentPlan(event)}
            >
              <div className="space-y-4">
                <p className="text-sm font-medium">Payoff plan setup</p>
                <ol className="space-y-3">
                  <li className="flex gap-3 rounded-xl border border-[var(--border)] px-3 py-3">
                    <span className="mt-0.5 flex h-6 w-6 shrink-0 items-center justify-center rounded-full bg-[var(--link)] text-xs font-semibold text-white">
                      1
                    </span>
                    <div className="min-w-0 flex-1 text-sm">
                      <p className="mb-1 font-medium">What&apos;s your goal?</p>
                      <p className="mb-2 text-xs text-[var(--muted)]">
                        We&apos;ll suggest a payment order and utilization
                        settings. You can change them in the steps below.
                      </p>
                      <div
                        className="grid gap-2 sm:grid-cols-3"
                        role="radiogroup"
                        aria-label="Payoff goal"
                      >
                        {GOAL_OPTIONS.map((option) => {
                          const selected = selectedGoal === option.value;
                          return (
                            <button
                              key={option.value}
                              type="button"
                              role="radio"
                              aria-checked={selected}
                              disabled={comparing}
                              onClick={() => applyGoalPresets(option.value)}
                              className={`rounded-xl border px-3 py-3 text-left text-sm transition disabled:cursor-not-allowed disabled:opacity-45 ${
                                selected
                                  ? "border-[var(--link)] bg-[var(--link)]/10 ring-1 ring-[var(--link)]"
                                  : "border-[var(--border)] bg-white hover:bg-[var(--accent-soft)]"
                              }`}
                            >
                              <span className="font-semibold">
                                {option.label}
                              </span>
                              <span className="mt-0.5 block text-xs text-[var(--muted)]">
                                {option.description}
                              </span>
                            </button>
                          );
                        })}
                      </div>
                    </div>
                  </li>

                  <li className="flex gap-3 rounded-xl border border-[var(--border)] px-3 py-3">
                    <span className="mt-0.5 flex h-6 w-6 shrink-0 items-center justify-center rounded-full bg-[var(--link)] text-xs font-semibold text-white">
                      2
                    </span>
                    <div className="min-w-0 flex-1 space-y-3 text-sm">
                      <div>
                        <p className="mb-1 font-medium">Consider a loan</p>
                        <p className="text-xs text-[var(--muted)]">
                          Optional. Choose a loan type, enter amount and APR,
                          then review the repayment schedule and interest cost.
                          Apply proceeds with Avalanche, Snowball, or specific
                          accounts. Leave amount blank to skip. Balance
                          transfers stay in step 7.
                        </p>
                      </div>
                      <div className="flex flex-wrap gap-4">
                        <label className="block">
                          <span className="mb-1.5 block text-xs font-medium text-[var(--muted)]">
                            Loan amount
                          </span>
                          <input
                            type="text"
                            inputMode="decimal"
                            placeholder="Optional"
                            value={loanAmount}
                            onChange={(event) => {
                              setLoanAmount(
                                sanitizeMoneyInput(event.target.value),
                              );
                              clearResults();
                            }}
                            disabled={comparing}
                            className="w-full max-w-xs rounded-xl border border-[var(--border)] bg-white px-3 py-2.5 text-sm"
                          />
                        </label>
                        <label className="block">
                          <span className="mb-1.5 block text-xs font-medium text-[var(--muted)]">
                            Loan interest rate (APR %)
                          </span>
                          <input
                            type="text"
                            inputMode="decimal"
                            placeholder="e.g. 9.99"
                            value={loanApr}
                            onChange={(event) => {
                              setLoanApr(
                                sanitizeMoneyInput(event.target.value),
                              );
                              clearResults();
                            }}
                            disabled={comparing || loanAmount.trim() === ""}
                            className="w-full max-w-[8rem] rounded-xl border border-[var(--border)] bg-white px-3 py-2.5 text-sm"
                          />
                        </label>
                      </div>
                      <div className="space-y-2">
                        <p className="text-xs font-medium text-[var(--muted)]">
                          Loan type
                        </p>
                        <div
                          className="grid gap-2 sm:grid-cols-2 lg:grid-cols-3"
                          role="radiogroup"
                          aria-label="Loan type"
                        >
                          {LOAN_TYPE_OPTIONS.map((option) => {
                            const selected = loanType === option.value;
                            return (
                              <button
                                key={option.value}
                                type="button"
                                role="radio"
                                aria-checked={selected}
                                disabled={comparing}
                                onClick={() => {
                                  setLoanType(option.value);
                                  if (
                                    option.value === "HomeEquity" &&
                                    (!loanTermMonths.trim() ||
                                      Number(loanTermMonths) < 60)
                                  ) {
                                    setLoanTermMonths("120");
                                  }
                                  if (
                                    option.value === "Personal" &&
                                    Number(loanTermMonths) > 84
                                  ) {
                                    setLoanTermMonths("36");
                                  }
                                  clearResults();
                                }}
                                className={`rounded-xl border px-3 py-3 text-left text-sm transition disabled:cursor-not-allowed disabled:opacity-45 ${
                                  selected
                                    ? "border-[var(--link)] bg-[var(--link)]/10 ring-1 ring-[var(--link)]"
                                    : "border-[var(--border)] bg-white hover:bg-[var(--accent-soft)]"
                                }`}
                              >
                                <span className="font-semibold">
                                  {option.label}
                                </span>
                                <span className="mt-0.5 block text-xs text-[var(--muted)]">
                                  {option.description}
                                </span>
                              </button>
                            );
                          })}
                        </div>
                      </div>
                      {loanAmount.trim() !== "" ? (
                        <div className="flex flex-wrap gap-4">
                          {loanType === "Family" ? (
                            <label className="block">
                              <span className="mb-1.5 block text-xs font-medium text-[var(--muted)]">
                                Fixed monthly payment
                              </span>
                              <input
                                type="text"
                                inputMode="decimal"
                                value={loanFixedMonthlyPayment}
                                onChange={(event) => {
                                  setLoanFixedMonthlyPayment(
                                    sanitizeMoneyInput(event.target.value),
                                  );
                                  clearResults();
                                }}
                                disabled={comparing}
                                className="w-full max-w-[10rem] rounded-xl border border-[var(--border)] bg-white px-3 py-2.5 text-sm"
                              />
                            </label>
                          ) : (
                            <>
                              <label className="block">
                                <span className="mb-1.5 block text-xs font-medium text-[var(--muted)]">
                                  Term (months)
                                </span>
                                <input
                                  type="number"
                                  min="1"
                                  step="1"
                                  inputMode="numeric"
                                  value={loanTermMonths}
                                  onChange={(event) => {
                                    setLoanTermMonths(event.target.value);
                                    clearResults();
                                  }}
                                  disabled={comparing}
                                  className="w-full max-w-[8rem] rounded-xl border border-[var(--border)] bg-white px-3 py-2.5 text-sm"
                                />
                              </label>
                              {loanType === "Heloc" ? (
                                <label className="block">
                                  <span className="mb-1.5 block text-xs font-medium text-[var(--muted)]">
                                    Interest-only months
                                  </span>
                                  <input
                                    type="number"
                                    min="0"
                                    step="1"
                                    inputMode="numeric"
                                    value={loanInterestOnlyMonths}
                                    onChange={(event) => {
                                      setLoanInterestOnlyMonths(
                                        event.target.value,
                                      );
                                      clearResults();
                                    }}
                                    disabled={comparing}
                                    className="w-full max-w-[8rem] rounded-xl border border-[var(--border)] bg-white px-3 py-2.5 text-sm"
                                  />
                                </label>
                              ) : null}
                            </>
                          )}
                        </div>
                      ) : null}
                      <div className="space-y-2">
                        <p className="text-xs font-medium text-[var(--muted)]">
                          Apply loan proceeds using
                        </p>
                        <div
                          className="grid gap-2 sm:grid-cols-3"
                          role="radiogroup"
                          aria-label="Loan apply strategy"
                        >
                          {(
                            [
                              {
                                value: "Avalanche" as const,
                                label: "Avalanche",
                                description:
                                  "Highest APR cards first — usually saves the most interest.",
                              },
                              {
                                value: "Snowball" as const,
                                label: "Snowball",
                                description:
                                  "Lowest balance cards first — builds quick wins.",
                              },
                              {
                                value: "SelectedAccounts" as const,
                                label: "Specific accounts",
                                description:
                                  "Choose which cards receive the loan funds.",
                              },
                            ] as const
                          ).map((option) => {
                            const selected = loanApplyStrategy === option.value;
                            return (
                              <button
                                key={option.value}
                                type="button"
                                role="radio"
                                aria-checked={selected}
                                disabled={comparing}
                                onClick={() => {
                                  setLoanApplyStrategy(option.value);
                                  clearResults();
                                }}
                                className={`rounded-xl border px-3 py-3 text-left text-sm transition disabled:cursor-not-allowed disabled:opacity-45 ${
                                  selected
                                    ? "border-[var(--link)] bg-[var(--link)]/10 ring-1 ring-[var(--link)]"
                                    : "border-[var(--border)] bg-white hover:bg-[var(--accent-soft)]"
                                }`}
                              >
                                <span className="font-semibold">
                                  {option.label}
                                </span>
                                <span className="mt-0.5 block text-xs text-[var(--muted)]">
                                  {option.description}
                                </span>
                              </button>
                            );
                          })}
                        </div>
                        {loanApplyStrategy === "SelectedAccounts" ? (
                          <div className="space-y-2 rounded-xl border border-[var(--border)] bg-white p-3">
                            <p className="text-xs font-medium text-[var(--muted)]">
                              Select accounts (funds apply in this list order)
                            </p>
                            {includedCards.length === 0 ? (
                              <p className="text-xs text-[var(--muted)]">
                                No included credit cards available.
                              </p>
                            ) : (
                              <ul className="space-y-2">
                                {includedCards.map((card) => {
                                  const checked =
                                    loanApplyCreditCardIds.includes(
                                      card.accountId,
                                    );
                                  return (
                                    <li key={card.accountId}>
                                      <label className="flex cursor-pointer items-start gap-2 text-sm">
                                        <input
                                          type="checkbox"
                                          className="mt-1"
                                          checked={checked}
                                          disabled={comparing}
                                          onChange={(event) => {
                                            setLoanApplyCreditCardIds(
                                              (current) => {
                                                if (event.target.checked) {
                                                  if (
                                                    current.includes(
                                                      card.accountId,
                                                    )
                                                  ) {
                                                    return current;
                                                  }
                                                  return [
                                                    ...current,
                                                    card.accountId,
                                                  ];
                                                }
                                                return current.filter(
                                                  (id) =>
                                                    id !== card.accountId,
                                                );
                                              },
                                            );
                                            clearResults();
                                          }}
                                        />
                                        <span>
                                          <span className="font-medium">
                                            {card.name}
                                          </span>
                                          <span className="mt-0.5 block text-xs text-[var(--muted)]">
                                            Balance{" "}
                                            {formatCurrencyDetailed(
                                              card.balance,
                                            )}
                                            {hasInterestRate(card)
                                              ? ` · ${card.interestRate}% APR`
                                              : ""}
                                          </span>
                                        </span>
                                      </label>
                                    </li>
                                  );
                                })}
                              </ul>
                            )}
                          </div>
                        ) : null}
                      </div>
                      {loanAmount.trim() !== "" ? (
                        <div className="rounded-xl border border-[var(--border)] bg-[var(--accent-soft)]/40 p-3">
                          <p className="mb-2 text-sm font-medium">
                            Loan repayment preview
                          </p>
                          {loanScheduleLoading ? (
                            <p className="text-xs text-[var(--muted)]">
                              Calculating schedule…
                            </p>
                          ) : loanScheduleError ? (
                            <p className="text-xs text-red-700">
                              {loanScheduleError}
                            </p>
                          ) : loanSchedule ? (
                            <div className="space-y-3">
                              <div className="grid gap-2 sm:grid-cols-2 lg:grid-cols-4 text-sm">
                                <div>
                                  <p className="text-xs text-[var(--muted)]">
                                    Monthly payment
                                  </p>
                                  <p className="font-semibold">
                                    {formatCurrencyDetailed(
                                      loanSchedule.monthlyPayment,
                                    )}
                                    {loanSchedule.phase2MonthlyPayment != null
                                      ? ` → ${formatCurrencyDetailed(loanSchedule.phase2MonthlyPayment)}`
                                      : ""}
                                  </p>
                                </div>
                                <div>
                                  <p className="text-xs text-[var(--muted)]">
                                    Months to pay off
                                  </p>
                                  <p className="font-semibold">
                                    {loanSchedule.monthsToPayoff}
                                  </p>
                                </div>
                                <div>
                                  <p className="text-xs text-[var(--muted)]">
                                    Total interest
                                  </p>
                                  <p className="font-semibold">
                                    {formatCurrencyDetailed(
                                      loanSchedule.totalInterest,
                                    )}
                                  </p>
                                </div>
                                <div>
                                  <p className="text-xs text-[var(--muted)]">
                                    Total cost
                                  </p>
                                  <p className="font-semibold">
                                    {formatCurrencyDetailed(
                                      loanSchedule.totalPaid,
                                    )}
                                  </p>
                                </div>
                              </div>
                              <div className="max-h-56 overflow-auto rounded-lg border border-[var(--border)] bg-white">
                                <table className="min-w-full text-left text-xs">
                                  <thead className="sticky top-0 bg-white">
                                    <tr className="border-b border-[var(--border)] text-[var(--muted)]">
                                      <th className="px-2 py-1.5 font-medium">
                                        Month
                                      </th>
                                      <th className="px-2 py-1.5 font-medium">
                                        Payment
                                      </th>
                                      <th className="px-2 py-1.5 font-medium">
                                        Interest
                                      </th>
                                      <th className="px-2 py-1.5 font-medium">
                                        Principal
                                      </th>
                                      <th className="px-2 py-1.5 font-medium">
                                        Balance
                                      </th>
                                    </tr>
                                  </thead>
                                  <tbody>
                                    {loanSchedule.schedule.map((row) => (
                                      <tr
                                        key={row.monthNumber}
                                        className="border-b border-[var(--border)]/60"
                                      >
                                        <td className="px-2 py-1">
                                          {row.monthNumber}
                                        </td>
                                        <td className="px-2 py-1">
                                          {formatCurrencyDetailed(row.payment)}
                                        </td>
                                        <td className="px-2 py-1">
                                          {formatCurrencyDetailed(row.interest)}
                                        </td>
                                        <td className="px-2 py-1">
                                          {formatCurrencyDetailed(
                                            row.principal,
                                          )}
                                        </td>
                                        <td className="px-2 py-1">
                                          {formatCurrencyDetailed(
                                            row.endingBalance,
                                          )}
                                        </td>
                                      </tr>
                                    ))}
                                  </tbody>
                                </table>
                              </div>
                            </div>
                          ) : (
                            <p className="text-xs text-[var(--muted)]">
                              Enter term or payment details to see the schedule.
                            </p>
                          )}
                          <div className="flex flex-wrap items-center gap-3 pt-1">
                            <button
                              type="button"
                              onClick={() => void handleShowLoanSavings()}
                              disabled={
                                comparing ||
                                loanSavingsLoading ||
                                loanAmount.trim() === ""
                              }
                              className="rounded-full bg-[var(--link)] px-4 py-2 text-sm font-semibold text-white disabled:opacity-40"
                            >
                              {loanSavingsLoading
                                ? "Showing savings…"
                                : "Show savings"}
                            </button>
                            <p className="text-xs text-[var(--muted)]">
                              Compare taking this loan vs continuing your
                              current monthly card payments
                              {selectedRepaymentType
                                ? ` (${selectedRepaymentType}).`
                                : " (uses Avalanche until you pick a payment order)."}
                            </p>
                          </div>
                          {loanSavingsError ? (
                            <p className="text-xs text-red-700">
                              {loanSavingsError}
                            </p>
                          ) : null}
                          {loanSavings ? (
                            <div className="space-y-3 rounded-xl border border-[var(--border)] bg-white p-3">
                              <p className="text-sm font-medium">
                                Loan vs monthly payments
                              </p>
                              <p className="text-sm text-[var(--foreground)]">
                                {loanSavings.summary}
                              </p>
                              <div className="grid gap-3 sm:grid-cols-3 text-sm">
                                <div>
                                  <p className="text-xs text-[var(--muted)]">
                                    Interest saved
                                  </p>
                                  <p
                                    className={`font-semibold ${
                                      loanSavings.interestSaved > 0
                                        ? "text-emerald-700"
                                        : loanSavings.interestSaved < 0
                                          ? "text-red-700"
                                          : ""
                                    }`}
                                  >
                                    {formatCurrencyDetailed(
                                      loanSavings.interestSaved,
                                    )}
                                  </p>
                                </div>
                                <div>
                                  <p className="text-xs text-[var(--muted)]">
                                    Months saved
                                  </p>
                                  <p className="font-semibold">
                                    {loanSavings.monthsSaved}
                                  </p>
                                </div>
                                <div>
                                  <p className="text-xs text-[var(--muted)]">
                                    Total paid saved
                                  </p>
                                  <p className="font-semibold">
                                    {formatCurrencyDetailed(
                                      loanSavings.totalPaidSaved,
                                    )}
                                  </p>
                                </div>
                              </div>
                              <div className="grid gap-3 sm:grid-cols-2 text-sm">
                                {(
                                  [
                                    loanSavings.withoutLoan,
                                    loanSavings.withLoan,
                                  ] as const
                                ).map((scenario) => (
                                  <div
                                    key={scenario.label}
                                    className="rounded-lg border border-[var(--border)] p-3"
                                  >
                                    <p className="font-medium">
                                      {scenario.label}
                                    </p>
                                    <p className="mt-0.5 text-xs text-[var(--muted)]">
                                      {displayStrategyName(scenario.strategy)}
                                      {!scenario.isValid
                                        ? " · incomplete payoff"
                                        : ""}
                                    </p>
                                    <dl className="mt-2 space-y-1 text-xs">
                                      <div className="flex justify-between gap-2">
                                        <dt className="text-[var(--muted)]">
                                          Total interest
                                        </dt>
                                        <dd className="font-medium">
                                          {formatCurrencyDetailed(
                                            scenario.totalInterest,
                                          )}
                                        </dd>
                                      </div>
                                      <div className="flex justify-between gap-2">
                                        <dt className="text-[var(--muted)]">
                                          Total paid
                                        </dt>
                                        <dd className="font-medium">
                                          {formatCurrencyDetailed(
                                            scenario.totalPaid,
                                          )}
                                        </dd>
                                      </div>
                                      <div className="flex justify-between gap-2">
                                        <dt className="text-[var(--muted)]">
                                          Months
                                        </dt>
                                        <dd className="font-medium">
                                          {scenario.monthsToPayoff}
                                        </dd>
                                      </div>
                                      <div className="flex justify-between gap-2">
                                        <dt className="text-[var(--muted)]">
                                          Debt-free
                                        </dt>
                                        <dd className="font-medium">
                                          {formatPayoffDate(
                                            scenario.estimatedPayoffDate,
                                          )}
                                        </dd>
                                      </div>
                                    </dl>
                                  </div>
                                ))}
                              </div>
                            </div>
                          ) : null}
                        </div>
                      ) : null}
                    </div>
                  </li>

                  <li className="flex gap-3 rounded-xl border border-[var(--border)] px-3 py-3">
                    <span className="mt-0.5 flex h-6 w-6 shrink-0 items-center justify-center rounded-full bg-[var(--link)] text-xs font-semibold text-white">
                      3
                    </span>
                    <div className="min-w-0 flex-1 text-sm">
                      <p className="mb-1 font-medium">
                        Determine payment order
                      </p>
                      <p className="mb-2 text-xs text-[var(--muted)]">
                        Choose one order for this plan. Avalanche and Snowball
                        decide Extra routing; Minimums is a baseline with no
                        Extra routing. Save up to {MAX_COMPARE_PLANS} named plans
                        to compare.
                      </p>
                      <div
                        className="grid gap-2 sm:grid-cols-3"
                        role="radiogroup"
                        aria-label="Payment repayment type"
                      >
                        {REPAYMENT_OPTIONS.map((option) => {
                          const selected =
                            selectedRepaymentType === option.value;
                          return (
                            <button
                              key={option.value}
                              type="button"
                              role="radio"
                              aria-checked={selected}
                              disabled={comparing || option.disabled}
                              onClick={() => {
                                setSelectedRepaymentType(option.value);
                                clearResults();
                              }}
                              className={`rounded-xl border px-3 py-3 text-left text-sm transition disabled:cursor-not-allowed disabled:opacity-45 ${
                                selected
                                  ? "border-[var(--link)] bg-[var(--link)]/10 ring-1 ring-[var(--link)]"
                                  : "border-[var(--border)] bg-white hover:bg-[var(--accent-soft)]"
                              }`}
                            >
                              <span className="font-semibold">
                                {option.value}
                              </span>
                              <span className="mt-0.5 block text-xs text-[var(--muted)]">
                                {option.description}
                              </span>
                            </button>
                          );
                        })}
                      </div>
                    </div>
                  </li>

                  <li className="flex gap-3 rounded-xl border border-[var(--border)] px-3 py-3">
                    <span className="mt-0.5 flex h-6 w-6 shrink-0 items-center justify-center rounded-full bg-[var(--link)] text-xs font-semibold text-white">
                      4
                    </span>
                    <div className="min-w-0 flex-1 space-y-3 text-sm">
                      <div>
                        <p className="mb-1 font-medium">
                          Configure additional funds
                        </p>
                        <p className="text-xs text-[var(--muted)]">
                          Extra money applied on top of each card&apos;s
                          minimum payment, routed by the order from step 3.
                        </p>
                      </div>
                      <div className="flex flex-wrap gap-4">
                        <div className="block">
                          <span className="mb-1.5 block text-xs font-medium text-[var(--muted)]">
                            Combined minimums
                          </span>
                          <p className="text-lg font-semibold tabular-nums">
                            {formatCurrencyDetailed(defaultMonthly)}
                          </p>
                        </div>
                        <label className="block">
                          <span className="mb-1.5 block text-xs font-medium text-[var(--muted)]">
                            Extra monthly payment
                          </span>
                          <input
                            type="text"
                            inputMode="decimal"
                            placeholder="0"
                            value={extraMonthlyPayment}
                            onChange={(event) => {
                              setExtraMonthlyPayment(
                                sanitizeMoneyInput(event.target.value),
                              );
                              clearResults();
                            }}
                            disabled={comparing}
                            className="w-full max-w-xs rounded-xl border border-[var(--border)] bg-white px-3 py-2.5 text-sm"
                          />
                        </label>
                        <div className="block">
                          <span className="mb-1.5 block text-xs font-medium text-[var(--muted)]">
                            Total monthly debt payment
                          </span>
                          <p className="text-lg font-semibold tabular-nums">
                            {formatCurrencyDetailed(totalMonthlyPayment)}
                          </p>
                        </div>
                      </div>
                    </div>
                  </li>

                  <li className="flex gap-3 rounded-xl border border-[var(--border)] px-3 py-3">
                    <span className="mt-0.5 flex h-6 w-6 shrink-0 items-center justify-center rounded-full bg-[var(--link)] text-xs font-semibold text-white">
                      5
                    </span>
                    <div className="min-w-0 flex-1 space-y-3 text-sm">
                      <div>
                        <p className="mb-1 font-medium">
                          Target utilization for all cards
                        </p>
                        <p className="text-xs text-[var(--muted)]">
                          Extra first brings each card down to this utilization
                          %. When a card meets the target, Extra moves to the
                          next card. After every card meets the target, choose
                          Avalanche or Snowball below to finish paying to $0
                          (or leave both unchecked to keep step 3&apos;s order).
                          Leave the % blank to skip.
                        </p>
                      </div>
                      <label className="block">
                        <span className="mb-1.5 block text-xs font-medium text-[var(--muted)]">
                          Target utilization %
                        </span>
                        <input
                          type="number"
                          min="1"
                          max="99"
                          step="1"
                          value={targetUtilization}
                          onChange={(event) => {
                            setTargetUtilization(event.target.value);
                            if (event.target.value.trim() === "") {
                              setPostUtilizationStrategy(null);
                            }
                            clearResults();
                          }}
                          disabled={comparing}
                          placeholder="e.g. 30"
                          className="w-full max-w-[8rem] rounded-xl border border-[var(--border)] bg-white px-3 py-2.5 text-sm"
                        />
                      </label>
                      <div className="space-y-2">
                        <p className="text-xs font-medium text-[var(--muted)]">
                          After utilization is met
                        </p>
                        <label className="flex cursor-pointer items-start gap-2">
                          <input
                            type="checkbox"
                            checked={postUtilizationStrategy === "Snowball"}
                            onChange={(event) => {
                              setPostUtilizationStrategy(
                                event.target.checked ? "Snowball" : null,
                              );
                              clearResults();
                            }}
                            disabled={
                              comparing || targetUtilization.trim() === ""
                            }
                            className="mt-1"
                          />
                          <span className="text-xs text-[var(--muted)]">
                            <span className="font-medium text-[var(--foreground)]">
                              Snowball after utilization is met
                            </span>
                            <span className="mt-0.5 block">
                              Lowest balance first once every card hits the
                              target %.
                            </span>
                          </span>
                        </label>
                        <label className="flex cursor-pointer items-start gap-2">
                          <input
                            type="checkbox"
                            checked={postUtilizationStrategy === "Avalanche"}
                            onChange={(event) => {
                              setPostUtilizationStrategy(
                                event.target.checked ? "Avalanche" : null,
                              );
                              clearResults();
                            }}
                            disabled={
                              comparing || targetUtilization.trim() === ""
                            }
                            className="mt-1"
                          />
                          <span className="text-xs text-[var(--muted)]">
                            <span className="font-medium text-[var(--foreground)]">
                              Avalanche after utilization is met
                            </span>
                            <span className="mt-0.5 block">
                              Highest APR first once every card hits the target
                              %.
                            </span>
                          </span>
                        </label>
                      </div>
                      <label className="flex cursor-pointer items-start gap-2">
                        <input
                          type="checkbox"
                          checked={payOverLimitFirst}
                          onChange={(event) => {
                            setPayOverLimitFirst(event.target.checked);
                            clearResults();
                          }}
                          disabled={comparing}
                          className="mt-1"
                        />
                        <span className="text-xs text-[var(--muted)]">
                          Also pay over-limit balances first (before the
                          utilization target).
                        </span>
                      </label>
                    </div>
                  </li>

                  <li className="flex gap-3 rounded-xl border border-[var(--border)] px-3 py-3">
                    <span className="mt-0.5 flex h-6 w-6 shrink-0 items-center justify-center rounded-full bg-[var(--link)] text-xs font-semibold text-white">
                      6
                    </span>
                    <label className="flex min-w-0 flex-1 cursor-pointer items-start gap-2 text-sm">
                      <input
                        type="checkbox"
                        checked={enableCashAdvanceBalanceMoves}
                        onChange={(event) => {
                          setEnableCashAdvanceBalanceMoves(
                            event.target.checked,
                          );
                          clearResults();
                        }}
                        disabled={comparing}
                        className="mt-1"
                      />
                      <span>
                        <span className="font-medium">
                          Use cash-advance APR to help pay the focus card
                        </span>
                        <span className="mt-0.5 block text-xs text-[var(--muted)]">
                          If another card has a cash-advance APR lower than the
                          card being paid, use that card&apos;s available credit
                          to pay the focus card (never past the limit). If no
                          card qualifies, use the highest-APR card with
                          available credit instead.
                        </span>
                      </span>
                    </label>
                  </li>

                  <li className="flex gap-3 rounded-xl border border-[var(--border)] px-3 py-3">
                    <span className="mt-0.5 flex h-6 w-6 shrink-0 items-center justify-center rounded-full bg-[var(--link)] text-xs font-semibold text-white">
                      7
                    </span>
                    <div className="min-w-0 flex-1 space-y-3 text-sm">
                      <div>
                        <p className="mb-1 font-medium">
                          Promotional APR balance transfers
                        </p>
                        <p className="text-xs text-[var(--muted)]">
                          Optional. Add one or more promotional transfers
                          (multi-step). Each can run in a different plan month
                          (0 = first month).
                        </p>
                      </div>
                      {promoTransfers.map((row, index) => (
                        <div
                          key={row.id}
                          className="space-y-2 rounded-xl border border-[var(--border)] bg-black/[0.015] p-3"
                        >
                          <div className="flex items-center justify-between gap-2">
                            <p className="text-xs font-semibold uppercase tracking-wide text-[var(--muted)]">
                              Transfer {index + 1}
                            </p>
                            <button
                              type="button"
                              disabled={comparing}
                              onClick={() => {
                                setPromoTransfers((current) =>
                                  current.filter((item) => item.id !== row.id),
                                );
                                clearResults();
                              }}
                              className="text-xs font-medium text-[var(--muted)] hover:text-red-700"
                            >
                              Remove
                            </button>
                          </div>
                          <div className="grid gap-2 sm:grid-cols-2">
                            <label className="block text-xs">
                              <span className="mb-1 block font-medium">
                                From card
                              </span>
                              <select
                                value={row.fromCreditCardId}
                                disabled={comparing}
                                onChange={(event) => {
                                  const value = event.target.value;
                                  setPromoTransfers((current) =>
                                    current.map((item) =>
                                      item.id === row.id
                                        ? { ...item, fromCreditCardId: value }
                                        : item,
                                    ),
                                  );
                                  clearResults();
                                }}
                                className="w-full rounded-xl border border-[var(--border)] bg-white px-3 py-2 text-sm"
                              >
                                <option value="">Select…</option>
                                {includedCards.map((card) => (
                                  <option
                                    key={card.accountId}
                                    value={card.accountId}
                                  >
                                    {card.name}
                                  </option>
                                ))}
                              </select>
                            </label>
                            <label className="block text-xs">
                              <span className="mb-1 block font-medium">
                                To card (promo APR)
                              </span>
                              <select
                                value={row.toCreditCardId}
                                disabled={comparing}
                                onChange={(event) => {
                                  const value = event.target.value;
                                  setPromoTransfers((current) =>
                                    current.map((item) =>
                                      item.id === row.id
                                        ? { ...item, toCreditCardId: value }
                                        : item,
                                    ),
                                  );
                                  clearResults();
                                }}
                                className="w-full rounded-xl border border-[var(--border)] bg-white px-3 py-2 text-sm"
                              >
                                <option value="">Select…</option>
                                {includedCards.map((card) => (
                                  <option
                                    key={card.accountId}
                                    value={card.accountId}
                                  >
                                    {card.name}
                                  </option>
                                ))}
                              </select>
                            </label>
                            <label className="block text-xs">
                              <span className="mb-1 block font-medium">
                                Amount (blank = max)
                              </span>
                              <input
                                type="text"
                                inputMode="decimal"
                                value={row.amount}
                                disabled={comparing}
                                onChange={(event) => {
                                  const value = sanitizeMoneyInput(
                                    event.target.value,
                                  );
                                  setPromoTransfers((current) =>
                                    current.map((item) =>
                                      item.id === row.id
                                        ? { ...item, amount: value }
                                        : item,
                                    ),
                                  );
                                  clearResults();
                                }}
                                className="w-full rounded-xl border border-[var(--border)] bg-white px-3 py-2 text-sm"
                              />
                            </label>
                            <label className="block text-xs">
                              <span className="mb-1 block font-medium">
                                Promo APR %
                              </span>
                              <input
                                type="number"
                                min="0"
                                step="0.01"
                                value={row.promotionalApr}
                                disabled={comparing}
                                onChange={(event) => {
                                  const value = event.target.value;
                                  setPromoTransfers((current) =>
                                    current.map((item) =>
                                      item.id === row.id
                                        ? { ...item, promotionalApr: value }
                                        : item,
                                    ),
                                  );
                                  clearResults();
                                }}
                                className="w-full rounded-xl border border-[var(--border)] bg-white px-3 py-2 text-sm"
                              />
                            </label>
                            <label className="block text-xs">
                              <span className="mb-1 block font-medium">
                                Promo months
                              </span>
                              <input
                                type="number"
                                min="1"
                                step="1"
                                value={row.promotionalMonths}
                                disabled={comparing}
                                onChange={(event) => {
                                  const value = event.target.value;
                                  setPromoTransfers((current) =>
                                    current.map((item) =>
                                      item.id === row.id
                                        ? {
                                            ...item,
                                            promotionalMonths: value,
                                          }
                                        : item,
                                    ),
                                  );
                                  clearResults();
                                }}
                                className="w-full rounded-xl border border-[var(--border)] bg-white px-3 py-2 text-sm"
                              />
                            </label>
                            <label className="block text-xs">
                              <span className="mb-1 block font-medium">
                                Apply in month # (0 = first)
                              </span>
                              <input
                                type="number"
                                min="0"
                                step="1"
                                value={row.applyAtMonth}
                                disabled={comparing}
                                onChange={(event) => {
                                  const value = event.target.value;
                                  setPromoTransfers((current) =>
                                    current.map((item) =>
                                      item.id === row.id
                                        ? { ...item, applyAtMonth: value }
                                        : item,
                                    ),
                                  );
                                  clearResults();
                                }}
                                className="w-full rounded-xl border border-[var(--border)] bg-white px-3 py-2 text-sm"
                              />
                            </label>
                          </div>
                        </div>
                      ))}
                      <button
                        type="button"
                        disabled={comparing || includedCards.length < 2}
                        onClick={() => {
                          setPromoTransfers((current) => [
                            ...current,
                            newPromoTransferDraft(),
                          ]);
                          clearResults();
                        }}
                        className="rounded-full border border-[var(--border)] px-3 py-1.5 text-xs font-semibold text-[var(--link)] hover:bg-[var(--accent-soft)] disabled:opacity-40"
                      >
                        Add promotional transfer
                      </button>
                    </div>
                  </li>
                </ol>
              </div>

              <div className="flex flex-wrap items-center gap-3">
                <button
                  type="submit"
                  disabled={comparing || selectedRepaymentType == null}
                  className="rounded-full bg-[var(--link)] px-4 py-2 text-sm font-semibold text-white disabled:opacity-40"
                >
                  {comparing && displayPlans.length <= 1
                    ? "Showing plan…"
                    : "Show plan"}
                </button>
                <button
                  type="button"
                  disabled={
                    comparing ||
                    savingPlan ||
                    selectedRepaymentType == null ||
                    compareTray.length >= MAX_COMPARE_PLANS
                  }
                  onClick={() => openAddPlanNaming()}
                  className="rounded-full border border-[var(--border)] px-4 py-2 text-sm font-semibold text-[var(--link)] hover:bg-[var(--accent-soft)] disabled:opacity-40"
                >
                  Add another plan to compare
                </button>
                <button
                  type="button"
                  disabled={comparing || compareTray.length === 0}
                  onClick={() => void handleCompareTrayPlans()}
                  className="rounded-full border border-[var(--link)] px-4 py-2 text-sm font-semibold text-[var(--link)] hover:bg-[var(--link)]/10 disabled:opacity-40"
                >
                  {comparing && displayPlans.length > 1
                    ? "Comparing plans…"
                    : `Compare plans${compareTray.length > 0 ? ` (${compareTray.length})` : ""}`}
                </button>
                <button
                  type="button"
                  disabled={
                    comparing ||
                    startingPlan ||
                    selectedRepaymentType == null
                  }
                  onClick={() => void handleStartThisPlan()}
                  className="rounded-full bg-emerald-700 px-4 py-2 text-sm font-semibold text-white hover:bg-emerald-800 disabled:opacity-40"
                >
                  {startingPlan ? "Starting…" : "Start this plan"}
                </button>
                <Link
                  href="/credit-cards/payoff/active"
                  className="text-sm font-medium text-[var(--link)] hover:underline"
                >
                  View active plan
                </Link>
              </div>

              {namingOpen ? (
                <div className="rounded-xl border border-[var(--border)] bg-black/[0.02] px-3 py-3">
                  <p className="text-sm font-medium">Name this plan</p>
                  <p className="mt-1 text-xs text-[var(--muted)]">
                    Saved to your account and added to the compare tray (max{" "}
                    {MAX_COMPARE_PLANS}).
                  </p>
                  <div className="mt-3 flex flex-wrap items-end gap-3">
                    <label className="block min-w-[16rem] flex-1">
                      <span className="mb-1.5 block text-xs font-medium text-[var(--muted)]">
                        Plan name
                      </span>
                      <input
                        type="text"
                        value={planNameDraft}
                        onChange={(event) =>
                          setPlanNameDraft(event.target.value)
                        }
                        disabled={savingPlan}
                        className="w-full rounded-xl border border-[var(--border)] bg-white px-3 py-2 text-sm"
                      />
                    </label>
                    <button
                      type="button"
                      disabled={savingPlan || !planNameDraft.trim()}
                      onClick={() => void handleSavePlanToCompare()}
                      className="rounded-full bg-[var(--link)] px-4 py-2 text-sm font-semibold text-white disabled:opacity-40"
                    >
                      {savingPlan ? "Saving…" : "Save to compare tray"}
                    </button>
                    <button
                      type="button"
                      disabled={savingPlan}
                      onClick={() => {
                        setNamingOpen(false);
                        setPlanNameDraft("");
                      }}
                      className="text-sm font-medium text-[var(--muted)] hover:underline"
                    >
                      Cancel
                    </button>
                  </div>
                </div>
              ) : null}

              {compareTray.length > 0 ? (
                <div className="rounded-xl border border-[var(--border)] px-3 py-3">
                  <p className="text-sm font-medium">
                    Compare tray ({compareTray.length}/{MAX_COMPARE_PLANS})
                  </p>
                  <ul className="mt-2 space-y-2">
                    {compareTray.map((plan) => (
                      <li
                        key={plan.savedPayoffPlanId}
                        className="flex flex-wrap items-center justify-between gap-2 text-sm"
                      >
                        <span className="min-w-0 flex-1 break-words">
                          {plan.name}
                        </span>
                        <button
                          type="button"
                          disabled={comparing || startingPlan}
                          onClick={() => void handleStartSavedPlan(plan)}
                          className="shrink-0 text-xs font-semibold text-emerald-800 hover:underline disabled:opacity-40"
                        >
                          Start
                        </button>
                        <button
                          type="button"
                          disabled={comparing || savingPlan}
                          onClick={() =>
                            void handleRemoveFromTray(
                              plan.savedPayoffPlanId,
                              true,
                            )
                          }
                          className="shrink-0 text-xs font-semibold text-red-700 hover:underline disabled:opacity-40"
                        >
                          Remove
                        </button>
                      </li>
                    ))}
                  </ul>
                </div>
              ) : null}
            </form>

            {status ? (
              <p className="mt-4 rounded-xl border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-900">
                {status}
              </p>
            ) : null}
          </Card>

          {displayPlans.length > 0 ? (
            <div className="space-y-4">
              <Card className="p-5">
                <p className="text-sm text-[var(--muted)]">
                  {displayPlans.length === 1
                    ? "Showing current setup"
                    : `Comparing ${displayPlans.length} saved plans`}
                  {payOverLimitFirst ? " · Over-limit first" : ""}
                  {enableCashAdvanceBalanceMoves
                    ? " · Cash-advance assist"
                    : ""}
                  {targetUtilization.trim() !== ""
                    ? ` · Target utilization ${targetUtilization.trim()}%`
                    : ""}
                  {postUtilizationStrategy
                    ? ` · After util: ${postUtilizationStrategy}`
                    : ""}
                  {promoTransfers.length > 0
                    ? ` · ${promoTransfers.length} promo transfer${promoTransfers.length === 1 ? "" : "s"}`
                    : ""}
                </p>
                {compareWarnings.length ? (
                  <ul className="mt-3 list-disc space-y-1 pl-5 text-sm text-amber-800">
                    {compareWarnings.map((warning) => (
                      <li key={warning}>{warning}</li>
                    ))}
                  </ul>
                ) : null}
                {isCompareMode &&
                (paymentAtOrBelowMinimums || strategiesHaveIdenticalTotals) ? (
                  <p className="mt-3 rounded-xl border border-amber-200 bg-amber-50 px-3 py-2 text-sm text-amber-900">
                    Avalanche and Snowball only diverge when Extra monthly
                    payment is greater than zero. The plan then routes that
                    extra by repayment order and utilization steps.
                  </p>
                ) : null}
              </Card>

              <div
                className={`grid gap-4 ${
                  displayPlans.length > 2
                    ? "md:grid-cols-3"
                    : displayPlans.length > 1
                      ? "md:grid-cols-2"
                      : ""
                }`}
              >
                {displayPlans.map((plan) => {
                  const strategy = plan.summary;
                  const isSelected = selectedPlanKey === plan.key;
                  return (
                    <Card
                      key={plan.key}
                      className={`p-5 ${
                        isSelected ? "ring-2 ring-[var(--link)]" : ""
                      }`}
                    >
                      <div className="flex items-center justify-between gap-2">
                        <h3 className="text-lg font-semibold break-words">
                          {plan.name}
                        </h3>
                      </div>
                      <p className="mt-1 text-xs text-[var(--muted)]">
                        {displayStrategyName(strategy.strategy)}
                      </p>
                      {!strategy.isValid ? (
                        <p className="mt-3 text-sm text-red-700">
                          Invalid for this payment budget.
                        </p>
                      ) : (
                        <dl className="mt-3 space-y-2 text-sm">
                          <div className="flex justify-between gap-3">
                            <dt className="text-[var(--muted)]">Debt-free</dt>
                            <dd className="font-medium">
                              {formatPayoffDate(strategy.estimatedPayoffDate)}
                            </dd>
                          </div>
                          <div className="flex justify-between gap-3">
                            <dt className="text-[var(--muted)]">Months</dt>
                            <dd className="font-medium">
                              {strategy.monthsToPayoff}
                            </dd>
                          </div>
                          <div className="flex justify-between gap-3">
                            <dt className="text-[var(--muted)]">
                              Total interest
                            </dt>
                            <dd className="font-medium">
                              {formatCurrencyDetailed(strategy.totalInterest)}
                            </dd>
                          </div>
                        </dl>
                      )}
                      <button
                        type="button"
                        disabled={!strategy.isValid}
                        onClick={() => selectPlan(plan)}
                        className="mt-4 rounded-full bg-[var(--link)] px-4 py-2 text-sm font-semibold text-white disabled:opacity-40"
                      >
                        {isSelected ? "Selected plan" : "Choose this plan"}
                      </button>
                    </Card>
                  );
                })}
              </div>

              {selectedStrategy?.isValid ? (
                <Card className="p-5">
                  <h3 className="text-sm font-semibold text-[var(--foreground)]">
                    {displayPlans.find((p) => p.key === selectedPlanKey)
                      ?.name ?? displayStrategyName(selectedStrategy.strategy)}{" "}
                    detail
                  </h3>
                  {firstPaidOffCard ? (
                    <p className="mt-3 rounded-xl border border-[var(--border)] bg-black/[0.02] px-3 py-2 text-sm">
                      <span className="font-medium">First card paid off: </span>
                      {firstPaidOffCard.name}
                      <span className="text-[var(--muted)]">
                        {" "}
                        ·{" "}
                        {formatPayoffDate(firstPaidOffCard.estimatedPayoffDate)}
                      </span>
                    </p>
                  ) : null}
                  {extraFocus ? (
                    <p className="mt-2 rounded-xl border border-[var(--link)]/30 bg-[var(--link)]/5 px-3 py-2 text-sm">
                      <span className="font-medium">
                        Extra payment applied first to:{" "}
                      </span>
                      {extraFocus.card.name}
                      <span className="text-[var(--muted)]">
                        {" "}
                        · {formatCurrencyDetailed(extraFocus.extra)} in month 1
                        (above that card&apos;s minimum)
                      </span>
                    </p>
                  ) : totalExtraPayment > 0 ? (
                    <p className="mt-2 rounded-xl border border-amber-200 bg-amber-50 px-3 py-2 text-sm text-amber-900">
                      No first-month extra was allocated yet (for example during
                      over-limit or utilization steps). Open cards below and
                      check the Extra column in later months.
                    </p>
                  ) : (
                    <p className="mt-2 rounded-xl border border-amber-200 bg-amber-50 px-3 py-2 text-sm text-amber-900">
                      Enter an Extra monthly payment above zero, then run the
                      plan again to see Extra applied.
                    </p>
                  )}
                  <p className="mt-2 text-sm text-[var(--muted)]">
                    Cards are listed in estimated payoff order. Extra usually
                    goes to the strategy&apos;s target card first—not always the
                    card that pays off soonest. Your total monthly payment stays
                    fixed: when a card hits 100%, its minimum rolls into Extra
                    for the remaining cards (see larger Extra values in later
                    months).
                  </p>

                  <PayoffUtilizationTimeline
                    cardOrder={selectedStrategy.cardOrder}
                    limitsByCardId={creditLimitsByCardId}
                    totalCreditLimits={
                      portfolioUtilizationBasis.totalCreditLimits
                    }
                    outsidePlanBalance={
                      portfolioUtilizationBasis.outsidePlanBalance
                    }
                    targetPercent={planUtilizationTargetPercent}
                  />

                  <ul className="mt-4 divide-y divide-[var(--border)]">
                    {cardsByPayoffDate.map((card, index) => {
                      const isOpen = selectedCardId === card.creditCardId;
                      const utilMetMonth = findUtilizationMetMonth(
                        card,
                        creditLimitsByCardId.get(card.creditCardId),
                        planUtilizationTargetPercent,
                      );
                      const paidOffMonth = findPaidOffMonth(card);
                      return (
                        <li key={card.creditCardId}>
                          <button
                            type="button"
                            onClick={() =>
                              setSelectedCardId((current) =>
                                current === card.creditCardId
                                  ? null
                                  : card.creditCardId,
                              )
                            }
                            className="flex w-full items-center justify-between gap-3 py-3 text-left"
                          >
                            <span className="flex min-w-0 items-center gap-2.5">
                              <span className="w-5 shrink-0 text-sm tabular-nums text-[var(--muted)]">
                                {index + 1}.
                              </span>
                              <CompanyLogo
                                name={card.name}
                                accountId={card.creditCardId}
                                size={28}
                              />
                              <span className="min-w-0">
                                <span className="flex min-w-0 flex-wrap items-center gap-x-2 gap-y-1">
                                  <span className="truncate font-medium">
                                    {card.name}
                                  </span>
                                  {utilMetMonth ? (
                                    <span
                                      title={`${planUtilizationTargetPercent}% utilization met`}
                                      className="inline-flex items-center gap-1 rounded-full bg-black/[0.04] px-1.5 py-0.5 text-[11px] tabular-nums text-[var(--muted)]"
                                    >
                                      <Gauge
                                        className="h-3.5 w-3.5 shrink-0 text-[var(--link)]"
                                        strokeWidth={2}
                                        aria-hidden
                                      />
                                      <span className="font-medium text-[var(--foreground)]">
                                        {planUtilizationTargetPercent}%
                                      </span>
                                      <span>
                                        {formatPayoffDate(utilMetMonth)}
                                      </span>
                                    </span>
                                  ) : null}
                                  {paidOffMonth ? (
                                    <span
                                      title="Paid off (100%)"
                                      className="inline-flex items-center gap-1 rounded-full bg-emerald-500/10 px-1.5 py-0.5 text-[11px] tabular-nums text-emerald-800"
                                    >
                                      <CheckCircle2
                                        className="h-3.5 w-3.5 shrink-0"
                                        strokeWidth={2}
                                        aria-hidden
                                      />
                                      <span className="font-medium">100%</span>
                                      <span>
                                        {formatPayoffDate(paidOffMonth)}
                                      </span>
                                    </span>
                                  ) : null}
                                </span>
                                <span className="mt-0.5 block text-xs text-[var(--muted)]">
                                  Interest{" "}
                                  {formatCurrencyDetailed(
                                    card.totalInterestPaid,
                                  )}
                                </span>
                              </span>
                            </span>
                            <span className="shrink-0 text-xs font-medium text-[var(--link)]">
                              {isOpen ? "Hide" : "View months"}
                            </span>
                          </button>

                          {isOpen ? (
                            <div className="mb-4 overflow-x-auto rounded-xl border border-[var(--border)]">
                              {(card.monthlyBalances?.length ?? 0) === 0 ? (
                                <p className="px-3 py-4 text-sm text-[var(--muted)]">
                                  No monthly schedule returned for this card.
                                </p>
                              ) : (
                                <table className="min-w-full text-left text-sm">
                                  <thead className="bg-black/[0.03] text-xs uppercase tracking-wide text-[var(--muted)]">
                                    <tr>
                                      <th className="px-3 py-2 font-medium">
                                        Month
                                      </th>
                                      <th className="px-3 py-2 font-medium">
                                        Starting
                                      </th>
                                      <th className="px-3 py-2 font-medium">
                                        Start util
                                      </th>
                                      <th className="px-3 py-2 font-medium">
                                        Interest
                                      </th>
                                      <th className="px-3 py-2 font-medium">
                                        Payment
                                      </th>
                                      <th className="px-3 py-2 font-medium">
                                        Minimum
                                      </th>
                                      <th className="px-3 py-2 font-medium">
                                        Extra
                                      </th>
                                      <th className="px-3 py-2 font-medium">
                                        BT In
                                      </th>
                                      <th className="px-3 py-2 font-medium">
                                        BT Out
                                      </th>
                                      <th className="px-3 py-2 font-medium">
                                        Ending
                                      </th>
                                      <th className="px-3 py-2 font-medium">
                                        End util
                                      </th>
                                    </tr>
                                  </thead>
                                  <tbody>
                                    {(card.monthlyBalances ?? []).map((row) => {
                                      const limit = creditLimitsByCardId.get(
                                        card.creditCardId,
                                      );
                                      return (
                                      <Fragment
                                        key={`${card.creditCardId}-${row.month}`}
                                      >
                                        <tr className="border-t border-[var(--border)]">
                                          <td className="px-3 py-2 tabular-nums">
                                            {formatPayoffDate(row.month)}
                                          </td>
                                          <td className="px-3 py-2 tabular-nums">
                                            {formatCurrencyDetailed(
                                              row.startingBalance,
                                            )}
                                          </td>
                                          <td className="px-3 py-2 tabular-nums text-[var(--muted)]">
                                            {formatUtilizationPercent(
                                              row.startingBalance,
                                              limit,
                                            )}
                                          </td>
                                          <td className="px-3 py-2 tabular-nums">
                                            {formatCurrencyDetailed(
                                              row.interestCharged,
                                            )}
                                          </td>
                                          <td className="px-3 py-2 tabular-nums">
                                            {formatCurrencyDetailed(
                                              row.paymentApplied,
                                            )}
                                          </td>
                                          <td className="px-3 py-2 tabular-nums">
                                            {formatCurrencyDetailed(
                                              row.minimumPaymentApplied ?? 0,
                                            )}
                                          </td>
                                          <td className="px-3 py-2 tabular-nums">
                                            {formatCurrencyDetailed(
                                              row.extraPaymentApplied ?? 0,
                                            )}
                                          </td>
                                          <td className="px-3 py-2 tabular-nums">
                                            {formatCurrencyDetailed(
                                              row.balanceTransferredIn ?? 0,
                                            )}
                                          </td>
                                          <td className="px-3 py-2 tabular-nums">
                                            {formatCurrencyDetailed(
                                              row.balanceTransferredOut ?? 0,
                                            )}
                                          </td>
                                          <td className="px-3 py-2 font-medium tabular-nums">
                                            {formatCurrencyDetailed(
                                              row.endingBalance,
                                            )}
                                          </td>
                                          <td className="px-3 py-2 font-medium tabular-nums">
                                            {formatUtilizationPercent(
                                              row.endingBalance,
                                              limit,
                                            )}
                                          </td>
                                        </tr>
                                        {(row.transfers?.length ?? 0) > 0 ? (
                                          <tr className="border-t border-[var(--border)] bg-black/[0.015]">
                                            <td
                                              colSpan={11}
                                              className="px-3 py-2 text-xs text-[var(--muted)]"
                                            >
                                              <span className="font-medium text-[var(--foreground)]">
                                                Balance moves:{" "}
                                              </span>
                                              {row.transfers!.map((leg, i) => (
                                                <span
                                                  key={`${leg.direction}-${leg.counterpartyCreditCardId}-${i}`}
                                                >
                                                  {i > 0 ? "; " : ""}
                                                  {leg.direction === "In"
                                                    ? `+${formatCurrencyDetailed(leg.amount)} from ${leg.counterpartyName}`
                                                    : `−${formatCurrencyDetailed(leg.amount)} to ${leg.counterpartyName}`}
                                                </span>
                                              ))}
                                            </td>
                                          </tr>
                                        ) : null}
                                      </Fragment>
                                      );
                                    })}
                                  </tbody>
                                </table>
                              )}
                            </div>
                          ) : null}
                        </li>
                      );
                    })}
                  </ul>
                </Card>
              ) : null}

              {compareAssumptions.length ? (
                <Card className="p-5">
                  <h3 className="text-sm font-semibold">Assumptions</h3>
                  <ul className="mt-2 list-disc space-y-1 pl-5 text-sm text-[var(--muted)]">
                    {compareAssumptions.map((item) => (
                      <li key={item}>{item}</li>
                    ))}
                  </ul>
                  {formulaVersion ? (
                    <p className="mt-3 text-xs text-[var(--muted)]">
                      Formula {formulaVersion}
                    </p>
                  ) : null}
                </Card>
              ) : null}
            </div>
          ) : null}
        </>
      ) : null}

      <p className="rounded-xl border border-[var(--border)] bg-black/[0.02] px-4 py-3 text-xs leading-relaxed text-[var(--muted)]">
        {DISCLAIMER}
      </p>
    </div>
  );
}
