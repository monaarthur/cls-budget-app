export const BUDGET_GRID_COLUMNS = [
  { colId: "accountName", label: "Account" },
  { colId: "accountCategoryName", label: "Category" },
  { colId: "accountPaymentDay", label: "Acct payment day" },
  { colId: "amount", label: "Budgeted" },
  { colId: "paymentMade", label: "Paid" },
  { colId: "owed", label: "Owed" },
  { colId: "budgetPaymentStatusName", label: "Status" },
  { colId: "isCleared", label: "Cleared" },
  { colId: "paymentDate", label: "Payment day" },
  { colId: "clearedDate", label: "Cleared date" },
  { colId: "accountNumber", label: "Account number" },
  { colId: "accountBalance", label: "Balance" },
  { colId: "accountMonthlyPayment", label: "Monthly (account)" },
  { colId: "paymentSourceId", label: "Payment source" },
  { colId: "incomeSourceId", label: "Income source" },
] as const;

export const BUDGET_DEFAULT_HIDDEN_COLUMNS = new Set([
  "accountNumber",
  "accountBalance",
  "accountMonthlyPayment",
  "clearedDate",
  "incomeSourceId",
]);

export type BudgetGridMode = "budget" | "payment";

/** Visible columns (in order) for each grid mode. Actions stay available in both. */
export const BUDGET_GRID_MODE_COLUMNS: Record<
  BudgetGridMode,
  readonly string[]
> = {
  budget: [
    "accountName",
    "accountPaymentDay",
    "paymentDate",
    "amount",
    "budgetPaymentStatusName",
    "paymentMade",
    "actions",
  ],
  payment: [
    "accountName",
    "paymentDate",
    "amount",
    "budgetPaymentStatusName",
    "paymentMade",
    "isCleared",
    "actions",
  ],
};

export const BUDGET_GRID_MODE_LABELS: Record<BudgetGridMode, string> = {
  budget: "Budget Mode",
  payment: "Payment Mode",
};
