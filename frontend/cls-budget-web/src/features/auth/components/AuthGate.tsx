"use client";

import { usePathname, useRouter } from "next/navigation";
import { useEffect, type ReactNode } from "react";
import { useAuth } from "@/features/auth/hooks/useAuth";
import { isAuthPublicPath } from "@/features/auth/lib/authConfig";

/**
 * Client-side guard when auth is enabled. Middleware handles the cookie check;
 * this covers hydration edge cases before the session is restored.
 */
export function AuthGate({ children }: { children: ReactNode }) {
  const pathname = usePathname();
  const router = useRouter();
  const { authEnabled, isLoading, isAuthenticated } = useAuth();

  useEffect(() => {
    if (!authEnabled || isLoading || isAuthenticated) return;
    if (isAuthPublicPath(pathname)) return;

    const returnUrl = encodeURIComponent(pathname);
    router.replace(`/login?returnUrl=${returnUrl}`);
  }, [authEnabled, isLoading, isAuthenticated, pathname, router]);

  if (authEnabled && isLoading && !isAuthPublicPath(pathname)) {
    return (
      <div className="flex flex-1 items-center justify-center p-12 text-sm text-[var(--muted)]">
        Loading…
      </div>
    );
  }

  if (
    authEnabled &&
    !isLoading &&
    !isAuthenticated &&
    !isAuthPublicPath(pathname)
  ) {
    return null;
  }

  return <>{children}</>;
}
