"use client";

import { LogOut, User } from "lucide-react";
import { useAuth } from "@/features/auth/hooks/useAuth";

export function UserMenu({ compact = false }: { compact?: boolean }) {
  const { authEnabled, user, logout } = useAuth();

  if (!authEnabled || !user) {
    return null;
  }

  if (compact) {
    return (
      <div className="flex items-center gap-2">
        <span className="max-w-[120px] truncate text-xs font-medium text-[var(--muted)]">
          {user.displayName}
        </span>
        <button
          type="button"
          onClick={() => void logout()}
          className="rounded-lg p-2 text-[var(--muted)] transition hover:bg-black/[0.04] hover:text-[var(--foreground)]"
          aria-label="Sign out"
        >
          <LogOut className="h-4 w-4" />
        </button>
      </div>
    );
  }

  return (
    <div className="mt-auto border-t border-[var(--border)] pt-4">
      <div className="flex items-center gap-3 rounded-xl px-3 py-2.5">
        <div className="flex h-9 w-9 items-center justify-center rounded-full bg-[var(--accent-soft)] text-[var(--link)]">
          <User className="h-4 w-4" />
        </div>
        <div className="min-w-0 flex-1">
          <p className="truncate text-sm font-semibold">{user.displayName}</p>
          <p className="truncate text-xs text-[var(--muted)]">{user.role}</p>
        </div>
        <button
          type="button"
          onClick={() => void logout()}
          className="rounded-lg p-2 text-[var(--muted)] transition hover:bg-black/[0.04] hover:text-[var(--foreground)]"
          aria-label="Sign out"
          title="Sign out"
        >
          <LogOut className="h-4 w-4" />
        </button>
      </div>
    </div>
  );
}
