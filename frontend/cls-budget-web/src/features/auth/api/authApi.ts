import { apiGet, apiPost } from "@/lib/api/client";
import type {
  AuthResponse,
  CurrentUser,
  ForgotPasswordRequest,
  LoginRequest,
  RegisterRequest,
  ResetPasswordRequest,
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

  forgotPassword: (body: ForgotPasswordRequest) =>
    apiPost<boolean, ForgotPasswordRequest>(`${basePath}/forgot-password`, body),

  resetPassword: (body: ResetPasswordRequest) =>
    apiPost<boolean, ResetPasswordRequest>(`${basePath}/reset-password`, body),
};
