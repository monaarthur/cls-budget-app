"use client";

import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from "react";
import { useRouter } from "next/navigation";
import { authApi } from "@/features/auth/api/authApi";
import { refreshSession } from "@/features/auth/lib/authCookies";
import { AUTH_ENABLED } from "@/features/auth/lib/authConfig";
import {
  clearAuthSession,
  getAccessToken,
  persistAuthSession,
  persistAccessToken,
} from "@/features/auth/lib/authStorage";
import type {
  AuthResponse,
  CurrentUser,
  LoginRequest,
  RegisterRequest,
} from "@/features/auth/types/auth";
import { ApiError } from "@/lib/api/client";

export interface AuthContextValue {
  authEnabled: boolean;
  isLoading: boolean;
  isAuthenticated: boolean;
  user: CurrentUser | null;
  login: (request: LoginRequest) => Promise<void>;
  register: (request: RegisterRequest) => Promise<void>;
  logout: () => Promise<void>;
}

const AuthContext = createContext<AuthContextValue | null>(null);

async function applyAuthResponse(
  response: AuthResponse,
  setUser: (user: CurrentUser) => void,
) {
  await persistAuthSession(response);
  setUser(response.user);
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const router = useRouter();
  const [isLoading, setIsLoading] = useState(AUTH_ENABLED);
  const [user, setUser] = useState<CurrentUser | null>(null);

  const hydrate = useCallback(async () => {
    if (!AUTH_ENABLED) {
      setUser(null);
      setIsLoading(false);
      return;
    }

    let accessToken = getAccessToken();

    if (!accessToken) {
      const refreshed = await refreshSession();
      if (refreshed) {
        persistAccessToken(refreshed);
        setUser(refreshed.user);
        setIsLoading(false);
        return;
      }

      setUser(null);
      setIsLoading(false);
      return;
    }

    try {
      const result = await authApi.me(accessToken);
      if (result.data) {
        setUser(result.data);
      } else {
        await clearAuthSession();
        setUser(null);
      }
    } catch (err) {
      if (err instanceof ApiError && err.status === 401) {
        const refreshed = await refreshSession();
        if (refreshed) {
          await applyAuthResponse(refreshed, setUser);
          setIsLoading(false);
          return;
        }
      }
      await clearAuthSession();
      setUser(null);
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    void hydrate();
  }, [hydrate]);

  const login = useCallback(async (request: LoginRequest) => {
    const result = await authApi.login(request);
    if (!result.data) {
      throw new ApiError(400, result.errors[0] ?? "Login failed", result.errors);
    }
    await applyAuthResponse(result.data, setUser);
  }, []);

  const register = useCallback(async (request: RegisterRequest) => {
    const result = await authApi.register(request);
    if (!result.data) {
      throw new ApiError(
        400,
        result.errors[0] ?? "Registration failed",
        result.errors,
      );
    }
    await applyAuthResponse(result.data, setUser);
  }, []);

  const logout = useCallback(async () => {
    await clearAuthSession();
    setUser(null);
    if (AUTH_ENABLED) {
      router.push("/login");
    }
  }, [router]);

  const isAuthenticated = AUTH_ENABLED ? user !== null : true;

  const value = useMemo<AuthContextValue>(
    () => ({
      authEnabled: AUTH_ENABLED,
      isLoading,
      isAuthenticated,
      user,
      login,
      register,
      logout,
    }),
    [isLoading, isAuthenticated, user, login, register, logout],
  );

  return (
    <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
  );
}

export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext);
  if (!ctx) {
    throw new Error("useAuth must be used within AuthProvider");
  }
  return ctx;
}
