import { NextResponse } from "next/server";
import { cookies } from "next/headers";

const REFRESH_COOKIE = "refresh_token";
const ACCESS_COOKIE = "access_token";

function apiBaseUrl(): string {
  return (process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:5123").replace(
    /\/$/,
    "",
  );
}

function maxAgeSeconds(isoExpiry: string): number {
  const ms = new Date(isoExpiry).getTime() - Date.now();
  return Math.max(60, Math.floor(ms / 1000));
}

export async function POST() {
  const cookieStore = await cookies();
  const refreshToken = cookieStore.get(REFRESH_COOKIE)?.value;

  if (!refreshToken) {
    return NextResponse.json(
      { success: false, data: null, errors: ["No refresh token."] },
      { status: 401 },
    );
  }

  const backendRes = await fetch(`${apiBaseUrl()}/api/v1/auth/refresh`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ refreshToken }),
    cache: "no-store",
  });

  const text = await backendRes.text();
  if (!text) {
    return NextResponse.json(
      { success: false, data: null, errors: ["Empty refresh response."] },
      { status: backendRes.status },
    );
  }

  const body = JSON.parse(text) as {
    success: boolean;
    data: {
      accessToken: string;
      accessTokenExpiresAt: string;
      refreshToken: string;
      refreshTokenExpiresAt: string;
      user: unknown;
    } | null;
    errors: string[];
  };

  if (!backendRes.ok || !body.success || !body.data) {
    const response = NextResponse.json(body, { status: backendRes.status });
    response.cookies.set(REFRESH_COOKIE, "", { httpOnly: true, path: "/", maxAge: 0 });
    response.cookies.set(ACCESS_COOKIE, "", { path: "/", maxAge: 0 });
    return response;
  }

  const response = NextResponse.json(body);
  response.cookies.set(REFRESH_COOKIE, body.data.refreshToken, {
    httpOnly: true,
    secure: process.env.NODE_ENV === "production",
    sameSite: "lax",
    path: "/",
    maxAge: maxAgeSeconds(body.data.refreshTokenExpiresAt),
  });
  response.cookies.set(ACCESS_COOKIE, body.data.accessToken, {
    httpOnly: false,
    secure: process.env.NODE_ENV === "production",
    sameSite: "lax",
    path: "/",
    maxAge: maxAgeSeconds(body.data.accessTokenExpiresAt),
  });

  return response;
}
