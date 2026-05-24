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

/** Default grid row order: Mortgage → Loan → Utility → Credit Card → others */
const CATEGORY_SORT_ORDER: Record<string, number> = {
  Mortgage: 0,
  Loan: 1,
  Utility: 2,
  "Credit Card": 3,
};

export function getAccountCategorySortIndex(categoryId: number): number {
  return CATEGORY_SORT_ORDER[getAccountCategoryName(categoryId)] ?? 100;
}

export function compareAccountCategoryIds(
  categoryIdA: number,
  categoryIdB: number,
  labelA = "",
  labelB = "",
): number {
  const orderDiff =
    getAccountCategorySortIndex(categoryIdA) -
    getAccountCategorySortIndex(categoryIdB);
  if (orderDiff !== 0) return orderDiff;

  if (labelA && labelB) {
    return labelA.localeCompare(labelB, undefined, { sensitivity: "base" });
  }

  return categoryIdA - categoryIdB;
}

export function sortRowsByCategory<T extends { accountCategoryId: number }>(
  rows: T[],
  getLabel: (row: T) => string,
): T[] {
  return [...rows].sort((a, b) =>
    compareAccountCategoryIds(
      a.accountCategoryId,
      b.accountCategoryId,
      getLabel(a),
      getLabel(b),
    ),
  );
}
