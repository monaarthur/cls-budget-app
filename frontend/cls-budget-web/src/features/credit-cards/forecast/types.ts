export interface CalculationEnvelope<T> {
  calculatedOnUtc: string;
  formulaVersion: string;
  assumptions: string[];
  warnings: string[];
  result: T;
}

export interface CreateForecastRequest {
  strategy: string;
  totalMonthlyDebtPayment: number;
  forecastMonths: number;
  startDate?: string | null;
  monthlyNetIncome?: number | null;
  monthlyExpenses?: number | null;
  targetUtilizationPercent?: number | null;
  payOverLimitFirst?: boolean;
  name?: string | null;
  save?: boolean;
}

export interface ForecastMonth {
  month: string;
  monthIndex: number;
  startingDebt: number;
  newCharges: number;
  interest: number;
  payments: number;
  endingDebt: number;
  totalCreditLimit: number;
  overallUtilizationPercentage: number;
  availableCash: number;
  cardsPaidOffThisMonth: number;
  cumulativeInterest: number;
}

export interface ForecastResult {
  forecastId: number | null;
  name: string | null;
  strategy: string;
  startingDebt: number;
  monthlyPayment: number;
  forecastMonths: number;
  estimatedDebtFreeDate: string | null;
  totalInterestPaid: number;
  months: ForecastMonth[];
}
