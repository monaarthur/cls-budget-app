export interface AccountSubCategoryResponse {
  accountSubCategoryId: number;
  accountCategoryId: number;
  name: string;
  description: string | null;
}

export interface AccountCategoryResponse {
  accountCategoryId: number;
  name: string;
  description: string | null;
  isSystem: boolean;
  subCategories: AccountSubCategoryResponse[];
}

export interface CreateAccountCategoryRequest {
  name: string;
  description?: string | null;
}

export interface CreateAccountSubCategoryRequest {
  accountCategoryId: number;
  name: string;
  description?: string | null;
}
