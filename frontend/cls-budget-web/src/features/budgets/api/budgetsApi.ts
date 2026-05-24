import { apiDelete, apiGet, apiPost, apiPut } from "@/lib/api/client";
import type {
  BudgetResponse,
  CopyBudgetRequest,
  CreateBudgetRequest,
  UpdateBudgetRequest,
} from "@/features/budgets/types/budget";

const basePath = "/api/v1/budgets";

export const budgetsApi = {
  getAll: () => apiGet<BudgetResponse[]>(basePath),
  getById: (id: number) => apiGet<BudgetResponse>(`${basePath}/${id}`),
  create: (body: CreateBudgetRequest) =>
    apiPost<BudgetResponse, CreateBudgetRequest>(basePath, body),
  copy: (id: number, body: CopyBudgetRequest) =>
    apiPost<BudgetResponse, CopyBudgetRequest>(`${basePath}/${id}/copy`, body),
  update: (id: number, body: UpdateBudgetRequest) =>
    apiPut<BudgetResponse, UpdateBudgetRequest>(`${basePath}/${id}`, body),
  addAccount: (budgetId: number, accountId: number) =>
    apiPost<BudgetResponse, Record<string, never>>(
      `${basePath}/${budgetId}/accounts/${accountId}`,
      {},
    ),
  removeAccount: (budgetId: number, accountId: number) =>
    apiDelete<BudgetResponse>(`${basePath}/${budgetId}/accounts/${accountId}`),
  remove: (id: number) => apiDelete<null>(`${basePath}/${id}`),
};
