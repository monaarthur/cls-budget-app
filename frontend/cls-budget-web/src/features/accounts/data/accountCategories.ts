import type { AccountCategoryResponse } from "@/features/accounts/types/accountCategory";

/** Fallback system categories when the API list is not loaded yet. */
export const ACCOUNT_CATEGORIES: readonly { id: number; name: string }[] = [
  { id: 1, name: "Credit Card" },
  { id: 2, name: "Loan" },
  { id: 3, name: "Mortgage" },
  { id: 4, name: "Utility" },
  { id: 5, name: "Subscription" },
  { id: 6, name: "Savings" },
  { id: 7, name: "Checking" },
] as const;

export const ACCOUNT_CATEGORY_NAMES = ACCOUNT_CATEGORIES.map((c) => c.name);

/** Default grid row order: Mortgage → Loan → Utility → Credit Card → others */
const CATEGORY_SORT_ORDER: Record<string, number> = {
  Mortgage: 0,
  Loan: 1,
  Utility: 2,
  "Credit Card": 3,
};

export function getCategoryNames(
  categories: readonly AccountCategoryResponse[],
): string[] {
  if (categories.length === 0) return [...ACCOUNT_CATEGORY_NAMES];
  return categories.map((c) => c.name);
}

export function getAccountCategoryName(
  categoryId: number,
  categories?: readonly AccountCategoryResponse[],
): string {
  const fromApi = categories?.find((c) => c.accountCategoryId === categoryId)?.name;
  if (fromApi) return fromApi;
  return ACCOUNT_CATEGORIES.find((c) => c.id === categoryId)?.name ?? "Unknown";
}

export function getAccountCategoryId(
  name: string,
  categories?: readonly AccountCategoryResponse[],
): number | undefined {
  const fromApi = categories?.find((c) => c.name === name)?.accountCategoryId;
  if (fromApi != null) return fromApi;
  return ACCOUNT_CATEGORIES.find((c) => c.name === name)?.id;
}

export function getSubCategoryNames(
  categoryId: number,
  categories: readonly AccountCategoryResponse[],
): string[] {
  const category = categories.find((c) => c.accountCategoryId === categoryId);
  return category?.subCategories.map((s) => s.name) ?? [];
}

export function getAccountSubCategoryName(
  categoryId: number,
  subCategoryId: number | null | undefined,
  categories: readonly AccountCategoryResponse[],
): string {
  if (subCategoryId == null) return "";
  const category = categories.find((c) => c.accountCategoryId === categoryId);
  return (
    category?.subCategories.find((s) => s.accountSubCategoryId === subCategoryId)
      ?.name ?? ""
  );
}

export function getAccountSubCategoryId(
  categoryId: number,
  name: string,
  categories: readonly AccountCategoryResponse[],
): number | undefined {
  if (!name.trim()) return undefined;
  const category = categories.find((c) => c.accountCategoryId === categoryId);
  return category?.subCategories.find((s) => s.name === name)?.accountSubCategoryId;
}

export function getAccountCategorySortIndex(
  categoryId: number,
  categories?: readonly AccountCategoryResponse[],
): number {
  return CATEGORY_SORT_ORDER[getAccountCategoryName(categoryId, categories)] ?? 100;
}

export function compareAccountCategoryIds(
  categoryIdA: number,
  categoryIdB: number,
  labelA = "",
  labelB = "",
  categories?: readonly AccountCategoryResponse[],
): number {
  const orderDiff =
    getAccountCategorySortIndex(categoryIdA, categories) -
    getAccountCategorySortIndex(categoryIdB, categories);
  if (orderDiff !== 0) return orderDiff;

  if (labelA && labelB) {
    return labelA.localeCompare(labelB, undefined, { sensitivity: "base" });
  }

  return categoryIdA - categoryIdB;
}

export function sortRowsByCategory<T extends { accountCategoryId: number }>(
  rows: T[],
  getLabel: (row: T) => string,
  categories?: readonly AccountCategoryResponse[],
): T[] {
  return [...rows].sort((a, b) =>
    compareAccountCategoryIds(
      a.accountCategoryId,
      b.accountCategoryId,
      getLabel(a),
      getLabel(b),
      categories,
    ),
  );
}
