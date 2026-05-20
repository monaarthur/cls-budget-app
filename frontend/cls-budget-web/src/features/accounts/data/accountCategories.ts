/** Mirrors backend LookupDataSeed account categories. */
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

const idToName = new Map(
  ACCOUNT_CATEGORIES.map((c) => [c.id, c.name] as const),
);

const nameToId = new Map(
  ACCOUNT_CATEGORIES.map((c) => [c.name, c.id] as const),
);

export function getAccountCategoryName(categoryId: number): string {
  return idToName.get(categoryId) ?? "Unknown";
}

export function getAccountCategoryId(name: string): number | undefined {
  return nameToId.get(name);
}
