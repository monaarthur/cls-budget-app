import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { createRequire } from "node:module";

const require = createRequire(import.meta.url);
const __dirname = path.dirname(fileURLToPath(import.meta.url));
const OUT_DIR = path.join(__dirname, "../public/account-logos");
const API_BASE =
  process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:5123";
const CREDIT_CARD_LOGO_RULES = require("../src/features/accounts/data/creditCardLogoDomains.json");

const PLACEHOLDER_DOMAIN_PATTERN =
  /(?:^|\.)cls-budget\.local$|^localhost$|^import\.cls-budget\.local$/i;

function normalizeAccountUrl(url) {
  if (!url) return null;
  const trimmed = String(url).trim();
  if (!trimmed) return null;
  if (!/^https?:\/\//i.test(trimmed)) return `https://${trimmed}`;
  return trimmed;
}

function extractDomain(url) {
  const normalized = normalizeAccountUrl(url);
  if (!normalized) return null;
  try {
    return new URL(normalized).hostname.replace(/^www\./i, "").toLowerCase();
  } catch {
    return null;
  }
}

function isPlaceholderDomain(domain) {
  if (!domain) return true;
  return PLACEHOLDER_DOMAIN_PATTERN.test(domain);
}

function resolveCreditCardLogoDomain(name, url) {
  const normalizedName = String(name).trim().toLowerCase();
  const nameMatch = CREDIT_CARD_LOGO_RULES.find(
    (rule) =>
      normalizedName === rule.match || normalizedName.includes(rule.match),
  )?.domain;

  if (nameMatch) {
    return nameMatch;
  }

  const urlDomain = extractDomain(url);
  if (urlDomain && !isPlaceholderDomain(urlDomain)) {
    return urlDomain;
  }

  return null;
}

function logoDomainCandidates(domain) {
  const normalized = String(domain).trim().toLowerCase();
  const candidates = [normalized];
  const parts = normalized.split(".");
  if (parts.length > 2) {
    candidates.push(parts.slice(-2).join("."));
  }
  return [...new Set(candidates)];
}

function detectExtension(buffer) {
  if (buffer[0] === 0x89 && buffer[1] === 0x50) return "png";
  if (buffer[0] === 0xff && buffer[1] === 0xd8) return "jpg";
  if (
    buffer[0] === 0x52 &&
    buffer[1] === 0x49 &&
    buffer[2] === 0x46 &&
    buffer[3] === 0x46
  ) {
    return "webp";
  }
  return "png";
}

async function downloadLogoForDomain(domain) {
  const domains = logoDomainCandidates(domain);
  const sources = domains.flatMap((candidate) => [
    `https://logo.clearbit.com/${candidate}`,
    `https://www.google.com/s2/favicons?domain=${candidate}&sz=128`,
    `https://icons.duckduckgo.com/ip3/${candidate}.ico`,
  ]);

  for (const source of sources) {
    try {
      const response = await fetch(source);
      if (!response.ok) continue;

      const buffer = Buffer.from(await response.arrayBuffer());
      if (buffer.length < 48) continue;

      return { buffer, extension: detectExtension(buffer), source, domain };
    } catch {
      continue;
    }
  }

  return null;
}

async function loadCreditCardAccounts() {
  const response = await fetch(`${API_BASE}/api/v1/accounts`);
  if (!response.ok) {
    throw new Error(`Failed to load accounts (${response.status})`);
  }

  const payload = await response.json();
  const accounts = payload.data ?? payload;
  return accounts.filter((account) => account.isCreditCard === true);
}

async function main() {
  fs.mkdirSync(OUT_DIR, { recursive: true });

  const accounts = await loadCreditCardAccounts();
  const manifest = {};
  let saved = 0;
  let skipped = 0;

  for (const account of accounts) {
    const domain = resolveCreditCardLogoDomain(account.name, account.url);
    if (!domain) {
      console.warn(`Skipping ${account.name}: no logo domain mapping`);
      skipped += 1;
      continue;
    }

    const logo = await downloadLogoForDomain(domain);
    if (!logo) {
      console.warn(`Skipping ${account.name}: logo not found for ${domain}`);
      skipped += 1;
      continue;
    }

    const filename = `${account.accountId}.${logo.extension}`;
    fs.writeFileSync(path.join(OUT_DIR, filename), logo.buffer);
    manifest[account.accountId] = {
      accountId: account.accountId,
      name: account.name,
      domain: logo.domain,
      url: account.url,
      filename,
      source: logo.source,
      updatedAt: new Date().toISOString(),
    };
    saved += 1;
    console.log(`Saved ${filename} (${account.name} -> ${logo.domain})`);
  }

  fs.writeFileSync(
    path.join(OUT_DIR, "manifest.json"),
    `${JSON.stringify(manifest, null, 2)}\n`,
    "utf8",
  );

  console.log(
    `Done. Saved ${saved} logo${saved === 1 ? "" : "s"}, skipped ${skipped}.`,
  );
}

main().catch((error) => {
  console.error(error instanceof Error ? error.message : error);
  process.exit(1);
});
