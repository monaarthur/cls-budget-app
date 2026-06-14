import {
  clearAuthCookies,
  setRefreshCookie,
} from "@/features/auth/lib/authCookies";
import type { AuthResponse } from "@/features/auth/types/auth";

const ACCESS_TOKEN_KEY = "cls_budget_access_token";
const ACCESS_COOKIE = "access_token";

function maxAgeSeconds(isoExpiry: string): number {
  const ms = new Date(isoExpiry).getTime() - Date.now();
  return Math.max(60, Math.floor(ms / 1000));
}

function setAccessCookie(accessToken: string, accessTokenExpiresAt: string): void {
  const accessMaxAge = maxAgeSeconds(accessTokenExpiresAt);
  document.cookie = `${ACCESS_COOKIE}=${encodeURIComponent(accessToken)}; path=/; max-age=${accessMaxAge}; SameSite=Lax`;
}

export function getAccessToken(): string | null {
  if (typeof window === "undefined") return null;
  return localStorage.getItem(ACCESS_TOKEN_KEY);
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
  document.cookie = `${ACCESS_COOKIE}=; path=/; max-age=0; SameSite=Lax`;
  await clearAuthCookies();
}
