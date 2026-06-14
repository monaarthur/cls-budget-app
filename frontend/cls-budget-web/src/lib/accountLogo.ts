import creditCardLogoDomains from "@/features/accounts/data/creditCardLogoDomains.json";

const PLACEHOLDER_DOMAIN_PATTERN =
  /(?:^|\.)cls-budget\.local$|^localhost$|^import\.cls-budget\.local$/i;

type LogoDomainRule = {
  match: string;
  domain: string;
};

const CREDIT_CARD_LOGO_RULES = creditCardLogoDomains as LogoDomainRule[];

export function isPlaceholderLogoDomain(domain: string | null | undefined): boolean {
  if (!domain) return true;
  return PLACEHOLDER_DOMAIN_PATTERN.test(domain);
}

export function resolveCreditCardLogoDomain(
  name: string,
  url: string | null | undefined,
): string | null {
  const normalizedName = name.trim().toLowerCase();
  const nameMatch = CREDIT_CARD_LOGO_RULES.find(
    (rule) =>
      normalizedName === rule.match || normalizedName.includes(rule.match),
  )?.domain;

  if (nameMatch) {
    return nameMatch;
  }

  const urlDomain = extractDomainFromAccountUrl(url);
  if (urlDomain && !isPlaceholderLogoDomain(urlDomain)) {
    return urlDomain;
  }

  return null;
}

export function logoDomainCandidates(domain: string): string[] {
  const normalized = domain.trim().toLowerCase();
  const candidates = [normalized];

  const parts = normalized.split(".");
  if (parts.length > 2) {
    candidates.push(parts.slice(-2).join("."));
  }

  return [...new Set(candidates)];
}

export function normalizeAccountUrl(url: string | null | undefined): string | null {
  if (url == null) return null;
  const trimmed = url.trim();
  if (!trimmed) return null;
  if (!/^https?:\/\//i.test(trimmed)) return `https://${trimmed}`;
  return trimmed;
}

export function extractDomainFromAccountUrl(
  url: string | null | undefined,
): string | null {
  const normalized = normalizeAccountUrl(url);
  if (!normalized) return null;

  try {
    return new URL(normalized).hostname.replace(/^www\./i, "").toLowerCase();
  } catch {
    return null;
  }
}

const LOGO_EXTENSIONS = ["png", "jpg", "jpeg", "webp", "ico"] as const;

export function getAccountLogoCandidates(accountId: number): string[] {
  return LOGO_EXTENSIONS.map(
    (extension) => `/account-logos/${accountId}.${extension}`,
  );
}

export function getAccountLogoPath(accountId: number): string {
  return `/account-logos/${accountId}.png`;
}

export const ACCOUNT_LOGOS_DIR = "public/account-logos";
