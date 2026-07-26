export interface CalculationEnvelope<T> {
  calculatedOnUtc: string;
  formulaVersion: string;
  assumptions: string[];
  warnings: string[];
  result: T;
}

export interface BalanceTransferLeg {
  counterpartyCreditCardId: number;
  counterpartyName: string;
  amount: number;
  direction: "In" | "Out" | string;
}

export interface CardMonthlyBalance {
  month: string;
  startingBalance: number;
  interestCharged: number;
  paymentApplied: number;
  minimumPaymentApplied: number;
  extraPaymentApplied: number;
  principalApplied: number;
  balanceTransferredIn?: number;
  balanceTransferredOut?: number;
  transfers?: BalanceTransferLeg[];
  endingBalance: number;
}

export interface CardPayoffOrder {
  creditCardId: number;
  name: string;
  priorityOrder: number;
  estimatedPayoffDate: string | null;
  totalInterestPaid: number;
  monthlyBalances?: CardMonthlyBalance[];
}

export interface PayoffStrategySummary {
  strategy: string;
  estimatedPayoffDate: string | null;
  totalInterest: number;
  monthsToPayoff: number;
  combinedMinimumPayments: number;
  isValid: boolean;
  warnings: string[];
  cardOrder: CardPayoffOrder[];
}

export interface ComparePayoffPlansResult {
  startingDebt: number;
  monthlyPayment: number;
  strategies: PayoffStrategySummary[];
  recommendedStrategy: string | null;
  reason: string | null;
}

export interface PromotionalBalanceTransfer {
  fromCreditCardId: number;
  toCreditCardId: number;
  amount?: number | null;
  promotionalAnnualPercentageRate: number;
  promotionalPeriodMonths: number;
  /** 0 = first forecast month. */
  applyAtMonthOffset?: number;
}

export interface ComparePayoffPlansRequest {
  totalMonthlyDebtPayment: number;
  startDate?: string | null;
  /** Applied to both Avalanche and Snowball (1–99). Omit/blank to pay straight to zero. */
  targetUtilizationPercent?: number | null;
  /** Pay balances above credit limit first (before util target / strategy). */
  payOverLimitFirst?: boolean;
  /**
   * After payments, use a lower cash-advance APR card's available credit to pay
   * the focus card; if none qualify, use the highest-APR card with room.
   */
  enableCashAdvanceBalanceMoves?: boolean;
  promotionalTransfers?: PromotionalBalanceTransfer[];
  /** After utilization target is met: "Avalanche" or "Snowball". */
  postUtilizationStrategy?: "Avalanche" | "Snowball" | null;
  /** Optional loan principal applied to cards first, then repaid in the plan. */
  loanAmount?: number | null;
  /** APR percent for the optional loan. */
  loanAnnualPercentageRate?: number | null;
  /** Avalanche, Snowball, or SelectedAccounts for applying loan proceeds. */
  loanApplyStrategy?: "Avalanche" | "Snowball" | "SelectedAccounts" | null;
  /** Credit card account ids when applying to specific accounts (order preserved). */
  loanApplyCreditCardIds?: number[] | null;
  loanType?: LoanTypeId | null;
  loanTermMonths?: number | null;
  loanInterestOnlyMonths?: number | null;
  loanFixedMonthlyPayment?: number | null;
}

export type LoanTypeId =
  | "Personal"
  | "HomeEquity"
  | "Heloc"
  | "Retirement401k"
  | "Family";

export interface LoanScheduleRequest {
  loanType: LoanTypeId;
  amount: number;
  annualPercentageRate: number;
  termMonths?: number | null;
  interestOnlyMonths?: number | null;
  fixedMonthlyPayment?: number | null;
}

export interface LoanScheduleMonth {
  monthNumber: number;
  payment: number;
  interest: number;
  principal: number;
  endingBalance: number;
}

export interface LoanScheduleResult {
  isValid: boolean;
  errors: string[];
  loanType: string;
  loanTypeDisplayName: string;
  monthlyPayment: number;
  phase2MonthlyPayment: number | null;
  monthsToPayoff: number;
  totalInterest: number;
  totalPaid: number;
  schedule: LoanScheduleMonth[];
}

