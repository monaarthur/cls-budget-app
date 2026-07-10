import { ApiError } from "@/lib/api/client";
import type { ApiResponse } from "@/lib/api/types";
import type {
  InviteTenantUserRequest,
  InviteTenantUserResponse,
  TenantSummary,
} from "@/features/admin/types/admin";
import { getAdminApiKey } from "@/features/admin/lib/adminStorage";

const ADMIN_KEY_HEADER = "X-Admin-Api-Key";

function getBaseUrl(): string {
  const configured = process.env.NEXT_PUBLIC_API_BASE_URL?.replace(/\/$/, "");
  if (configured) return configured;
  if (typeof window !== "undefined") return "";
  return "http://localhost:5123";
}

async function adminRequest<T>(
  path: string,
  init?: RequestInit,
): Promise<ApiResponse<T>> {
  const apiKey = getAdminApiKey();
  if (!apiKey) {
    throw new ApiError(401, "Admin API key is required.");
  }

  let res: Response;
  try {
    res = await fetch(`${getBaseUrl()}${path}`, {
      cache: "no-store",
      ...init,
      headers: {
        "Content-Type": "application/json",
        [ADMIN_KEY_HEADER]: apiKey,
        ...(init?.headers ?? {}),
      },
    });
  } catch {
    throw new ApiError(
      0,
      "Cannot reach the budget API. Start the backend with: dotnet run --project src/CLS.Budget.Api --launch-profile http",
    );
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

  if (!res.ok || body.success === false) {
    const message =
      body.errors?.filter(Boolean).join(" ") ||
      res.statusText ||
      "Request failed";
    throw new ApiError(res.status, message, body.errors ?? []);
  }

  return body;
}

export const adminApi = {
  listTenants: () =>
    adminRequest<TenantSummary[]>("/api/v1/admin/tenants"),

  inviteUser: (body: InviteTenantUserRequest) =>
    adminRequest<InviteTenantUserResponse>(
      "/api/v1/admin/tenant-users/invite",
      {
        method: "POST",
        body: JSON.stringify({
          tenantId: body.tenantId,
          email: body.email,
          displayName: body.displayName,
          role: body.role,
        }),
      },
    ),
};
