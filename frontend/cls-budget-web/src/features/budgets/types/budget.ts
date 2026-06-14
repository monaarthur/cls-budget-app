export interface BudgetResponse {
  budgetId: number;
  name: string;
  startPeriod: string;
  endPeriod: string;
  budgetTemplateId: number;
  notes?: string | null;
  accountIds: number[];
  payScheduleId?: number | null;
}

export interface CreateBudgetRequest {
  name: string;
  startPeriod: string;
  endPeriod: string;
  budgetTemplateId: number;
  notes?: string | null;
}

export interface UpdateBudgetRequest {
  name: string;
  startPeriod: string;
  endPeriod: string;
  budgetTemplateId: number;
  notes?: string | null;
  accountIds?: number[] | null;
  payScheduleId?: number | null;
}

export interface CopyBudgetRequest {
  name: string;
  startPeriod: string;
  endPeriod: string;
  budgetTemplateId?: number | null;
}
