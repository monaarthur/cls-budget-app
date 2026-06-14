import { apiGet, apiPost } from "@/lib/api/client";
import type {
  AuthResponse,
  CurrentUser,
  LoginRequest,
  RegisterRequest,
} from "@/features/auth/types/auth";

const basePath = "/api/v1/auth";

export const authApi = {
  login: (body: LoginRequest) =>
    apiPost<AuthResponse, LoginRequest>(`${basePath}/login`, body),

  register: (body: RegisterRequest) =>
    apiPost<AuthResponse, RegisterRequest>(`${basePath}/register`, body),

  refresh: (refreshToken: string) =>
    apiPost<AuthResponse, { refreshToken: string }>(`${basePath}/refresh`, {
      refreshToken,
    }),

  me: (accessToken: string) =>
    apiGet<CurrentUser>(`${basePath}/me`, {
      headers: { Authorization: `Bearer ${accessToken}` },
    }),
};
