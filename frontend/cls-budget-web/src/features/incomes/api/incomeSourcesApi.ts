import { apiGet } from "@/lib/api/client";
import type { IncomeSourceResponse } from "@/features/incomes/types/income";

const incomeSourcesPath = "/api/v1/income-sources";

export const incomeSourcesApi = {
  getAll: () => apiGet<IncomeSourceResponse[]>(incomeSourcesPath),
};
