export const GRID_COLUMNS = [
  { colId: "name", label: "Name" },
  { colId: "number", label: "Number" },
  { colId: "balance", label: "Balance" },
  { colId: "limit", label: "Limit" },
  { colId: "monthlyPayment", label: "Monthly" },
  { colId: "paymentDay", label: "Payment day" },
  { colId: "accountCategoryName", label: "Category" },
  { colId: "isPaidOff", label: "Paid off" },
  { colId: "accountOpenDate", label: "Opened" },
  { colId: "paidOffDate", label: "Paid off date" },
  { colId: "phone", label: "Phone" },
  { colId: "email", label: "Email" },
  { colId: "url", label: "URL" },
  { colId: "username", label: "Username" },
  { colId: "description", label: "Description" },
  { colId: "notes", label: "Notes" },
] as const;

/** Columns hidden on first load for the accounts grid. */
export const DEFAULT_HIDDEN_COLUMNS = new Set([
  "phone",
  "email",
  "url",
  "username",
  "description",
  "notes",
  "paidOffDate",
]);

/** Not shown on credit card pages (grid or column picker). */
export const CREDIT_CARD_EXCLUDED_COLUMNS = new Set([
  "accountCategoryName",
  "accountOpenDate",
]);
