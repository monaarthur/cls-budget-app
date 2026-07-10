/** When false, the app runs without login (matches backend Auth:Enabled=false in dev). */
export const AUTH_ENABLED =
  process.env.NEXT_PUBLIC_AUTH_ENABLED === "true";

export const AUTH_PUBLIC_PATHS = [
  "/login",
  "/register",
  "/forgot-password",
  "/reset-password",
  "/admin",
] as const;

export function isAdminPath(pathname: string | null | undefined): boolean {
  if (!pathname) return false;
  return pathname === "/admin" || pathname.startsWith("/admin/");
}

export function isAuthPublicPath(pathname: string | null | undefined): boolean {
  if (!pathname) return false;
  if (isAdminPath(pathname)) return true;
  return AUTH_PUBLIC_PATHS.some(
    (p) => pathname === p || pathname.startsWith(`${p}/`),
  );
}

/** Prefer Next pathname; fall back to the browser URL during static-export hydration. */
export function resolvePathname(pathname: string | null | undefined): string | null {
  if (pathname) return pathname;
  if (typeof window !== "undefined") return window.location.pathname;
  return null;
}
