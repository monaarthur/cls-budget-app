"use client";

import { usePathname, useRouter } from "next/navigation";
import { useEffect, type ReactNode } from "react";
import { useAuth } from "@/features/auth/hooks/useAuth";
import {
  isAuthPublicPath,
  resolvePathname,
} from "@/features/auth/lib/authConfig";

/**
 * Client-side guard when auth is enabled. Middleware handles the cookie check;
 * this covers hydration edge cases before the session is restored.
 */
export function AuthGate({ children }: { children: ReactNode }) {
  const pathname = usePathname();
  const router = useRouter();
  const { authEnabled, isLoading, isAuthenticated } = useAuth();
  const resolvedPath = resolvePathname(pathname);
  const isPublic = isAuthPublicPath(resolvedPath);

  useEffect(() => {
    if (!authEnabled || isLoading || isAuthenticated) return;

    const path = resolvePathname(pathname);
    if (!path || isAuthPublicPath(path)) return;

    const returnUrl = encodeURIComponent(path);
    router.replace(`/login?returnUrl=${returnUrl}`);
  }, [authEnabled, isLoading, isAuthenticated, pathname, router]);

  if (isPublic) {
    return <>{children}</>;
  }

  if (!resolvedPath || (authEnabled && isLoading)) {
    return (
      <div className="flex flex-1 items-center justify-center p-12 text-sm text-[var(--muted)]">
        Loading…
      </div>
    );
  }

  if (authEnabled && !isLoading && !isAuthenticated) {
    return null;
  }

  return <>{children}</>;
}