export interface CompareLoanSavingsRequest {
  totalMonthlyDebtPayment: number;
  strategy?: string | null;
  targetUtilizationPercent?: number | null;
  payOverLimitFirst?: boolean;
  enableCashAdvanceBalanceMoves?: boolean;
  promotionalTransfers?: PromotionalBalanceTransfer[];
  postUtilizationStrategy?: "Avalanche" | "Snowball" | null;
  loanAmount: number;
  loanAnnualPercentageRate: number;
  loanApplyStrategy?: "Avalanche" | "Snowball" | "SelectedAccounts" | null;
  loanApplyCreditCardIds?: number[] | null;
  loanType: LoanTypeId;
  loanTermMonths?: number | null;
  loanInterestOnlyMonths?: number | null;
  loanFixedMonthlyPayment?: number | null;
}

export interface LoanSavingsScenario {
  label: string;
  strategy: string;
  totalInterest: number;
  totalPrincipalPaid: number;
  totalPaid: number;
  monthsToPayoff: number;
  estimatedPayoffDate: string | null;
  isValid: boolean;
  warnings: string[];
}

export interface CompareLoanSavingsResult {
  withoutLoan: LoanSavingsScenario;
  withLoan: LoanSavingsScenario;
  interestSaved: number;
  monthsSaved: number;
  totalPaidSaved: number;
  summary: string;
}

export interface SavedPayoffPlan {
  savedPayoffPlanId: number;
  name: string;
  goal?: string | null;
  strategy: string;
  extraMonthlyPayment: number;
  totalMonthlyDebtPayment: number;
  targetUtilizationPercent: number | null;
  payOverLimitFirst: boolean;
  postUtilizationStrategy: string | null;
  enableCashAdvanceBalanceMoves: boolean;
  loanAmount?: number | null;
  loanAnnualPercentageRate?: number | null;
  loanApplyStrategy?: string | null;
  loanApplyCreditCardIds?: number[] | null;
  loanType?: string | null;
  loanTermMonths?: number | null;
  loanInterestOnlyMonths?: number | null;
  loanFixedMonthlyPayment?: number | null;
  promotionalTransfers: PromotionalBalanceTransfer[];
  createdOnUtc: string;
  updatedOnUtc: string;
}

export interface SavePayoffPlanRequest {
  name: string;
  goal?: string | null;
  strategy: string;
  extraMonthlyPayment: number;
  totalMonthlyDebtPayment: number;
  targetUtilizationPercent?: number | null;
  payOverLimitFirst?: boolean;
  postUtilizationStrategy?: "Avalanche" | "Snowball" | null;
  enableCashAdvanceBalanceMoves?: boolean;
  loanAmount?: number | null;
  loanAnnualPercentageRate?: number | null;
  loanApplyStrategy?: "Avalanche" | "Snowball" | "SelectedAccounts" | null;
  loanApplyCreditCardIds?: number[] | null;
  loanType?: LoanTypeId | null;
  loanTermMonths?: number | null;
  loanInterestOnlyMonths?: number | null;
  loanFixedMonthlyPayment?: number | null;
  promotionalTransfers?: PromotionalBalanceTransfer[];
  startDate?: string | null;
}

export type UpdateSavedPayoffPlanRequest = SavePayoffPlanRequest;

export interface CompareSavedPayoffPlansRequest {
  planIds: number[];
  startDate?: string | null;
}

export interface SavedPayoffPlanCompareItem {
  savedPayoffPlanId: number;
  name: string;
  strategySummary: PayoffStrategySummary;
}

export interface CompareSavedPayoffPlansResult {
  plans: SavedPayoffPlanCompareItem[];
}

export interface ActivePayoffPlanProgress {
  startingDebt: number;
  currentDebt: number;
  paidToDate: number;
  debtReduced: number;
  projectedMonthsRemaining: number;
  projectedRemainingInterest: number;
  projectedPayoffDate: string | null;
  projectionIsValid: boolean;
  plannedMonthlyPayment: number;
  averageMonthlyPaid: number;
  adherenceNote: string | null;
  warnings: string[];
}

