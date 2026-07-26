export interface CalculationEnvelope<T> {
  calculatedOnUtc: string;
  formulaVersion: string;
  assumptions: string[];
  warnings: string[];
  result: T;
}

export interface AnalyzeCashFlowRequest {
  monthlyNetIncome: number;
  requiredExpenses: number;
  variableExpenses: number;
  existingDebtMinimums?: number | null;
  emergencySavingsContribution: number;
  safetyBuffer: number;
  additionalAvailableFunds?: number;
  userOverrideExtraPayment?: number | null;
}

export interface CashFlowAnalysisResult {
  monthlyDisposableIncome: number;
  requiredDebtMinimums: number;
  safeExtraDebtPayment: number;
  aggressiveExtraDebtPayment: number;
  remainingCashBuffer: number;
  recommendedExtraDebtPayment: number;
  usedUserOverride: boolean;
  suggestedTotalMonthlyDebtPayment: number;
}
