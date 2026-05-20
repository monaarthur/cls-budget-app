import type {
  AccountResponse,
  UpdateAccountRequest,
} from "@/features/accounts/types/account";

export type AccountGridRow = AccountResponse;

export function toUpdateAccountRequest(row: AccountGridRow): UpdateAccountRequest {
  return {
    name: row.name,
    number: row.number,
    description: row.description,
    balance: row.balance,
    limit: row.limit,
    accountOpenDate: row.accountOpenDate,
    monthlyPayment: row.monthlyPayment,
    phone: row.phone,
    email: row.email,
    url: row.url,
    username: row.username,
    notes: row.notes,
    isPaidOff: row.isPaidOff,
    paidOffDate: row.paidOffDate,
    accountCategoryId: row.accountCategoryId,
  };
}

export function formatDateForGrid(iso: string | null): string {
  if (!iso) return "";
  return iso.slice(0, 10);
}

export function parseGridDate(value: string | null | undefined): string | null {
  if (!value) return null;
  const trimmed = value.trim();
  if (!trimmed) return null;
  if (trimmed.includes("T")) return trimmed;
  return `${trimmed}T00:00:00.000Z`;
}
