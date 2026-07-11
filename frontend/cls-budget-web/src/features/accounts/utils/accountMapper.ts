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
    paymentDay: row.paymentDay,
    phone: row.phone,
    email: row.email,
    url: row.url,
    username: row.username,
    notes: row.notes,
    isPaidOff: row.isPaidOff,
    paidOffDate: row.paidOffDate,
    isCreditCard: row.isCreditCard,
    accountCategoryId: row.accountCategoryId,
    interestRate: row.interestRate,
  };
}

export function formatDateForGrid(iso: string | null): string {
  if (!iso) return "";
  const date = toUtcDateFromIso(iso);
  if (!date) return "";
  const month = String(date.getUTCMonth() + 1).padStart(2, "0");
  const day = String(date.getUTCDate()).padStart(2, "0");
  const year = date.getUTCFullYear();
  return `${month}/${day}/${year}`;
}

export function toDateInputValue(iso: string | null): string {
  if (!iso) return "";
  const date = toUtcDateFromIso(iso);
  if (!date) return "";
  const month = String(date.getUTCMonth() + 1).padStart(2, "0");
  const day = String(date.getUTCDate()).padStart(2, "0");
  return `${date.getUTCFullYear()}-${month}-${day}`;
}

function toUtcDateFromIso(iso: string): Date | null {
  const trimmed = iso.trim();
  if (!trimmed) return null;

  const hasTime = trimmed.includes("T");
  const hasZone =
    trimmed.endsWith("Z") || /[+-]\d{2}:\d{2}$/.test(trimmed);
  const normalized = hasTime
    ? hasZone
      ? trimmed
      : `${trimmed}Z`
    : `${trimmed}T00:00:00.000Z`;

  const date = new Date(normalized);
  return Number.isFinite(date.getTime()) ? date : null;
}

export function normalizeGridDateIso(value: unknown): string | null {
  const parsed = parseGridDateValue(value);
  if (!parsed) return null;
  const date = toUtcDateFromIso(parsed);
  if (!date) return null;
  return new Date(
    Date.UTC(date.getUTCFullYear(), date.getUTCMonth(), date.getUTCDate()),
  ).toISOString();
}

export function parseAndNormalizeGridDate(
  rawValue: unknown,
  fallback?: string | null,
): string | null {
  const parsed = parseGridDateValue(rawValue);
  if (parsed != null) return normalizeGridDateIso(parsed);
  if (fallback !== undefined) {
    return normalizeGridDateIso(fallback);
  }
  return null;
}

export function parseGridDate(value: string | null | undefined): string | null {
  return parseGridDateValue(value);
}

export function parseGridDateValue(value: unknown): string | null {
  if (value == null || value === "") return null;

  if (value instanceof Date) {
    if (!Number.isFinite(value.getTime())) return null;
    return new Date(
      Date.UTC(value.getUTCFullYear(), value.getUTCMonth(), value.getUTCDate()),
    ).toISOString();
  }

  const trimmed = String(value).trim();
  if (!trimmed) return null;
  if (trimmed.includes("T")) {
    const date = toUtcDateFromIso(trimmed);
    if (!date) return null;
    return new Date(
      Date.UTC(date.getUTCFullYear(), date.getUTCMonth(), date.getUTCDate()),
    ).toISOString();
  }

  const slashMatch = /^(\d{1,2})\/(\d{1,2})\/(\d{4})$/.exec(trimmed);
  if (slashMatch) {
    const month = Number(slashMatch[1]);
    const day = Number(slashMatch[2]);
    const year = Number(slashMatch[3]);
    if (month >= 1 && month <= 12 && day >= 1 && day <= 31) {
      const date = new Date(Date.UTC(year, month - 1, day));
      if (
        date.getUTCFullYear() === year &&
        date.getUTCMonth() === month - 1 &&
        date.getUTCDate() === day
      ) {
        return date.toISOString();
      }
    }
    return null;
  }

  if (/^\d{4}-\d{2}-\d{2}$/.test(trimmed)) {
    return `${trimmed}T00:00:00.000Z`;
  }

  const parsed = new Date(trimmed);
  if (Number.isFinite(parsed.getTime())) {
    return new Date(
      Date.UTC(parsed.getFullYear(), parsed.getMonth(), parsed.getDate()),
    ).toISOString();
  }

  return null;
}

export function formatPaymentDay(value: unknown): string {
  if (value === null || value === undefined || value === "") return "";
  const day = Number(value);
  if (!Number.isFinite(day)) return "";
  return String(day).padStart(2, "0");
}

export function isCreditCardAccount(
  account: Pick<AccountResponse, "isCreditCard">,
): boolean {
  return account.isCreditCard === true;
}