export interface ActivePayoffPlan {
  activePayoffPlanId: number;
  name: string;
  status: string;
  sourceSavedPayoffPlanId?: number | null;
  startedOnUtc: string;
  endedOnUtc?: string | null;
  currentVersionNumber: number;
  startingDebt: number;
  goal?: string | null;
  strategy: string;
  extraMonthlyPayment: number;
  totalMonthlyDebtPayment: number;
  targetUtilizationPercent: number | null;
  payOverLimitFirst: boolean;
  postUtilizationStrategy: string | null;
  enableCashAdvanceBalanceMoves: boolean;
  loanAmount?: number | null;
  loanAnnualPercentageRate?: number | null;
  loanApplyStrategy?: string | null;
  loanApplyCreditCardIds?: number[] | null;
  loanType?: string | null;
  loanTermMonths?: number | null;
  loanInterestOnlyMonths?: number | null;
  loanFixedMonthlyPayment?: number | null;
  promotionalTransfers: PromotionalBalanceTransfer[];
  progress: ActivePayoffPlanProgress;
}

export interface ActivatePayoffPlanRequest {
  savedPayoffPlanId?: number | null;
  name?: string | null;
  goal?: string | null;
  strategy?: string | null;
  extraMonthlyPayment?: number | null;
  totalMonthlyDebtPayment?: number | null;
  targetUtilizationPercent?: number | null;
  payOverLimitFirst?: boolean;
  postUtilizationStrategy?: "Avalanche" | "Snowball" | null;
  enableCashAdvanceBalanceMoves?: boolean;
  loanAmount?: number | null;
  loanAnnualPercentageRate?: number | null;
  loanApplyStrategy?: "Avalanche" | "Snowball" | "SelectedAccounts" | null;
  loanApplyCreditCardIds?: number[] | null;
  loanType?: LoanTypeId | null;
  loanTermMonths?: number | null;
  loanInterestOnlyMonths?: number | null;
  loanFixedMonthlyPayment?: number | null;
  promotionalTransfers?: PromotionalBalanceTransfer[];
  reason?: string | null;
  startDate?: string | null;
}

export type ReviseActivePayoffPlanRequest = SavePayoffPlanRequest & {
  reason?: string | null;
};

export interface RecordPayoffPlanPaymentRequest {
  accountId: number;
  amount: number;
  paymentDate?: string | null;
  notes?: string | null;
}

export interface PayoffPlanVersion {
  payoffPlanVersionId: number;
  versionNumber: number;
  reason: string | null;
  createdOnUtc: string;
  strategy: string;
  totalMonthlyDebtPayment: number;
  snapshotDebt: number;
  projectedMonthsToPayoff: number;
  projectedTotalInterest: number;
  projectedPayoffDate: string | null;
  projectionIsValid: boolean;
}

export interface PayoffPlanPayment {
  payoffPlanPaymentId: number;
  accountId: number;
  accountName?: string | null;
  amount: number;
  paymentDate: string;
  notes?: string | null;
  payoffPlanVersionId: number;
  versionNumber: number;
  isVoided: boolean;
  voidedOnUtc?: string | null;
  createdOnUtc: string;
}

export interface PayoffPlanEvent {
  payoffPlanEventId: number;
  eventType: string;
  summary: string;
  payloadJson?: string | null;
  createdOnUtc: string;
}

export interface ActivePayoffPlanHistory {
  activePayoffPlanId: number;
  name: string;
  status: string;
  versions: PayoffPlanVersion[];
  payments: PayoffPlanPayment[];
  events: PayoffPlanEvent[];
}

export interface UtilizationThreshold {
  thresholdPercent: number;
  targetBalance: number;
  paymentRequired: number;
}

export interface CardUtilization {
  creditCardId: number;
  name: string;
  currentBalance: number;
  creditLimit: number;
  availableCredit: number;
  utilizationPercentage: number;
  thresholdTargets: UtilizationThreshold[];
}

export interface UtilizationSummaryResult {
  totalBalances: number;
  totalCreditLimits: number;
  overallUtilizationPercentage: number;
  cards: CardUtilization[];
  overallThresholdTargets: UtilizationThreshold[];
}
