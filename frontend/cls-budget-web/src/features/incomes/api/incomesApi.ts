import { apiDelete, apiGet, apiPost, apiPut } from "@/lib/api/client";
import type {
  CreateIncomeRequest,
  IncomeResponse,
  IncomeSummaryResponse,
  UpdateIncomeRequest,
} from "@/features/incomes/types/income";

const incomesPath = "/api/v1/incomes";

export const incomesApi = {
  getAll: () => apiGet<IncomeResponse[]>(incomesPath),
  getById: (id: number) => apiGet<IncomeResponse>(`${incomesPath}/${id}`),
  getByBudget: (budgetId: number) =>
    apiGet<IncomeResponse[]>(`${incomesPath}/budget/${budgetId}`),
  getSummaryByBudget: (budgetId: number) =>
    apiGet<IncomeSummaryResponse>(`${incomesPath}/budget/${budgetId}/summary`),
  create: (body: CreateIncomeRequest) =>
    apiPost<IncomeResponse, CreateIncomeRequest>(incomesPath, body),
  update: (id: number, body: UpdateIncomeRequest) =>
    apiPut<IncomeResponse, UpdateIncomeRequest>(
      `${incomesPath}/${id}`,
      body,
    ),
  remove: (id: number) => apiDelete<null>(`${incomesPath}/${id}`),
};
