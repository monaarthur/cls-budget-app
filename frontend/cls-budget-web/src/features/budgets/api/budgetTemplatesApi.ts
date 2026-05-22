import { apiGet } from "@/lib/api/client";
import type { BudgetTemplateResponse } from "@/features/budgets/types/budgetTemplate";

const basePath = "/api/v1/budget-templates";

export const budgetTemplatesApi = {
  getAll: () => apiGet<BudgetTemplateResponse[]>(basePath),
};
