import type { AuthResponse } from "@/features/auth/types/auth";

const REFRESH_COOKIE = "refresh_token";

function maxAgeSeconds(isoExpiry: string): number {
  const ms = new Date(isoExpiry).getTime() - Date.now();
  return Math.max(60, Math.floor(ms / 1000));
}

/** Persists the refresh token in an httpOnly cookie via the Next.js session route. */
export async function setRefreshCookie(
  refreshToken: string,
  refreshTokenExpiresAt: string,
): Promise<void> {
  await fetch("/api/auth/session", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ refreshToken, refreshTokenExpiresAt }),
  });
}

/** Clears auth cookies (access + refresh) via the Next.js session route. */
export async function clearAuthCookies(): Promise<void> {
  await fetch("/api/auth/session", { method: "DELETE" });
}

/** Server-side refresh using the httpOnly refresh cookie. */
export async function refreshSession(): Promise<AuthResponse | null> {
  const res = await fetch("/api/auth/refresh", { method: "POST" });
  if (!res.ok) return null;

  const body = (await res.json()) as {
    success: boolean;
    data: AuthResponse | null;
  };

  return body.success && body.data ? body.data : null;
}

export { REFRESH_COOKIE };
