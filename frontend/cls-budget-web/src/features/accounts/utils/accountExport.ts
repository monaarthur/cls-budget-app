import { getAccountCategoryName } from "@/features/accounts/data/accountCategories";
import type { AccountResponse } from "@/features/accounts/types/account";
import { formatDateForGrid, formatPaymentDay } from "@/features/accounts/utils/accountMapper";
import type { ExportColumn } from "@/lib/tableExport";
import { downloadExcelTable, downloadPdfTable } from "@/lib/tableExport";

function money(value: number | null | undefined): number | null {
  if (value == null || !Number.isFinite(value)) return null;
  return value;
}

function percent(value: number | null | undefined): number | null {
  if (value == null || !Number.isFinite(value)) return null;
  return value;
}

const sharedColumns: ExportColumn<AccountResponse>[] = [
  { header: "Name", value: (row) => row.name, width: 2.4 },
  { header: "Number", value: (row) => row.number, width: 1.3 },
  {
    header: "Balance",
    value: (row) => money(row.balance),
    kind: "number",
    width: 1.2,
  },
  {
    header: "Limit",
    value: (row) => money(row.limit),
    kind: "number",
    width: 1.2,
  },
  {
    header: "Monthly",
    value: (row) => money(row.monthlyPayment),
    kind: "number",
    width: 1.1,
  },
  {
    header: "Payment day",
    value: (row) => formatPaymentDay(row.paymentDay),
    width: 1,
  },
  {
    header: "Grace",
    value: (row) => row.gracePeriod ?? "",
    kind: "number",
    width: 0.8,
  },
  {
    header: "Grace day",
    value: (row) => formatPaymentDay(row.graceDay ?? null),
    width: 0.9,
  },
  {
    header: "Paid off",
    value: (row) => row.isPaidOff,
    kind: "boolean",
    width: 0.9,
  },
];

const accountOnlyColumns: ExportColumn<AccountResponse>[] = [
  {
    header: "Category",
    value: (row) =>
      row.accountCategoryName ?? getAccountCategoryName(row.accountCategoryId),
    width: 1.4,
  },
  {
    header: "Subcategory",
    value: (row) => row.accountSubCategoryName ?? "",
    width: 1.3,
  },
  {
    header: "Opened",
    value: (row) => formatDateForGrid(row.accountOpenDate),
    width: 1.1,
  },
];

const creditCardOnlyColumns: ExportColumn<AccountResponse>[] = [
  {
    header: "APR %",
    value: (row) => percent(row.interestRate),
    kind: "number",
    width: 0.9,
  },
  {
    header: "Cash advance APR %",
    value: (row) => percent(row.cashOutInterestRate),
    kind: "number",
    width: 1.3,
  },
  {
    header: "Cash advance fee %",
    value: (row) => percent(row.cashAdvanceFeePercentage),
    kind: "number",
    width: 1.3,
  },
  {
    header: "Exclude payoff",
    value: (row) => row.includeInPayoffAnalysis === false,
    kind: "boolean",
    width: 1.1,
  },
];

const extraColumns: ExportColumn<AccountResponse>[] = [
  { header: "Phone", value: (row) => row.phone, width: 1.2 },
  { header: "Email", value: (row) => row.email, width: 1.6 },
  { header: "URL", value: (row) => row.url, width: 1.6 },
  { header: "Description", value: (row) => row.description ?? "", width: 1.6 },
  { header: "Notes", value: (row) => row.notes ?? "", width: 1.6 },
];

export function getAccountExportColumns(
  creditCardOnly: boolean,
  forPdf = false,
): ExportColumn<AccountResponse>[] {
  const columns = creditCardOnly
    ? [...sharedColumns, ...creditCardOnlyColumns]
    : [...sharedColumns, ...accountOnlyColumns];

  if (forPdf) return columns;
  return [...columns, ...extraColumns];
}

export function exportAccounts(
  format: "excel" | "pdf",
  accounts: AccountResponse[],
  creditCardOnly: boolean,
): void {
  const title = creditCardOnly ? "Credit cards" : "Accounts";
  const filenameBase = creditCardOnly ? "credit-cards" : "accounts";
  const columns = getAccountExportColumns(creditCardOnly, format === "pdf");

  if (format === "excel") {
    downloadExcelTable(title, columns, accounts, filenameBase);
    return;
  }

  downloadPdfTable(title, columns, accounts, filenameBase);
}
