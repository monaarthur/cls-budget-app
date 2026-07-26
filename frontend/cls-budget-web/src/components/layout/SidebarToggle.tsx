"use client";

import { PanelLeftClose, PanelLeftOpen } from "lucide-react";
import { useSidebarNav } from "@/components/layout/SidebarNavContext";

export function SidebarToggle({
  className = "",
}: {
  className?: string;
}) {
  const { open, toggle } = useSidebarNav();

  return (
    <button
      type="button"
      onClick={toggle}
      className={`hidden h-9 w-9 shrink-0 items-center justify-center rounded-xl border border-[var(--border)] bg-[var(--card)] text-[var(--muted)] transition hover:bg-black/[0.04] hover:text-[var(--foreground)] lg:inline-flex ${className}`}
      aria-label={open ? "Hide side menu" : "Show side menu"}
      title={open ? "Hide side menu" : "Show side menu"}
    >
      {open ? (
        <PanelLeftClose className="h-4 w-4" strokeWidth={2} />
      ) : (
        <PanelLeftOpen className="h-4 w-4" strokeWidth={2} />
      )}
    </button>
  );
}
