import { NextResponse } from "next/server";

const REFRESH_COOKIE = "refresh_token";
const ACCESS_COOKIE = "access_token";

function maxAgeSeconds(isoExpiry: string): number {
  const ms = new Date(isoExpiry).getTime() - Date.now();
  return Math.max(60, Math.floor(ms / 1000));
}

export async function POST(request: Request) {
  const body = (await request.json()) as {
    refreshToken?: string;
    refreshTokenExpiresAt?: string;
    accessToken?: string;
    accessTokenExpiresAt?: string;
  };

  const response = NextResponse.json({ success: true });

  if (body.refreshToken && body.refreshTokenExpiresAt) {
    response.cookies.set(REFRESH_COOKIE, body.refreshToken, {
      httpOnly: true,
      secure: process.env.NODE_ENV === "production",
      sameSite: "lax",
      path: "/",
      maxAge: maxAgeSeconds(body.refreshTokenExpiresAt),
    });
  }

  if (body.accessToken && body.accessTokenExpiresAt) {
    response.cookies.set(ACCESS_COOKIE, body.accessToken, {
      httpOnly: false,
      secure: process.env.NODE_ENV === "production",
      sameSite: "lax",
      path: "/",
      maxAge: maxAgeSeconds(body.accessTokenExpiresAt),
    });
  }

  return response;
}

export async function DELETE() {
  const response = NextResponse.json({ success: true });
  response.cookies.set(REFRESH_COOKIE, "", { httpOnly: true, path: "/", maxAge: 0 });
  response.cookies.set(ACCESS_COOKIE, "", { path: "/", maxAge: 0 });
  return response;
}
