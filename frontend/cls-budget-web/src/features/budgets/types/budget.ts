export interface BudgetResponse {
  budgetId: number;
  name: string;
  startPeriod: string;
  endPeriod: string;
  budgetTemplateId: number;
  accountIds: number[];
}

export interface CreateBudgetRequest {
  name: string;
  startPeriod: string;
  endPeriod: string;
  budgetTemplateId: number;
}

export interface UpdateBudgetRequest {
  name: string;
  startPeriod: string;
  endPeriod: string;
  budgetTemplateId: number;
  accountIds?: number[] | null;
}

export interface CopyBudgetRequest {
  name: string;
  startPeriod: string;
  endPeriod: string;
  budgetTemplateId?: number | null;
}
