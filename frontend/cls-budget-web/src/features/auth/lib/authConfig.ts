/** When false, the app runs without login (matches backend Auth:Enabled=false in dev). */
export const AUTH_ENABLED =
  process.env.NEXT_PUBLIC_AUTH_ENABLED === "true";

export const AUTH_PUBLIC_PATHS = ["/login", "/register"] as const;

export function isAuthPublicPath(pathname: string): boolean {
  return AUTH_PUBLIC_PATHS.some(
    (p) => pathname === p || pathname.startsWith(`${p}/`),
  );
}
