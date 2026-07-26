"use client";

import type { ReactNode } from "react";
import { SidebarToggle } from "@/components/layout/SidebarToggle";
import { getGreeting } from "@/lib/format";

export function TopBar({
  title,
  actions,
}: {
  title?: string;
  actions?: ReactNode;
}) {
  return (
    <header className="mb-6 flex items-center justify-between gap-3">
      <div className="flex min-w-0 items-start gap-3">
        <SidebarToggle className="mt-1" />
        <div className="min-w-0">
          <p className="text-sm text-[var(--muted)]">{getGreeting()}</p>
          <div className="flex flex-wrap items-baseline gap-x-4 gap-y-1">
            <h1 className="text-2xl font-bold tracking-tight">
              {title ?? "Overview"}
            </h1>
            {actions}
          </div>
        </div>
      </div>
      <div
        className="flex h-10 w-10 shrink-0 items-center justify-center rounded-full bg-[var(--card-elevated)] text-sm font-semibold text-[var(--muted)]"
        aria-label="Profile"
      >
        U
      </div>
    </header>
  );
}
