"use client";

import { usePathname } from "next/navigation";
import { BottomNav } from "@/components/layout/BottomNav";
import { MainContent } from "@/components/layout/MainContent";
import { SidebarNav } from "@/components/layout/SidebarNav";
import {
  SidebarNavProvider,
  useSidebarNav,
} from "@/components/layout/SidebarNavContext";
import { UserMenu } from "@/features/auth/components/UserMenu";
import { isAuthPublicPath } from "@/features/auth/lib/authConfig";

function AppShellLayout({ children }: { children: React.ReactNode }) {
  const { open } = useSidebarNav();

  return (
    <div className="flex min-h-screen bg-[var(--background)] text-[var(--foreground)]">
      {open ? <SidebarNav /> : null}
      <div className="flex min-h-screen min-w-0 flex-1 flex-col">
        <header className="flex items-center justify-end border-b border-[var(--border)] px-4 py-3 lg:hidden">
          <UserMenu compact />
        </header>
        <MainContent>{children}</MainContent>
        <BottomNav />
      </div>
    </div>
  );
}

export function AppShell({ children }: { children: React.ReactNode }) {
  const pathname = usePathname();

  if (isAuthPublicPath(pathname)) {
    return <>{children}</>;
  }

  return (
    <SidebarNavProvider>
      <AppShellLayout>{children}</AppShellLayout>
    </SidebarNavProvider>
  );
}
