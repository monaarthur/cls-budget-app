import type { AuthResponse } from "@/features/auth/types/auth";

const REFRESH_COOKIE = "refresh_token";
const ACCESS_COOKIE = "access_token";

function apiBaseUrl(): string {
  return (process.env.NEXT_PUBLIC_API_BASE_URL ?? "").replace(/\/$/, "");
}

function maxAgeSeconds(isoExpiry: string): number {
  const ms = new Date(isoExpiry).getTime() - Date.now();
  return Math.max(60, Math.floor(ms / 1000));
}

function secureCookieSuffix(): string {
  return process.env.NODE_ENV === "production" ? "; Secure" : "";
}

function readCookie(name: string): string | null {
  if (typeof document === "undefined") return null;
  const match = document.cookie.match(
    new RegExp(`(?:^|;\\s*)${name}=([^;]*)`),
  );
  return match ? decodeURIComponent(match[1]) : null;
}

function writeCookie(name: string, value: string, maxAge: number): void {
  if (typeof document === "undefined") return;
  document.cookie = `${name}=${encodeURIComponent(value)}; path=/; max-age=${maxAge}; SameSite=Lax${secureCookieSuffix()}`;
}

function clearCookie(name: string): void {
  if (typeof document === "undefined") return;
  document.cookie = `${name}=; path=/; max-age=0; SameSite=Lax`;
}

async function refreshViaNextRoute(): Promise<AuthResponse | null> {
  const res = await fetch("/api/auth/refresh", { method: "POST" });
  if (!res.ok) return null;

  const body = (await res.json()) as {
    success: boolean;
    data: AuthResponse | null;
  };

  return body.success && body.data ? body.data : null;
}

async function refreshViaBackendApi(): Promise<AuthResponse | null> {
  const refreshToken = readCookie(REFRESH_COOKIE);
  if (!refreshToken) return null;

  const backendRes = await fetch(`${apiBaseUrl()}/api/v1/auth/refresh`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ refreshToken }),
    cache: "no-store",
  });

  const text = await backendRes.text();
  if (!text) return null;

  const body = JSON.parse(text) as {
    success: boolean;
    data: AuthResponse | null;
    errors: string[];
  };

  if (!backendRes.ok || !body.success || !body.data) {
    clearCookie(REFRESH_COOKIE);
    clearCookie(ACCESS_COOKIE);
    return null;
  }

  writeCookie(
    REFRESH_COOKIE,
    body.data.refreshToken,
    maxAgeSeconds(body.data.refreshTokenExpiresAt),
  );
  writeCookie(
    ACCESS_COOKIE,
    body.data.accessToken,
    maxAgeSeconds(body.data.accessTokenExpiresAt),
  );

  return body.data;
}

/** Persists the refresh token cookie for session refresh. */
export async function setRefreshCookie(
  refreshToken: string,
  refreshTokenExpiresAt: string,
): Promise<void> {
  if (typeof window !== "undefined") {
    writeCookie(
      REFRESH_COOKIE,
      refreshToken,
      maxAgeSeconds(refreshTokenExpiresAt),
    );
    return;
  }

  await fetch("/api/auth/session", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ refreshToken, refreshTokenExpiresAt }),
  });
}

/** Clears auth cookies. */
export async function clearAuthCookies(): Promise<void> {
  if (typeof window !== "undefined") {
    clearCookie(REFRESH_COOKIE);
    clearCookie(ACCESS_COOKIE);
    return;
  }

  await fetch("/api/auth/session", { method: "DELETE" });
}

/** Refresh session using the backend API or local Next.js route in dev. */
export async function refreshSession(): Promise<AuthResponse | null> {
  if (typeof window !== "undefined" && apiBaseUrl()) {
    return refreshViaBackendApi();
  }

  return refreshViaNextRoute();
}

export { REFRESH_COOKIE };
