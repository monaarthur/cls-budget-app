import type { ReactNode } from "react";

/** Admin uses its own page chrome; bypasses AppShell from the root layout. */
export default function AdminLayout({ children }: { children: ReactNode }) {
  return children;
}
