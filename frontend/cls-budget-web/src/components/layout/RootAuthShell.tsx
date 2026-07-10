"use client";

import { usePathname } from "next/navigation";
import type { ReactNode } from "react";
import { AppShell } from "@/components/layout/AppShell";
import { AuthGate } from "@/features/auth/components/AuthGate";
import { isAdminPath, resolvePathname } from "@/features/auth/lib/authConfig";

/**
 * Admin routes skip AuthGate and AppShell (static export hydration + dedicated UI).
 */
export function RootAuthShell({ children }: { children: ReactNode }) {
  const pathname = usePathname();
  const resolvedPath = resolvePathname(pathname);

  if (isAdminPath(resolvedPath)) {
    return <>{children}</>;
  }

  return (
    <AuthGate>
      <AppShell>{children}</AppShell>
    </AuthGate>
  );
}
