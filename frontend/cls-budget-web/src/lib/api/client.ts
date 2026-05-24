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

async function request<T>(
  path: string,
  init?: RequestInit,
): Promise<ApiResponse<T>> {
  let res: Response;

  try {
    res = await fetch(`${getBaseUrl()}${path}`, {
      cache: "no-store",
      ...init,
      headers: {
        "Content-Type": "application/json",
        ...init?.headers,
      },
    });
  } catch {
    throw new ApiError(0, API_UNAVAILABLE_MESSAGE);
  }

  if (res.status === 204) {
    return { success: true, data: null, errors: [] };
  }

  const text = await res.text();
  if (!text) {
    if (res.status >= 500) {
      throw new ApiError(res.status, API_UNAVAILABLE_MESSAGE);
    }

    throw new ApiError(res.status, res.statusText || "Empty API response");
  }

  let body: ApiResponse<T>;
  try {
    body = JSON.parse(text) as ApiResponse<T>;
  } catch {
    if (res.status >= 500) {
      throw new ApiError(res.status, API_UNAVAILABLE_MESSAGE);
    }

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
