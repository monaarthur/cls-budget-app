import { BottomNav } from "@/components/layout/BottomNav";
import { MainContent } from "@/components/layout/MainContent";
import { SidebarNav } from "@/components/layout/SidebarNav";

export function AppShell({ children }: { children: React.ReactNode }) {
  return (
    <div className="flex min-h-screen bg-[var(--background)] text-[var(--foreground)]">
      <SidebarNav />
      <div className="flex min-h-screen flex-1 flex-col">
        <MainContent>{children}</MainContent>
        <BottomNav />
      </div>
    </div>
  );
}
