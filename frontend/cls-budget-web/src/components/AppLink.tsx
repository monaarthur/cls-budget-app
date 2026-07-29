import type { LinkProps } from "next/link";
import NextLink from "next/link";
import {
  forwardRef,
  type AnchorHTMLAttributes,
  type ReactNode,
} from "react";

/**
 * Normalize internal paths for static export (`trailingSlash: true` → `/path/index.html` on S3).
 * External URLs, hashes, and mailto links are left unchanged.
 */
export function appHref(href: string): string {
  if (
    !href ||
    href.startsWith("http://") ||
    href.startsWith("https://") ||
    href.startsWith("mailto:") ||
    href.startsWith("tel:") ||
    href.startsWith("#")
  ) {
    return href;
  }

  const hashIndex = href.indexOf("#");
  const queryIndex = href.indexOf("?");
  let splitAt = -1;
  if (hashIndex >= 0 && queryIndex >= 0) splitAt = Math.min(hashIndex, queryIndex);
  else if (hashIndex >= 0) splitAt = hashIndex;
  else if (queryIndex >= 0) splitAt = queryIndex;

  const path = splitAt >= 0 ? href.slice(0, splitAt) : href;
  const suffix = splitAt >= 0 ? href.slice(splitAt) : "";

  if (path === "" || path === "/") {
    return `/${suffix.startsWith("?") || suffix.startsWith("#") ? suffix : ""}`;
  }

  const withSlash = path.endsWith("/") ? path : `${path}/`;
  return `${withSlash}${suffix}`;
}

function normalizeHref(href: LinkProps["href"]): LinkProps["href"] {
  if (typeof href === "string") return appHref(href);
  if (href && typeof href === "object" && "pathname" in href && href.pathname) {
    return { ...href, pathname: appHref(href.pathname) };
  }
  return href;
}

type AppLinkProps = Omit<
  AnchorHTMLAttributes<HTMLAnchorElement>,
  keyof LinkProps
> &
  LinkProps & {
    children?: ReactNode;
  };

/**
 * Drop-in replacement for `next/link` that keeps hrefs compatible with S3 static hosting.
 */
export const AppLink = forwardRef<HTMLAnchorElement, AppLinkProps>(
  function AppLink({ href, ...props }, ref) {
    return <NextLink ref={ref} href={normalizeHref(href)} {...props} />;
  },
);
