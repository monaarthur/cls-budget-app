import fs from "node:fs";
import path from "node:path";
import {
  logoDomainCandidates,
  resolveCreditCardLogoDomain,
} from "@/lib/accountLogo";
import type { AccountResponse } from "@/features/accounts/types/account";

const OUT_DIR = path.join(process.cwd(), "public/account-logos");

function detectExtension(buffer: Buffer): string {
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

async function downloadLogoForDomain(domain: string) {
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

      return {
        buffer,
        extension: detectExtension(buffer),
        source,
        domain,
      };
    } catch {
      continue;
    }
  }

  return null;
}

async function loadCreditCardAccounts(): Promise<AccountResponse[]> {
  const apiBase =
    process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:5123";
  const response = await fetch(`${apiBase}/api/v1/accounts`, {
    cache: "no-store",
  });

  if (!response.ok) {
    throw new Error(`Failed to load accounts (${response.status})`);
  }

  const payload = await response.json();
  const accounts = (payload.data ?? payload) as AccountResponse[];
  return accounts.filter((account) => account.isCreditCard === true);
}

export interface SyncAccountLogosResult {
  saved: number;
  skipped: number;
  manifest: Record<
    number,
    {
      accountId: number;
      name: string;
      domain: string;
      url: string;
      filename: string;
      source: string;
      updatedAt: string;
    }
  >;
}

export async function syncAccountLogos(): Promise<SyncAccountLogosResult> {
  fs.mkdirSync(OUT_DIR, { recursive: true });

  const accounts = await loadCreditCardAccounts();
  const manifest: SyncAccountLogosResult["manifest"] = {};
  let saved = 0;
  let skipped = 0;

  for (const account of accounts) {
    const domain = resolveCreditCardLogoDomain(account.name, account.url);
    if (!domain) {
      skipped += 1;
      continue;
    }

    const logo = await downloadLogoForDomain(domain);
    if (!logo) {
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
  }

  fs.writeFileSync(
    path.join(OUT_DIR, "manifest.json"),
    `${JSON.stringify(manifest, null, 2)}\n`,
    "utf8",
  );

  return { saved, skipped, manifest };
}
