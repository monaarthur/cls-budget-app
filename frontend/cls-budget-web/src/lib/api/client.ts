import { AUTH_ENABLED } from "@/features/auth/lib/authConfig";
import { refreshSession } from "@/features/auth/lib/authCookies";
import {
  clearAuthSession,
  getAccessToken,
  persistAccessToken,
} from "@/features/auth/lib/authStorage";
import type { ApiResponse } from "./types";

export class ApiError extends Error {
  constructor(
    public readonly status: number,
    message: string,
    public readonly errors: string[] = [],
  ) {
    super(message);
    this.name = "ApiError";
  }
}

function getBaseUrl(): string {
  // Browser requests go through the Next.js dev proxy (same origin, no CORS).
  if (typeof window !== "undefined") {
    return "";
  }

  const baseUrl =
    process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:5123";
  return baseUrl.replace(/\/$/, "");
}

const API_UNAVAILABLE_MESSAGE =
  "Cannot reach the budget API. Start the backend with: dotnet run --project src/CLS.Budget.Api --launch-profile http";

let refreshPromise: Promise<boolean> | null = null;

async function tryRefreshSession(): Promise<boolean> {
  if (refreshPromise) return refreshPromise;

  refreshPromise = (async () => {
    const auth = await refreshSession();
    if (!auth) return false;
    persistAccessToken(auth);
    return true;
  })().finally(() => {
    refreshPromise = null;
  });

  return refreshPromise;
}

function authHeaders(init?: RequestInit): HeadersInit {
  const headers: Record<string, string> = {
    "Content-Type": "application/json",
  };

  if (AUTH_ENABLED) {
    const token = getAccessToken();
    if (token) {
      headers.Authorization = `Bearer ${token}`;
    }
  }

  const extra = init?.headers;
  if (extra instanceof Headers) {
    extra.forEach((value, key) => {
      headers[key] = value;
    });
  } else if (Array.isArray(extra)) {
    for (const [key, value] of extra) {
      headers[key] = value;
    }
  } else if (extra) {
    Object.assign(headers, extra);
  }

  return headers;
}

async function request<T>(
  path: string,
  init?: RequestInit,
  allowRetry = true,
): Promise<ApiResponse<T>> {
  let res: Response;

  try {
    res = await fetch(`${getBaseUrl()}${path}`, {
      cache: "no-store",
      ...init,
      headers: authHeaders(init),
    });
  } catch {
    throw new ApiError(0, API_UNAVAILABLE_MESSAGE);
  }

  if (
    AUTH_ENABLED &&
    res.status === 401 &&
    allowRetry &&
    !path.startsWith("/api/v1/auth/")
  ) {
    const refreshed = await tryRefreshSession();
    if (refreshed) {
      return request<T>(path, init, false);
    }

    await clearAuthSession();
    if (typeof window !== "undefined") {
      const returnUrl = encodeURIComponent(window.location.pathname);
      window.location.href = `/login?returnUrl=${returnUrl}`;
    }
    throw new ApiError(401, "Session expired. Please sign in again.");
  }

  if (res.status === 204) {
    return { success: true, data: null, errors: [] };
  }

  const text = await res.text();
  if (!text) {
    throw new ApiError(res.status, res.statusText || "Empty API response");
  }

  let body: ApiResponse<T>;
  try {
    body = JSON.parse(text) as ApiResponse<T>;
  } catch {
    throw new ApiError(res.status, "Invalid API response");
  }

  if (!res.ok) {
    throw new ApiError(
      res.status,
      body.errors?.[0] ?? res.statusText,
      body.errors ?? [],
    );
  }

  if (body.success === false) {
    throw new ApiError(
      res.status,
      body.errors?.[0] ?? "Request failed",
      body.errors ?? [],
    );
  }

  return body;
}

export function apiGet<T>(path: string, init?: RequestInit) {
  return request<T>(path, init);
}

export function apiPost<T, B>(path: string, body: B) {
  return request<T>(path, {
    method: "POST",
    body: JSON.stringify(body),
  });
}

export function apiPut<T, B>(path: string, body: B) {
  return request<T>(path, {
    method: "PUT",
    body: JSON.stringify(body),
  });
}

export function apiDelete<T>(path: string) {
  return request<T>(path, { method: "DELETE" });
}
