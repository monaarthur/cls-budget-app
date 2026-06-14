"use client";

import { useMemo, useState } from "react";
import { AccountAvatar } from "@/components/ui/AccountAvatar";
import { getAccountLogoCandidates } from "@/lib/accountLogo";

export function CompanyLogo({
  name,
  accountId,
  size = 44,
  className = "",
}: {
  name: string;
  accountId: number;
  size?: number;
  className?: string;
}) {
  const candidates = useMemo(
    () => getAccountLogoCandidates(accountId),
    [accountId],
  );
  const [candidateIndex, setCandidateIndex] = useState(0);

  if (candidateIndex >= candidates.length) {
    return <AccountAvatar name={name} size={size} className={className} />;
  }

  return (
    <img
      src={candidates[candidateIndex]}
      width={size}
      height={size}
      alt=""
      aria-hidden
      className={`shrink-0 rounded-full bg-white object-contain ring-1 ring-black/10 ${className}`}
      style={{ width: size, height: size, padding: Math.max(2, size * 0.12) }}
      onError={() => setCandidateIndex((index) => index + 1)}
    />
  );
}
