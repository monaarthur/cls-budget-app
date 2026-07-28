export function formatCurrency(amount: number): string {
  return new Intl.NumberFormat("en-US", {
    style: "currency",
    currency: "USD",
    minimumFractionDigits: 0,
    maximumFractionDigits: 0,
  }).format(amount);
}

export function formatCurrencyDetailed(amount: number): string {
  return new Intl.NumberFormat("en-US", {
    style: "currency",
    currency: "USD",
  }).format(amount);
}

/**
 * Normalize pasted/typed money text: strip $, commas, spaces, and currency codes.
 * Keeps digits, a single decimal point, and an optional leading minus.
 * Examples: "$1,234.56" → "1234.56", "($50)" → "-50", "USD 100" → "100"
 */
export function sanitizeMoneyInput(raw: string): string {
  let text = raw.trim();
  if (!text) return "";

  let negative = false;
  if (/^\(.*\)$/.test(text)) {
    negative = true;
    text = text.slice(1, -1).trim();
  }

  text = text
    .replace(/usd/gi, "")
    .replace(/\$/g, "")
    .replace(/,/g, "")
    .replace(/\s+/g, "");

  if (text.startsWith("-")) {
    negative = true;
    text = text.slice(1);
  } else if (text.startsWith("+")) {
    text = text.slice(1);
  }

  // Keep only digits and dots, then collapse to a single decimal point.
  text = text.replace(/[^\d.]/g, "");
  const firstDot = text.indexOf(".");
  if (firstDot !== -1) {
    text =
      text.slice(0, firstDot + 1) +
      text.slice(firstDot + 1).replace(/\./g, "");
  }

  if (!text || text === ".") return negative ? "-" : "";
  return negative ? `-${text}` : text;
}

/**
 * Parse a money value from number or free-form text ($1,234.56, etc.).
 * Returns null when empty/invalid.
 */
export function parseMoneyInput(value: unknown): number | null {
  if (value === null || value === undefined) return null;
  if (typeof value === "number") {
    return Number.isFinite(value) ? value : null;
  }

  const cleaned = sanitizeMoneyInput(String(value));
  if (!cleaned || cleaned === "-" || cleaned === "." || cleaned === "-.") {
    return null;
  }

  const n = Number(cleaned);
  return Number.isFinite(n) ? n : null;
}

/** Like parseMoneyInput, but empty/invalid values become 0. */
export function parseMoneyInputOrZero(value: unknown): number {
  return parseMoneyInput(value) ?? 0;
}

export function getGreeting(): string {
  const hour = new Date().getHours();
  if (hour < 12) return "Good morning";
  if (hour < 17) return "Good afternoon";
  return "Good evening";
}
