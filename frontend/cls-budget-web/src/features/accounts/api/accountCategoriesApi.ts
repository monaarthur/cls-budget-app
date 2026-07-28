import { apiGet, apiPost } from "@/lib/api/client";
import type {
  AccountCategoryResponse,
  AccountSubCategoryResponse,
  CreateAccountCategoryRequest,
  CreateAccountSubCategoryRequest,
} from "@/features/accounts/types/accountCategory";

const basePath = "/api/v1/account-categories";

export const accountCategoriesApi = {
  getAll: () => apiGet<AccountCategoryResponse[]>(basePath),
  create: (body: CreateAccountCategoryRequest) =>
    apiPost<AccountCategoryResponse, CreateAccountCategoryRequest>(basePath, body),
  createSubCategory: (body: CreateAccountSubCategoryRequest) =>
    apiPost<AccountSubCategoryResponse, CreateAccountSubCategoryRequest>(
      `${basePath}/subcategories`,
      body,
    ),
};
