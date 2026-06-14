export interface IncomeResponse {
  budgetIncomeId: number;
  budgetId: number;
  incomeSourceId: number;
  incomeSourceName: string;
  amount: number;
  receivedDate: string;
  notes: string | null;
}

export interface CreateIncomeRequest {
  budgetId: number;
  incomeSourceId: number;
  amount: number;
  receivedDate: string;
  notes?: string | null;
}

export type UpdateIncomeRequest = CreateIncomeRequest;

export interface IncomeSourceResponse {
  incomeSourceId: number;
  name: string;
  isActive: boolean;
}

export interface IncomeSummaryItem {
  incomeSourceId: number;
  incomeSourceName: string;
  total: number;
}

export interface IncomeSummaryResponse {
  budgetId: number;
  total: number;
  items: IncomeSummaryItem[];
}
