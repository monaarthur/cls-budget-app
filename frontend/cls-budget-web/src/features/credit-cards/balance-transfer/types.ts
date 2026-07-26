export interface CalculationEnvelope<T> {
  calculatedOnUtc: string;
  formulaVersion: string;
  assumptions: string[];
  warnings: string[];
  result: T;
}

export interface AnalyzeBalanceTransferRequest {
  transferAmount: number;
  currentAnnualPercentageRate: number;
  promotionalAnnualPercentageRate: number;
  promotionalPeriodMonths: number;
  transferFeePercentage: number;
  transferFeeFlatAmount: number;
  newRegularAnnualPercentageRate: number;
  plannedMonthlyPayment: number;
  availableTransferLimit: number;
  startDate?: string | null;
  includeFeeInTransferredBalance?: boolean;
}

export interface BalanceTransferAnalysisResult {
  requestedTransferAmount: number;
  appliedTransferAmount: number;
  totalTransferFee: number;
  startingBalanceWithTransfer: number;
  interestWithoutTransfer: number;
  interestWithTransfer: number;
  netSavings: number;
  breakEvenMonth: number | null;
  balanceRemainingWhenPromotionEnds: number;
  paymentNeededToClearBeforePromotionEnds: number;
  monthsCompared: number;
  recommendation: string;
  explanation: string;
}
