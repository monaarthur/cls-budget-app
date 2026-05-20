export interface BudgetResponse {
  budgetId: number;
  name: string;
  startPeriod: string;
  endPeriod: string;
  budgetTemplateId: number;
}

export interface CreateBudgetRequest {
  name: string;
  startPeriod: string;
  endPeriod: string;
  budgetTemplateId: number;
}

export type UpdateBudgetRequest = CreateBudgetRequest;
