import { apiDelete, apiGet, apiPost, apiPut } from "@/lib/api/client";
import type {
  AccountResponse,
  CreateAccountRequest,
  UpdateAccountRequest,
} from "@/features/accounts/types/account";

const basePath = "/api/v1/accounts";

export const accountsApi = {
  getAll: () => apiGet<AccountResponse[]>(basePath),
  getById: (id: number) => apiGet<AccountResponse>(`${basePath}/${id}`),
  create: (body: CreateAccountRequest) =>
    apiPost<AccountResponse, CreateAccountRequest>(basePath, body),
  update: (id: number, body: UpdateAccountRequest) =>
    apiPut<AccountResponse, UpdateAccountRequest>(`${basePath}/${id}`, body),
  remove: (id: number) => apiDelete<null>(`${basePath}/${id}`),
};
