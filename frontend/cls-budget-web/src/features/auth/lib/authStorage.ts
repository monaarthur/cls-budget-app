import {
  clearAuthCookies,
  setRefreshCookie,
} from "@/features/auth/lib/authCookies";
import type { AuthResponse } from "@/features/auth/types/auth";

const ACCESS_TOKEN_KEY = "cls_budget_access_token";
const ACCESS_COOKIE = "access_token";

function maxAgeSeconds(isoExpiry: string): number {
  const ms = new Date(isoExpiry).getTime() - Date.now();
  if (Number.isNaN(ms)) return 60;
  return Math.max(60, Math.floor(ms / 1000));
}

function secureCookieSuffix(): string {
  return process.env.NODE_ENV === "production" ? "; Secure" : "";
}

function setAccessCookie(accessToken: string, accessTokenExpiresAt: string): void {
  const accessMaxAge = maxAgeSeconds(accessTokenExpiresAt);
  document.cookie = `${ACCESS_COOKIE}=${encodeURIComponent(accessToken)}; path=/; max-age=${accessMaxAge}; SameSite=Lax${secureCookieSuffix()}`;
}

/** Returns the stored access token, or null if missing/expired. */
export function getAccessToken(): string | null {
  if (typeof window === "undefined") return null;
  const token = localStorage.getItem(ACCESS_TOKEN_KEY);
  if (!token) return null;
  if (isJwtExpired(token)) {
    localStorage.removeItem(ACCESS_TOKEN_KEY);
    return null;
  }
  return token;
}

/** True when the JWT `exp` claim is missing or already past. */
export function isJwtExpired(token: string, skewSeconds = 30): boolean {
  try {
    const payload = token.split(".")[1];
    if (!payload) return true;
    const json = atob(payload.replace(/-/g, "+").replace(/_/g, "/"));
    const claims = JSON.parse(json) as { exp?: number };
    if (typeof claims.exp !== "number") return true;
    return claims.exp * 1000 <= Date.now() + skewSeconds * 1000;
  } catch {
    return true;
  }
}

/** Updates the access token in localStorage and the middleware-readable cookie. */
export function persistAccessToken(auth: Pick<AuthResponse, "accessToken" | "accessTokenExpiresAt">): void {
  localStorage.setItem(ACCESS_TOKEN_KEY, auth.accessToken);
  setAccessCookie(auth.accessToken, auth.accessTokenExpiresAt);
}

export async function persistAuthSession(auth: AuthResponse): Promise<void> {
  persistAccessToken(auth);
  await setRefreshCookie(auth.refreshToken, auth.refreshTokenExpiresAt);
}

export async function clearAuthSession(): Promise<void> {
  localStorage.removeItem(ACCESS_TOKEN_KEY);
  document.cookie = `${ACCESS_COOKIE}=; path=/; max-age=0; SameSite=Lax${secureCookieSuffix()}`;
  document.cookie = `${ACCESS_COOKIE}=; path=/; max-age=0; SameSite=Lax`;
  await clearAuthCookies();
}
