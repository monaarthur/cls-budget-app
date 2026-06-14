export const BUDGET_GRID_COLUMNS = [
  { colId: "accountName", label: "Account" },
  { colId: "accountCategoryName", label: "Category" },
  { colId: "accountPaymentDay", label: "Payment day" },
  { colId: "amount", label: "Budgeted" },
  { colId: "paymentMade", label: "Paid" },
  { colId: "owed", label: "Owed" },
  { colId: "budgetPaymentStatusName", label: "Status" },
  { colId: "isCleared", label: "Cleared" },
  { colId: "paymentDate", label: "Payment date" },
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
