export interface BudgetTemplateResponse {
  budgetTemplateId: number;
  name: string;
  description: string | null;
  accountIds: number[];
}
