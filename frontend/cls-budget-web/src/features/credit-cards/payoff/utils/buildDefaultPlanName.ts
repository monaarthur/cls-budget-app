export type PayoffGoalId =
  | "improveCredit"
  | "lowerUtilization"
  | "minimizeInterest";

export type DefaultPlanNameInput = {
  goal?: PayoffGoalId | null;
  repaymentType: "Avalanche" | "Snowball" | "Minimums";
  extraMonthlyPayment: number;
  targetUtilizationPercent: number | null;
  postUtilizationStrategy: "Avalanche" | "Snowball" | null;
  enableCashAdvanceBalanceMoves: boolean;
  promotionalTransferCount: number;
  loanAmount?: number | null;
  loanAnnualPercentageRate?: number | null;
  loanApplyStrategy?: "Avalanche" | "Snowball" | "SelectedAccounts" | null;
  loanApplyCreditCardIds?: number[] | null;
  loanType?: string | null;
  loanTermMonths?: number | null;
  /** Defaults to now when omitted. */
  stampedAt?: Date;
};

const GOAL_NAME_SEGMENTS: Record<PayoffGoalId, string> = {
  improveCredit: "Improve credit",
  lowerUtilization: "Lower util",
  minimizeInterest: "Min interest",
};

/** Segments joined by ` · `; omits empty optional parts. */
export function buildDefaultPlanName(input: DefaultPlanNameInput): string {
  const segments: string[] = [];

  if (input.goal) {
    segments.push(GOAL_NAME_SEGMENTS[input.goal]);
  }

  segments.push(input.repaymentType);

  if (input.extraMonthlyPayment > 0) {
    const amount = Number.isInteger(input.extraMonthlyPayment)
      ? String(input.extraMonthlyPayment)
      : input.extraMonthlyPayment.toFixed(2).replace(/\.?0+$/, "");
    segments.push(`Extra $${amount}`);
  }

  if (input.loanAmount != null && input.loanAmount > 0) {
    const amount = Number.isInteger(input.loanAmount)
      ? String(input.loanAmount)
      : input.loanAmount.toFixed(2).replace(/\.?0+$/, "");
    const apr =
      input.loanAnnualPercentageRate != null &&
      Number.isFinite(input.loanAnnualPercentageRate)
        ? input.loanAnnualPercentageRate
        : 0;
    const aprText = Number.isInteger(apr) ? String(apr) : apr.toFixed(2).replace(/\.?0+$/, "");
    const apply =
      input.loanApplyStrategy === "Snowball"
        ? "Snowball"
        : input.loanApplyStrategy === "SelectedAccounts"
          ? "Selected"
          : "Avalanche";
    const typeLabel =
      input.loanType === "HomeEquity"
        ? "home equity"
        : input.loanType === "Heloc"
          ? "HELOC"
          : input.loanType === "Retirement401k"
            ? "401k"
            : input.loanType === "Family"
              ? "family"
              : "personal";
    const term =
      input.loanTermMonths != null && input.loanTermMonths > 0
        ? ` ${input.loanTermMonths}mo`
        : "";
    segments.push(
      `Loan ${typeLabel} $${amount} @ ${aprText}%${term} ${apply}`,
    );
  }

  if (
    input.targetUtilizationPercent != null &&
    input.targetUtilizationPercent > 0
  ) {
    segments.push(`Util ${input.targetUtilizationPercent}%`);
  }

  if (input.postUtilizationStrategy) {
    segments.push(`AfterUtil ${input.postUtilizationStrategy}`);
  }

  if (input.enableCashAdvanceBalanceMoves) {
    segments.push("CashAdvance");
  }

  if (input.promotionalTransferCount > 0) {
    segments.push(`Promo x${input.promotionalTransferCount}`);
  }

  segments.push(formatPlanNameStamp(input.stampedAt ?? new Date()));
  return segments.join(" · ");
}

/** Simple local stamp: `7/21/26 8:49 AM`. */
export function formatPlanNameStamp(date: Date): string {
  const datePart = date.toLocaleDateString("en-US", {
    month: "numeric",
    day: "numeric",
    year: "2-digit",
  });
  const timePart = date.toLocaleTimeString("en-US", {
    hour: "numeric",
    minute: "2-digit",
    hour12: true,
  });
  return `${datePart} ${timePart}`;
}
