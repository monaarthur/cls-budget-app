import { apiDelete, apiGet, apiPost, apiPut } from "@/lib/api/client";
import type {
  BudgetResponse,
  CreateBudgetRequest,
  UpdateBudgetRequest,
} from "@/features/budgets/types/budget";

const basePath = "/api/v1/budgets";

export const budgetsApi = {
  getAll: () => apiGet<BudgetResponse[]>(basePath),
  getById: (id: number) => apiGet<BudgetResponse>(`${basePath}/${id}`),
  create: (body: CreateBudgetRequest) =>
    apiPost<BudgetResponse, CreateBudgetRequest>(basePath, body),
  update: (id: number, body: UpdateBudgetRequest) =>
    apiPut<BudgetResponse, UpdateBudgetRequest>(`${basePath}/${id}`, body),
  remove: (id: number) => apiDelete<null>(`${basePath}/${id}`),
};
