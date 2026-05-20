import Link from "next/link";
import { ChevronRight } from "lucide-react";
import { AccountAvatar } from "@/components/ui/AccountAvatar";
import { formatCurrency } from "@/lib/format";
import type { AccountResponse } from "@/features/accounts/types/account";

function utilizationPercent(balance: number, limit: number): number {
  if (limit <= 0) return 0;
  return Math.min(100, Math.round((balance / limit) * 100));
}

function utilizationClass(percent: number): string {
  if (percent >= 90) return "utilization-fill-danger";
  if (percent >= 70) return "utilization-fill-warning";
  return "utilization-fill";
}

export function AccountRow({
  account,
  showChevron = true,
}: {
  account: AccountResponse;
  showChevron?: boolean;
}) {
  const utilization = utilizationPercent(account.balance, account.limit);
  const hasLimit = account.limit > 0;
  const lastFour = account.number.replace(/\D/g, "").slice(-4) || account.number;

  return (
    <Link
      href="/accounts"
      className="flex items-center gap-3 rounded-2xl px-3 py-3 transition hover:bg-black/[0.04]"
    >
      <AccountAvatar name={account.name} />
      <div className="min-w-0 flex-1">
        <div className="flex items-start justify-between gap-2">
          <div className="min-w-0">
            <p className="truncate font-medium">{account.name}</p>
            <p className="truncate text-xs text-[var(--muted)]">
              {account.isPaidOff ? "Paid off" : `•••• ${lastFour}`}
            </p>
          </div>
          <p className="shrink-0 text-right font-semibold tabular-nums">
            {formatCurrency(account.balance)}
          </p>
        </div>
        {hasLimit && !account.isPaidOff ? (
          <div className="mt-2">
            <div className="utilization-track h-1.5 w-full overflow-hidden rounded-full">
              <div
                className={`utilization-fill ${utilizationClass(utilization)}`}
                style={{ width: `${utilization}%` }}
              />
            </div>
            <p className="mt-1 text-[10px] text-[var(--muted)]">
              {utilization}% of {formatCurrency(account.limit)} limit
            </p>
          </div>
        ) : null}
      </div>
      {showChevron ? (
        <ChevronRight className="h-4 w-4 shrink-0 text-[var(--muted)]" />
      ) : null}
    </Link>
  );
}
